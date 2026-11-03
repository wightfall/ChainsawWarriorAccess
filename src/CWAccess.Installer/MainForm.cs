using System;
using System.Diagnostics;
using System.Drawing;
using System.IO;
using System.Reflection;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace CWAccess.Installer
{
    /// <summary>
    /// The setup / updater window, built from standard Windows controls so screen readers read it: the setup
    /// language, two check boxes, a list of everything that happens (one event per line, focused at start; each new
    /// event is selected so it is read), and buttons with Alt shortcuts. Updates show their notes in UpdateDialog.
    /// </summary>
    public sealed class MainForm : Form
    {
        private readonly Label languageLabel = new Label();
        private readonly ComboBox languageBox = new ComboBox();
        private readonly CheckBox useLanguageCheck = new CheckBox();
        private readonly CheckBox startGameCheck = new CheckBox();
        private readonly Label eventsLabel = new Label();
        private readonly StatusBox events = new StatusBox();
        private readonly ProgressBar progress = new ProgressBar();
        private readonly Button installButton = new Button();
        private readonly Button checkButton = new Button();
        private readonly Button folderButton = new Button();
        private readonly Button startButton = new Button();
        private readonly Button uninstallButton = new Button();
        private readonly Button readmeButton = new Button();
        private readonly Button closeButton = new Button();
        private readonly GitHubClient github = new GitHubClient();
        private readonly Options options;

        private string gameDir;
        private Release latest;
        private bool busy;
        private bool filling;

        public static Version OwnVersion => Versions.Normalize(Assembly.GetExecutingAssembly().GetName().Version);

        public static bool HasPayload => Assembly.GetExecutingAssembly().GetManifestResourceInfo("payload.zip") != null;

        public MainForm(Options options)
        {
            this.options = options;
            Font = new Font("Segoe UI", 10f);
            ClientSize = new Size(680, 560);
            MinimumSize = new Size(600, 520);
            StartPosition = FormStartPosition.CenterScreen;

            languageLabel.AutoSize = true;
            languageLabel.Location = new Point(12, 16);
            languageBox.DropDownStyle = ComboBoxStyle.DropDownList;
            languageBox.SetBounds(220, 12, 448, 30);
            languageBox.Anchor = AnchorStyles.Top | AnchorStyles.Left | AnchorStyles.Right;
            languageBox.SelectedIndexChanged += OnLanguageChanged;

            useLanguageCheck.SetBounds(12, 50, 656, 28);
            useLanguageCheck.Anchor = AnchorStyles.Top | AnchorStyles.Left | AnchorStyles.Right;
            useLanguageCheck.Checked = options.ModLanguage != null;
            startGameCheck.SetBounds(12, 80, 656, 28);
            startGameCheck.Anchor = AnchorStyles.Top | AnchorStyles.Left | AnchorStyles.Right;
            startGameCheck.Checked = options.StartGame;

            eventsLabel.AutoSize = true;
            eventsLabel.Location = new Point(12, 116);
            events.SetBounds(12, 140, 656, 250);
            events.Anchor = AnchorStyles.Top | AnchorStyles.Bottom | AnchorStyles.Left | AnchorStyles.Right;

            progress.SetBounds(12, 396, 656, 18);
            progress.Anchor = AnchorStyles.Left | AnchorStyles.Right | AnchorStyles.Bottom;
            progress.Visible = false;

            int tab = 0;
            foreach (Control c in new Control[] { languageLabel, languageBox, useLanguageCheck, startGameCheck, eventsLabel, events }) c.TabIndex = tab++;
            int x = 12, y = 424;
            AddButton(installButton, ref x, y, ref tab, OnInstall);
            AddButton(checkButton, ref x, y, ref tab, OnCheck);
            AddButton(folderButton, ref x, y, ref tab, OnChooseFolder);
            x = 12;
            y += 46;
            AddButton(startButton, ref x, y, ref tab, OnStartGame);
            AddButton(uninstallButton, ref x, y, ref tab, OnUninstall);
            AddButton(readmeButton, ref x, y, ref tab, OnReadme);
            closeButton.SetBounds(12, 516, 214, 36);
            closeButton.Anchor = AnchorStyles.Left | AnchorStyles.Bottom;
            closeButton.TabIndex = tab;
            closeButton.Click += (s, e) => Close();
            Controls.Add(closeButton);
            CancelButton = closeButton;

            Controls.AddRange(new Control[] { languageLabel, languageBox, useLanguageCheck, startGameCheck, eventsLabel, events, progress });
            ApplyTexts();
            Shown += async (s, e) => await StartAsync();
        }

        private void AddButton(Button b, ref int x, int y, ref int tab, EventHandler click)
        {
            b.SetBounds(x, y, 214, 40);
            b.Anchor = AnchorStyles.Left | AnchorStyles.Bottom;
            b.TabIndex = tab++;
            b.Click += click;
            Controls.Add(b);
            x += 224;
        }

        private void ApplyTexts()
        {
            Text = SetupText.F("title", "Chainsaw Warrior Access Setup {0}", Versions.Show(OwnVersion));
            languageLabel.Text = SetupText.T("language_label", "Setup &language:");
            languageBox.AccessibleName = SetupText.T("language", "Setup language");
            useLanguageCheck.Text = SetupText.T("use_language", "Use this language for the &mod in the game");
            startGameCheck.Text = SetupText.T("start_check", "&Start Chainsaw Warrior when setup finishes (Play is pressed in its settings window)");
            eventsLabel.Text = SetupText.T("events_label", "&Events (newest last):");
            events.AccessibleName = SetupText.T("events", "Events");
            progress.AccessibleName = SetupText.T("progress", "Download progress");
            installButton.Text = SetupText.T("btn_install", "&Install or update");
            checkButton.Text = SetupText.T("btn_check", "&Check for updates");
            folderButton.Text = SetupText.T("btn_folder", "Choose game &folder");
            startButton.Text = SetupText.T("btn_start", "Start the &game now");
            uninstallButton.Text = SetupText.T("btn_uninstall", "&Uninstall");
            readmeButton.Text = SetupText.T("btn_readme", "&Read me");
            closeButton.Text = SetupText.T("btn_close", "Cl&ose");
        }

        // ------------------------------------------------------------------ events list

        /// <summary>Adds a line to the events box (newest last); the cursor stays on its first line.</summary>
        private void Event(string text)
        {
            if (InvokeRequired)
            {
                BeginInvoke(new Action<string>(Event), text);
                return;
            }
            events.AppendLine(text);
        }

        private void UpdateButtons()
        {
            bool game = gameDir != null;
            installButton.Enabled = game && !busy;
            uninstallButton.Enabled = game && !busy && (Installer.InstalledVersion != null || Installer.IsPatched);
            startButton.Enabled = game && !busy;
            checkButton.Enabled = folderButton.Enabled = !busy;
        }

        private ModInstaller Installer => gameDir != null ? new ModInstaller(gameDir) : null;

        private void FillLanguages()
        {
            filling = true;
            try
            {
                languageBox.Items.Clear();
                foreach (SetupText.Choice c in SetupText.Available(gameDir))
                {
                    languageBox.Items.Add(c);
                    if (c.Code == SetupText.Current) languageBox.SelectedItem = c;
                }
                if (languageBox.SelectedIndex < 0 && languageBox.Items.Count > 0) languageBox.SelectedIndex = 0;
            }
            finally
            {
                filling = false;
            }
        }

        private void OnLanguageChanged(object sender, EventArgs e)
        {
            if (filling || !(languageBox.SelectedItem is SetupText.Choice c)) return;
            if (!SetupText.Use(gameDir, c.Code)) return;
            ApplyTexts();
            Event(SetupText.F("ev_language", "Setup language: {0}.", c.Name));
        }

        // ------------------------------------------------------------------ start-up

        private async Task StartAsync()
        {
            ModInstaller.RemoveOldCopies(Application.ExecutablePath);
            gameDir = FindGameFolder();
            FillLanguages();
            events.Focus();
            events.ToTop();
            DescribeState();
            UpdateButtons();
            if (options.InstallNow)
            {
                await InstallAsync();
                return;
            }
            await CheckOnlineAsync(interactive: false);
        }

        private string FindGameFolder()
        {
            if (options.GameDir != null && ModInstaller.IsGameFolder(options.GameDir)) return options.GameDir;
            string exeDir = Path.GetDirectoryName(Application.ExecutablePath);
            return ModInstaller.FindGame(new[] { exeDir, Path.GetDirectoryName(exeDir) });
        }

        private void DescribeState()
        {
            if (gameDir == null)
            {
                Event(SetupText.T("ev_no_game", "Chainsaw Warrior was not found. Press Choose game folder and select the folder that contains CWSteamPC.exe."));
            }
            else
            {
                Event(SetupText.F("ev_game", "Game folder: {0}", gameDir));
                Event(SetupText.F("ev_installed", "Installed mod version: {0}.", Versions.Show(Installer.InstalledVersion)));
            }
            Event(HasPayload
                ? SetupText.F("ev_contains", "This setup contains version {0}.", Versions.Show(OwnVersion))
                : SetupText.F("ev_contains_online", "This setup is version {0}; it downloads the mod from GitHub.", Versions.Show(OwnVersion)));
        }

        // ------------------------------------------------------------------ update check

        private async Task CheckOnlineAsync(bool interactive)
        {
            SetBusy(true);
            Event(SetupText.T("ev_checking", "Checking GitHub for updates..."));
            try
            {
                latest = await github.GetLatestAsync();
                Event(latest != null ? SetupText.F("ev_latest", "Newest version on GitHub: {0}.", Versions.Show(latest.Version))
                                     : SetupText.T("ev_no_release", "No release is published on GitHub yet."));
            }
            catch (System.Net.WebException ex) when (ex.Response is System.Net.HttpWebResponse r && r.StatusCode == System.Net.HttpStatusCode.NotFound)
            {
                latest = null;
                Event(SetupText.T("ev_no_release", "No release is published on GitHub yet."));
            }
            catch (Exception ex)
            {
                latest = null;
                Event(SetupText.F("ev_offline", "Could not reach GitHub: {0}", ex.Message));
            }
            SetBusy(false);
            if (gameDir == null) return;
            Version installed = Installer.InstalledVersion;
            Version best = Versions.IsNewer(OwnVersion, installed) && HasPayload ? OwnVersion : installed;
            if (latest != null && Versions.IsNewer(latest.Version, best))
            {
                string have = installed == null ? SetupText.T("not_installed_yet", "the mod is not installed yet") : SetupText.F("you_have", "you have {0}", Versions.Show(installed));
                if (UpdateDialog.Ask(this, SetupText.F("upd_available", "Chainsaw Warrior Access {0} is available ({1}).", Versions.Show(latest.Version), have), latest.Body))
                    await InstallAsync();
                else Event(SetupText.T("ev_postponed", "Update postponed. Run this program again any time to install it."));
            }
            else if (HasPayload && Versions.IsNewer(OwnVersion, installed))
            {
                string body = latest != null && Versions.Normalize(latest.Version) == OwnVersion ? latest.Body : null;
                string have = installed == null ? SetupText.T("not_installed_yet", "the mod is not installed yet") : SetupText.F("you_have", "you have {0}", Versions.Show(installed));
                if (UpdateDialog.Ask(this, SetupText.F("upd_contained", "This setup contains Chainsaw Warrior Access {0} ({1}).", Versions.Show(OwnVersion), have), body))
                    await InstallAsync();
            }
            else
            {
                Event(installed == null ? SetupText.T("ev_press_install", "The mod is not installed. Press Install or update.")
                                        : SetupText.F("ev_up_to_date", "You have the newest version, {0}.", Versions.Show(installed)));
                if (interactive && installed != null)
                    MessageBox.Show(this, SetupText.F("ev_up_to_date", "You have the newest version, {0}.", Versions.Show(installed)), SetupText.T("no_updates", "No updates"), MessageBoxButtons.OK, MessageBoxIcon.Information);
            }
        }

        // ------------------------------------------------------------------ install

        private async Task InstallAsync()
        {
            if (gameDir == null || busy) return;
            while (ModInstaller.GameRunning())
            {
                if (MessageBox.Show(this, SetupText.T("game_running", "Chainsaw Warrior is running. Please close the game, then press Retry."), Text, MessageBoxButtons.RetryCancel, MessageBoxIcon.Warning) != DialogResult.Retry) return;
            }
            ModInstaller inst = Installer;
            string tempZip = null, tempSetup = null;
            SetBusy(true);
            try
            {
                Stream package;
                Version version;
                bool online = !HasPayload || (latest != null && Versions.IsNewer(latest.Version, OwnVersion));
                if (online)
                {
                    if (latest == null) latest = await github.GetLatestAsync();
                    ReleaseAsset zip = latest != null ? latest.Find(AssetKind.Package) : null;
                    if (zip == null) throw new InvalidOperationException(SetupText.T("err_no_package", "No package to download was found on GitHub."));
                    Event(SetupText.F("ev_downloading", "Downloading {0}...", zip.Name));
                    progress.Value = 0;
                    progress.Visible = true;
                    tempZip = await github.DownloadAsync(zip, new Progress<int>(p => progress.Value = Math.Max(0, Math.Min(100, p))));
                    ReleaseAsset setup = latest.Find(AssetKind.Setup);
                    if (setup != null)
                    {
                        try
                        {
                            tempSetup = await github.DownloadAsync(setup, null);
                        }
                        catch (Exception)
                        {
                            tempSetup = null;
                        }
                    }
                    if (tempSetup == null)
                    {
                        // The setup is also at the top of the zip.
                        try
                        {
                            tempSetup = ModInstaller.ExtractSetup(tempZip);
                        }
                        catch (Exception)
                        {
                            tempSetup = null;
                        }
                    }
                    package = File.OpenRead(tempZip);
                    version = latest.Version;
                }
                else
                {
                    package = Assembly.GetExecutingAssembly().GetManifestResourceStream("payload.zip");
                    version = OwnVersion;
                }
                Event(SetupText.F("ev_installing", "Installing Chainsaw Warrior Access {0}...", Versions.Show(version)));
                using (package)
                {
                    await Task.Run(() => inst.Install(package, Event));
                }
                string updater = inst.InstallUpdaterCopy(tempSetup ?? Application.ExecutablePath);
                Event(SetupText.F("ev_updater", "The setup was copied to {0}. Run it any time to check for updates.", updater));
                if (useLanguageCheck.Checked)
                {
                    inst.WriteLanguage(SetupText.Current);
                    Event(SetupText.F("ev_mod_language", "The mod will speak {0} in the game.", languageBox.SelectedItem));
                }
                Event(SetupText.F("ev_done", "Chainsaw Warrior Access {0} is installed.", Versions.Show(version)));
                SetBusy(false);
                bool start = startGameCheck.Checked;
                MessageBox.Show(this,
                    SetupText.F("done_message", "Chainsaw Warrior Access {0} is installed.", Versions.Show(version)) + "\n\n" +
                    (start ? SetupText.T("done_start", "The game starts when you close this message. After a few seconds it says \"Chainsaw Warrior Access loaded\". Press F1 in the game for help.")
                           : SetupText.T("done_nostart", "Start Chainsaw Warrior from Steam. In its settings window press Enter (Play). The game then says \"Chainsaw Warrior Access loaded\". Press F1 in the game for help.")),
                    SetupText.T("done_title", "Installation complete"), MessageBoxButtons.OK, MessageBoxIcon.Information);
                if (start) await StartGameAsync(closeAfter: true);
            }
            catch (UnauthorizedAccessException)
            {
                SetBusy(false);
                Event(SetupText.T("err_access", "Windows did not allow writing to the game folder."));
                OfferElevation();
            }
            catch (Exception ex)
            {
                SetBusy(false);
                Event(SetupText.F("err_install", "Installation failed: {0}", ex.Message));
                MessageBox.Show(this, SetupText.F("err_install", "Installation failed: {0}", ex.Message), Text, MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
            finally
            {
                progress.Visible = false;
                TryDelete(tempZip);
                TryDelete(tempSetup);
                UpdateButtons();
            }
        }

        private void OfferElevation()
        {
            if (MessageBox.Show(this, SetupText.T("ask_admin", "Restart setup as administrator?"), Text, MessageBoxButtons.YesNo, MessageBoxIcon.Warning) != DialogResult.Yes) return;
            try
            {
                string args = "--install --game \"" + gameDir + "\"";
                if (useLanguageCheck.Checked) args += " --mod-language " + SetupText.Current;
                if (startGameCheck.Checked) args += " --start-game";
                Process.Start(new ProcessStartInfo(Application.ExecutablePath, args) { Verb = "runas", UseShellExecute = true });
                Close();
            }
            catch (Exception ex)
            {
                Event(SetupText.F("err_admin", "Could not restart as administrator: {0}", ex.Message));
            }
        }

        private static void TryDelete(string path)
        {
            try
            {
                if (path != null && File.Exists(path)) File.Delete(path);
            }
            catch (Exception)
            {
                // A temporary file; Windows cleans the folder eventually.
            }
        }

        private void SetBusy(bool value)
        {
            busy = value;
            UseWaitCursor = value;
            UpdateButtons();
        }

        // ------------------------------------------------------------------ buttons

        private async void OnInstall(object sender, EventArgs e)
        {
            await InstallAsync();
        }

        private async void OnCheck(object sender, EventArgs e)
        {
            await CheckOnlineAsync(interactive: true);
        }

        private async void OnStartGame(object sender, EventArgs e)
        {
            await StartGameAsync(closeAfter: false);
        }

        private async Task StartGameAsync(bool closeAfter)
        {
            Event(SetupText.T("ev_starting", "Starting Chainsaw Warrior through Steam..."));
            SetBusy(true);
            bool pressed = await Task.Run(() => ModInstaller.StartGame(pressPlay: true));
            SetBusy(false);
            Event(pressed ? SetupText.T("ev_play_pressed", "Pressed Play in the game's settings window. The game is starting.")
                          : SetupText.T("ev_play_not_found", "The game's settings window did not appear; if it does, press Enter for Play."));
            if (closeAfter) Close();
        }

        private void OnChooseFolder(object sender, EventArgs e)
        {
            using (var dlg = new FolderBrowserDialog { Description = SetupText.T("folder_desc", "Select the Chainsaw Warrior folder (it contains CWSteamPC.exe)"), ShowNewFolderButton = false })
            {
                if (gameDir != null) dlg.SelectedPath = gameDir;
                if (dlg.ShowDialog(this) != DialogResult.OK) return;
                if (!ModInstaller.IsGameFolder(dlg.SelectedPath))
                {
                    Event(SetupText.T("err_not_game", "That folder does not contain CWSteamPC.exe."));
                    return;
                }
                gameDir = dlg.SelectedPath;
                FillLanguages();
                DescribeState();
                UpdateButtons();
            }
        }

        private void OnUninstall(object sender, EventArgs e)
        {
            if (gameDir == null) return;
            if (ModInstaller.GameRunning())
            {
                Event(SetupText.T("close_game_first", "Please close Chainsaw Warrior first."));
                return;
            }
            if (MessageBox.Show(this, SetupText.T("ask_uninstall", "Remove Chainsaw Warrior Access from the game?"), SetupText.T("btn_uninstall_title", "Uninstall"), MessageBoxButtons.YesNo, MessageBoxIcon.Question) != DialogResult.Yes) return;
            bool keep = MessageBox.Show(this, SetupText.T("ask_keep_settings", "Keep your mod settings for a later install?"), SetupText.T("btn_uninstall_title", "Uninstall"), MessageBoxButtons.YesNo, MessageBoxIcon.Question) == DialogResult.Yes;
            try
            {
                Installer.Uninstall(keep, Event);
            }
            catch (Exception ex)
            {
                Event(SetupText.F("err_uninstall", "Uninstall failed: {0}", ex.Message));
            }
            UpdateButtons();
        }

        private void OnReadme(object sender, EventArgs e)
        {
            string local = gameDir != null ? Path.Combine(gameDir, ModInstaller.ModFolder, "CWAccess-README.md") : null;
            try
            {
                if (local != null && File.Exists(local)) Process.Start(new ProcessStartInfo("notepad.exe", "\"" + local + "\""));
                else Process.Start(new ProcessStartInfo("https://github.com/" + GitHubClient.Owner + "/" + GitHubClient.Repo + "#readme") { UseShellExecute = true });
            }
            catch (Exception ex)
            {
                Event(SetupText.F("err_readme", "Could not open the read me: {0}", ex.Message));
            }
        }
    }
}
