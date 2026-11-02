using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.IO.Compression;
using System.Linq;
using System.Text;
using CWAccess.Installer;
using Xunit;

namespace CWAccess.Tests
{
    public class VersionTests
    {
        [Theory]
        [InlineData("v1.2.3", "1.2.3.0")]
        [InlineData("1.2", "1.2.0.0")]
        [InlineData("CWAccess 0.1.0", "0.1.0.0")]
        [InlineData("7", "7.0.0.0")]
        public void ParsesTags(string tag, string expected)
        {
            Assert.Equal(new Version(expected), Versions.Parse(tag));
        }

        [Fact]
        public void ParsesNothingAsNull()
        {
            Assert.Null(Versions.Parse("no digits"));
            Assert.Null(Versions.Parse(null));
        }

        [Fact]
        public void ComparesVersions()
        {
            Assert.True(Versions.IsNewer(new Version(0, 2), new Version(0, 1, 9)));
            Assert.False(Versions.IsNewer(new Version(1, 0), new Version(1, 0, 0, 0)));
            Assert.True(Versions.IsNewer(new Version(0, 1), null));
            Assert.False(Versions.IsNewer(null, new Version(1, 0)));
            Assert.Equal("0.1.0", Versions.Show(new Version(0, 1)));
        }
    }

    public class ReleaseTests
    {
        private const string Json = "[" +
            "{\"tag_name\":\"v0.3.0\",\"name\":\"draft\",\"draft\":true,\"prerelease\":false,\"body\":\"\",\"assets\":[]}," +
            "{\"tag_name\":\"v0.2.0\",\"name\":\"0.2\",\"draft\":false,\"prerelease\":false,\"body\":\"## New\\r\\n- **Thai** language\\r\\n- [link](http://x)\",\"assets\":[" +
            "{\"name\":\"ChainsawWarriorAccess-v0.2.0.zip\",\"browser_download_url\":\"http://a/zip\",\"size\":10}," +
            "{\"name\":\"CWAccess-Setup-v0.2.0.exe\",\"browser_download_url\":\"http://a/exe\",\"size\":20}]}," +
            "{\"tag_name\":\"v0.1.0\",\"name\":\"0.1\",\"draft\":false,\"prerelease\":false,\"body\":\"\",\"assets\":[]}]";

        [Fact]
        public void NewestSkipsDrafts()
        {
            Release r = ReleaseJson.Newest(ReleaseJson.ParseReleases(Json));
            Assert.Equal(new Version(0, 2, 0, 0), r.Version);
            Assert.Equal("http://a/zip", r.Find(AssetKind.Package).Url);
            Assert.Equal("http://a/exe", r.Find(AssetKind.Setup).Url);
        }

        [Fact]
        public void BrokenJsonIsEmpty()
        {
            Assert.Empty(ReleaseJson.ParseReleases("not json"));
        }

        [Fact]
        public void NotesBecomePlainLines()
        {
            List<string> lines = ReleaseNotes.Lines("## New\r\n\r\n- **Thai** language\r\n* [the link](http://x)\r\n---\r\nEnd");
            Assert.Equal(new[] { "New", "- Thai language", "- the link", "End" }, lines);
        }
    }

    public class SteamLibraryTests
    {
        [Fact]
        public void ReadsLibraryFolders()
        {
            string vdf = "\"libraryfolders\"\n{\n\t\"0\"\n\t{\n\t\t\"path\"\t\t\"C:\\\\Program Files (x86)\\\\Steam\"\n\t}\n\t\"1\"\n\t{\n\t\t\"path\"\t\t\"D:\\\\Games\"\n\t}\n}";
            Assert.Equal(new[] { @"C:\Program Files (x86)\Steam", @"D:\Games" }, ModInstaller.LibraryPaths(vdf).ToArray());
        }
    }

    /// <summary>Installs into a fake game folder holding a copy of the real game's Assembly-CSharp.dll.</summary>
    public sealed class ModInstallerTests : IDisposable
    {
        private readonly string game;
        private readonly byte[] original;

        public ModInstallerTests()
        {
            game = TestPaths.NewTempDir();
            string managed = Path.Combine(game, "CWSteamPC_Data", "Managed");
            Directory.CreateDirectory(managed);
            File.WriteAllText(Path.Combine(game, "CWSteamPC.exe"), string.Empty);
            foreach (string dll in new[] { "UnityEngine.dll", "Assembly-CSharp-firstpass.dll" }) File.Copy(Path.Combine(TestPaths.Managed, dll), Path.Combine(managed, dll));
            File.Copy(TestPaths.OriginalAssembly, Path.Combine(managed, "Assembly-CSharp.dll"));
            original = File.ReadAllBytes(Path.Combine(managed, "Assembly-CSharp.dll"));
        }

        public void Dispose()
        {
            try
            {
                Directory.Delete(game, true);
            }
            catch (IOException)
            {
                // A file still mapped by Cecil; the temp folder is cleaned by Windows.
            }
        }

        private static MemoryStream Package(params KeyValuePair<string, string>[] extra)
        {
            var ms = new MemoryStream();
            using (var zip = new ZipArchive(ms, ZipArchiveMode.Create, true))
            {
                ZipArchiveEntry boot = zip.CreateEntry("CWAccess/CWAccess.Boot.dll");
                using (Stream s = boot.Open())
                {
                    byte[] b = File.ReadAllBytes(typeof(CWAccess.Boot.Loader).Assembly.Location);
                    s.Write(b, 0, b.Length);
                }
                foreach (KeyValuePair<string, string> kv in extra)
                {
                    using (var w = new StreamWriter(zip.CreateEntry(kv.Key).Open(), Encoding.UTF8)) w.Write(kv.Value);
                }
            }
            ms.Position = 0;
            return ms;
        }

        [Fact]
        public void InstallPatchesOnceAndUninstallRestoresTheOriginal()
        {
            var inst = new ModInstaller(game);
            Assert.True(ModInstaller.IsGameFolder(game));
            Assert.False(inst.IsPatched);
            var log = new List<string>();
            inst.Install(Package(new KeyValuePair<string, string>("CWAccess/Languages/th.txt", "# Language: ไทย\n")), log.Add);
            Assert.True(inst.IsPatched);
            Assert.True(File.Exists(Path.Combine(game, "CWAccess", "Languages", "th.txt")));
            Assert.True(File.Exists(Path.Combine(inst.ManagedDir, "CWAccess.Boot.dll")));
            Assert.Equal(original, File.ReadAllBytes(inst.BackupPath));

            // A second install (an update) does not patch twice or replace the backup with a patched file.
            inst.Install(Package(), log.Add);
            Assert.True(inst.IsPatched);
            Assert.Equal(original, File.ReadAllBytes(inst.BackupPath));

            inst.Uninstall(keepSettings: false, report: log.Add);
            Assert.False(inst.IsPatched);
            Assert.Equal(original, File.ReadAllBytes(inst.AssemblyPath));
            Assert.False(File.Exists(inst.BackupPath));
            Assert.False(File.Exists(Path.Combine(inst.ManagedDir, "CWAccess.Boot.dll")));
            Assert.False(Directory.Exists(inst.ModDir));
        }

        [Fact]
        public void UpdatesKeepThePlayersSettings()
        {
            var inst = new ModInstaller(game);
            Directory.CreateDirectory(inst.ModDir);
            File.WriteAllText(Path.Combine(inst.ModDir, "CWAccess.ini"), "AnnounceDice = False\n");
            inst.Install(Package(new KeyValuePair<string, string>("CWAccess/CWAccess.ini", "AnnounceDice = True\n")), null);
            Assert.Contains("False", File.ReadAllText(Path.Combine(inst.ModDir, "CWAccess.ini")));
            inst.Uninstall(keepSettings: true, report: null);
            Assert.True(File.Exists(Path.Combine(inst.ModDir, "CWAccess.ini")));
        }

        [Fact]
        public void PackagesCannotWriteOutsideTheModFolder()
        {
            var inst = new ModInstaller(game);
            inst.Install(Package(new KeyValuePair<string, string>("CWAccess/../evil.txt", "x"), new KeyValuePair<string, string>("Other/file.txt", "x")), null);
            Assert.False(File.Exists(Path.Combine(game, "evil.txt")));
            Assert.False(File.Exists(Path.Combine(game, "Other", "file.txt")));
        }

        // The update that 0.1.0 and 0.2.0 could not finish: the setup in the game folder downloads a release, the
        // zip replaces CWAccess-Setup.exe (the running setup is renamed to .old), then the downloaded setup is copied
        // there too. The second copy renamed the file again and failed deleting the running .old copy.
        [Fact]
        public void TheSetupInTheGameFolderUpdatesItself()
        {
            var inst = new ModInstaller(game);
            Process running = RunningCopy.StartAt(inst.UpdaterPath);
            try
            {
                inst.RunningProgram = inst.UpdaterPath;
                inst.Install(Package(new KeyValuePair<string, string>("CWAccess/CWAccess-Setup.exe", "setup from the zip")), null);
                Assert.Equal("setup from the zip", File.ReadAllText(inst.UpdaterPath));
                string downloaded = Path.Combine(game, "CWAccess-Setup-v9.exe");
                File.WriteAllText(downloaded, "downloaded setup");
                inst.InstallUpdaterCopy(downloaded);
                Assert.Equal("downloaded setup", File.ReadAllText(inst.UpdaterPath));
                Assert.True(File.Exists(inst.UpdaterPath + ".old"));
            }
            finally
            {
                RunningCopy.Stop(running);
            }
            RunningCopy.RemoveOldCopies(inst.UpdaterPath);
            Assert.False(File.Exists(inst.UpdaterPath + ".old"));
        }

        [Fact]
        public void AnEarlierSetupStillRunningDoesNotStopAnUpdate()
        {
            var inst = new ModInstaller(game);
            Process earlier = RunningCopy.StartAt(inst.UpdaterPath + ".old");
            Process running = RunningCopy.StartAt(inst.UpdaterPath);
            try
            {
                inst.RunningProgram = inst.UpdaterPath;
                ModInstaller.RemoveOldCopies(inst.UpdaterPath);
                Assert.True(File.Exists(inst.UpdaterPath + ".old"));
                string downloaded = Path.Combine(game, "CWAccess-Setup-v9.exe");
                File.WriteAllText(downloaded, "downloaded setup");
                inst.InstallUpdaterCopy(downloaded);
                Assert.Equal("downloaded setup", File.ReadAllText(inst.UpdaterPath));
                Assert.Equal(2, Directory.GetFiles(inst.ModDir, "*.old").Length);
            }
            finally
            {
                RunningCopy.Stop(running);
                RunningCopy.Stop(earlier);
            }
            RunningCopy.RemoveOldCopies(inst.UpdaterPath);
            Assert.Empty(Directory.GetFiles(inst.ModDir, "*.old"));
        }

        [Fact]
        public void TheSetupAtTheTopOfTheZipIsNotCopiedIntoTheMod()
        {
            var inst = new ModInstaller(game);
            string zipPath = Path.Combine(game, "release.zip");
            using (MemoryStream ms = Package(new KeyValuePair<string, string>("CWAccess-Setup.exe", "setup at the top")))
            {
                File.WriteAllBytes(zipPath, ms.ToArray());
            }
            using (Stream zip = File.OpenRead(zipPath))
            {
                inst.Install(zip, null);
            }
            Assert.False(File.Exists(inst.UpdaterPath));
            Assert.False(File.Exists(Path.Combine(game, "CWAccess-Setup.exe")));
            string setup = ModInstaller.ExtractSetup(zipPath);
            try
            {
                Assert.Equal("setup at the top", File.ReadAllText(setup));
            }
            finally
            {
                File.Delete(setup);
            }
            File.Delete(zipPath);
            using (MemoryStream ms = Package())
            {
                File.WriteAllBytes(zipPath, ms.ToArray());
            }
            Assert.Null(ModInstaller.ExtractSetup(zipPath));
        }

        [Fact]
        public void WritesAndReadsTheModLanguage()
        {
            var inst = new ModInstaller(game);
            Directory.CreateDirectory(inst.ModDir);
            File.WriteAllText(Path.Combine(inst.ModDir, "CWAccess.ini"), "# comment\nLanguage = auto\nSoundCues = True\n");
            inst.WriteLanguage("th");
            Assert.Equal("th", inst.ReadLanguage());
            Assert.Contains("SoundCues = True", File.ReadAllText(Path.Combine(inst.ModDir, "CWAccess.ini")));
        }

        [Fact]
        public void SetupTextsComeFromTheGameFolder()
        {
            string langs = Path.Combine(game, "CWAccess", "Languages");
            Directory.CreateDirectory(langs);
            File.Copy(TestPaths.Thai, Path.Combine(langs, "th.txt"));
            try
            {
                Assert.Contains(SetupText.Available(game), c => c.Code == "th" && c.Name.Contains("Thai"));
                Assert.True(SetupText.Use(game, "th"));
                Assert.Equal("&ปิด", SetupText.T("btn_close", "Cl&ose"));
                Assert.Equal("Fallback", SetupText.T("no_such_key", "Fallback"));
                Assert.False(SetupText.Use(game, "zz"));
                Assert.Equal("th", SetupText.Current);
            }
            finally
            {
                SetupText.Use(game, "en");
            }
        }

        [Fact]
        public void UninstallWithoutBackupRefuses()
        {
            var inst = new ModInstaller(game);
            inst.Install(Package(), null);
            File.Delete(inst.BackupPath);
            Assert.Throws<FileNotFoundException>(() => inst.Uninstall(true, null));
        }
    }

    /// <summary>
    /// Runs a small program from a given file, as Windows runs the setup: while it runs, the file can be renamed but
    /// not deleted or overwritten.
    /// </summary>
    internal static class RunningCopy
    {
        private static string program;

        public static Process StartAt(string path)
        {
            if (program == null)
            {
                string exe = Path.Combine(TestPaths.NewTempDir(), "wait.exe");
                using (var csc = new Microsoft.CSharp.CSharpCodeProvider())
                {
                    var options = new System.CodeDom.Compiler.CompilerParameters { GenerateExecutable = true, OutputAssembly = exe };
                    var result = csc.CompileAssemblyFromSource(options, "class P { static void Main() { System.Threading.Thread.Sleep(120000); } }");
                    Assert.False(result.Errors.HasErrors, result.Errors.Count > 0 ? result.Errors[0].ToString() : null);
                }
                program = exe;
            }
            Directory.CreateDirectory(Path.GetDirectoryName(path));
            File.Copy(program, path, true);
            return Process.Start(new ProcessStartInfo(path) { CreateNoWindow = true, UseShellExecute = false });
        }

        /// <summary>Windows and virus scanners can hold the file of a stopped program for a moment.</summary>
        public static void RemoveOldCopies(string updater)
        {
            for (int i = 0; i < 50; i++)
            {
                ModInstaller.RemoveOldCopies(updater);
                if (Directory.GetFiles(Path.GetDirectoryName(updater), "*.old").Length == 0) return;
                System.Threading.Thread.Sleep(100);
            }
        }

        public static void Stop(Process p)
        {
            try
            {
                if (!p.HasExited) p.Kill();
                p.WaitForExit(10000);
            }
            catch (InvalidOperationException)
            {
                // Already gone.
            }
        }
    }

    public class OptionsTests
    {
        [Fact]
        public void ParsesTheCommandLine()
        {
            Options o = Options.Parse(new[] { "--quiet", "/install", "--game", "\"D:\\G\"", "--mod-language", "th", "--start-game" });
            Assert.True(o.Quiet);
            Assert.True(o.InstallNow);
            Assert.True(o.StartGame);
            Assert.Equal(@"D:\G", o.GameDir);
            Assert.Equal("th", o.ModLanguage);
        }
    }
}
