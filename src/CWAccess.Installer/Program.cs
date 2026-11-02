using System;
using System.IO;
using System.Reflection;
using System.Windows.Forms;

namespace CWAccess.Installer
{
    /// <summary>
    /// Command line: [--install] [--uninstall] [--quiet] [--game "folder"] [--mod-language code] [--start-game].
    /// --quiet works without a window: installs the built-in package (or uninstalls with --uninstall).
    /// </summary>
    public sealed class Options
    {
        public bool InstallNow;
        public bool Uninstall;
        public bool Quiet;
        public bool StartGame;
        public string GameDir;
        public string ModLanguage;

        public static Options Parse(string[] args)
        {
            var o = new Options();
            for (int i = 0; i < args.Length; i++)
            {
                string a = args[i].ToLowerInvariant();
                // Windows style "/install" and "-install" mean the same as "--install".
                if (a.StartsWith("/")) a = "--" + a.Substring(1);
                else if (a.StartsWith("-") && !a.StartsWith("--") && a.Length > 2) a = "-" + a;
                if (a == "--install") o.InstallNow = true;
                else if (a == "--uninstall") o.Uninstall = true;
                else if (a == "--quiet" || a == "--s" || a == "-s") o.Quiet = true;
                else if (a == "--start-game") o.StartGame = true;
                else if (a == "--game" && i + 1 < args.Length) o.GameDir = args[++i].Trim('"');
                else if (a == "--mod-language" && i + 1 < args.Length) o.ModLanguage = args[++i].Trim('"');
            }
            return o;
        }
    }

    internal static class Program
    {
        [STAThread]
        private static int Main(string[] args)
        {
            // Mono.Cecil is embedded: the game's CWAccess folder holds an older Cecil that the mod's Harmony needs.
            AppDomain.CurrentDomain.AssemblyResolve += (s, e) =>
            {
                if (!e.Name.StartsWith("Mono.Cecil,")) return null;
                using (Stream st = typeof(Program).Assembly.GetManifestResourceStream("Mono.Cecil.dll"))
                {
                    var bytes = new byte[st.Length];
                    st.Read(bytes, 0, bytes.Length);
                    return Assembly.Load(bytes);
                }
            };
            Options options = Options.Parse(args);
            if (options.Quiet) return Quiet(options);
            try
            {
                string exeDir = Path.GetDirectoryName(Application.ExecutablePath);
                string game = options.GameDir != null && ModInstaller.IsGameFolder(options.GameDir) ? options.GameDir : ModInstaller.FindGame(new[] { exeDir, Path.GetDirectoryName(exeDir) });
                SetupText.Init(game);
                if (options.ModLanguage != null) SetupText.Use(game, options.ModLanguage);
            }
            catch (Exception)
            {
                // English
            }
            Application.EnableVisualStyles();
            Application.SetCompatibleTextRenderingDefault(false);
            Application.Run(new MainForm(options));
            return 0;
        }

        /// <summary>Exit codes: 0 done, 2 game not found, 3 game running, 4 no built-in package, 1 other error.</summary>
        private static int Quiet(Options o)
        {
            try
            {
                string exeDir = Path.GetDirectoryName(Application.ExecutablePath);
                string game = o.GameDir != null && ModInstaller.IsGameFolder(o.GameDir) ? o.GameDir : ModInstaller.FindGame(new[] { exeDir, Path.GetDirectoryName(exeDir) });
                if (game == null) return 2;
                if (ModInstaller.GameRunning()) return 3;
                var inst = new ModInstaller(game);
                if (o.Uninstall)
                {
                    inst.Uninstall(keepSettings: true, report: Console.WriteLine);
                    return 0;
                }
                Stream payload = Assembly.GetExecutingAssembly().GetManifestResourceStream("payload.zip");
                if (payload != null)
                {
                    using (payload) inst.Install(payload, Console.WriteLine);
                    inst.InstallUpdaterCopy(Application.ExecutablePath);
                }
                else if (Directory.Exists(inst.ModDir))
                {
                    // Run from an extracted release zip: the files are already in place, only the loader is needed.
                    inst.SetUpLoader(Console.WriteLine);
                }
                else
                {
                    return 4;
                }
                if (o.ModLanguage != null) inst.WriteLanguage(o.ModLanguage);
                if (o.StartGame) ModInstaller.StartGame(pressPlay: true);
                return 0;
            }
            catch (Exception e)
            {
                Console.WriteLine(e.Message);
                return 1;
            }
        }
    }
}
