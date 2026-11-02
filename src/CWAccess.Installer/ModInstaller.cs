using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.IO.Compression;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading;
using Mono.Cecil;
using Mono.Cecil.Cil;

namespace CWAccess.Installer
{
    /// <summary>
    /// Installs, updates and removes the mod in a Chainsaw Warrior folder.
    ///
    /// Unity 4 games cannot use the usual mod loaders, so installing adds one call at the start of the game's
    /// CUIManager.Initialise (in the player's own Assembly-CSharp.dll) to CWAccess.Boot, which loads the mod from the
    /// CWAccess folder. The untouched file is kept as Assembly-CSharp.dll.cwaccess-original and put back on uninstall.
    /// </summary>
    public sealed class ModInstaller
    {
        public const string GameExe = "CWSteamPC.exe";
        public const string DataDir = "CWSteamPC_Data";
        public const string ModFolder = "CWAccess";
        public const string BootDll = "CWAccess.Boot.dll";
        public const string BackupSuffix = ".cwaccess-original";
        public const string SetupName = "CWAccess-Setup.exe";
        public const int SteamAppId = 251710;

        /// <summary>Files of the player that an update must not replace.</summary>
        private static readonly HashSet<string> Keep = new HashSet<string>(StringComparer.OrdinalIgnoreCase) { "CWAccess.ini", "strings.txt" };

        public ModInstaller(string gameDir)
        {
            GameDir = gameDir;
        }

        public string GameDir { get; }

        public string ModDir => Path.Combine(GameDir, ModFolder);

        public string ManagedDir => Path.Combine(GameDir, DataDir, "Managed");

        public string AssemblyPath => Path.Combine(ManagedDir, "Assembly-CSharp.dll");

        public string BackupPath => AssemblyPath + BackupSuffix;

        public string UpdaterPath => Path.Combine(ModDir, SetupName);

        public static bool IsGameFolder(string dir)
        {
            return !string.IsNullOrEmpty(dir) && File.Exists(Path.Combine(dir, GameExe)) && Directory.Exists(Path.Combine(dir, DataDir));
        }

        /// <summary>The version of the installed mod (CWAccess.dll), or null.</summary>
        public Version InstalledVersion
        {
            get
            {
                string dll = Path.Combine(ModDir, "CWAccess.dll");
                if (!File.Exists(dll)) return null;
                try
                {
                    return Versions.Normalize(new Version(FileVersionInfo.GetVersionInfo(dll).FileVersion));
                }
                catch (Exception)
                {
                    return null;
                }
            }
        }

        public bool IsPatched
        {
            get
            {
                if (!File.Exists(AssemblyPath)) return false;
                return Patcher.IsPatched(AssemblyPath, ManagedDir);
            }
        }

        public static bool GameRunning()
        {
            return Process.GetProcessesByName(Path.GetFileNameWithoutExtension(GameExe)).Length > 0;
        }

        /// <summary>
        /// Extracts a release package (a zip whose files are in a CWAccess folder) into the game, then sets up the
        /// loader. The player's settings are kept. Returns the number of files written.
        /// </summary>
        public int Install(Stream package, Action<string> report)
        {
            report = report ?? (s => { });
            Directory.CreateDirectory(ModDir);
            int files = 0;
            string running = Process.GetCurrentProcess().MainModule.FileName;
            using (var zip = new ZipArchive(package, ZipArchiveMode.Read))
            {
                foreach (ZipArchiveEntry e in zip.Entries)
                {
                    string name = e.FullName.Replace('\\', '/');
                    if (!name.StartsWith(ModFolder + "/", StringComparison.OrdinalIgnoreCase) || name.EndsWith("/")) continue;
                    string rel = name.Substring(ModFolder.Length + 1);
                    string dest = Path.GetFullPath(Path.Combine(ModDir, rel.Replace('/', Path.DirectorySeparatorChar)));
                    // Never write outside the mod folder.
                    if (!dest.StartsWith(Path.GetFullPath(ModDir) + Path.DirectorySeparatorChar, StringComparison.OrdinalIgnoreCase)) continue;
                    if (Keep.Contains(Path.GetFileName(dest)) && File.Exists(dest)) continue;
                    Directory.CreateDirectory(Path.GetDirectoryName(dest));
                    if (string.Equals(dest, running, StringComparison.OrdinalIgnoreCase)) MoveAside(dest);
                    e.ExtractToFile(dest, true);
                    files++;
                }
            }
            report(string.Format(SetupText.T("ev_files", "Copied {0} files to {1}."), files, ModDir));
            SetUpLoader(report);
            return files;
        }

        /// <summary>Copies CWAccess.Boot into the game and adds its call to the game's code (once).</summary>
        public void SetUpLoader(Action<string> report)
        {
            report = report ?? (s => { });
            string boot = Path.Combine(ModDir, BootDll);
            if (!File.Exists(boot)) throw new FileNotFoundException(BootDll + " is missing from " + ModDir);
            File.Copy(boot, Path.Combine(ManagedDir, BootDll), true);
            if (IsPatched)
            {
                report(SetupText.T("ev_already_patched", "The game was already set up for the mod."));
                return;
            }
            // The file on disk is the game's own: keep it (again, in case Steam updated the game).
            File.Copy(AssemblyPath, BackupPath, true);
            Patcher.Patch(AssemblyPath, ManagedDir);
            report(SetupText.T("ev_patched", "Added the mod's loader to the game. The original file is kept as Assembly-CSharp.dll.cwaccess-original."));
        }

        /// <summary>Copies a setup program into the mod folder as the updater (the running one is renamed first).</summary>
        public string InstallUpdaterCopy(string setupExe)
        {
            Directory.CreateDirectory(ModDir);
            if (!string.Equals(Path.GetFullPath(setupExe), Path.GetFullPath(UpdaterPath), StringComparison.OrdinalIgnoreCase))
            {
                if (File.Exists(UpdaterPath) && string.Equals(Process.GetCurrentProcess().MainModule.FileName, UpdaterPath, StringComparison.OrdinalIgnoreCase)) MoveAside(UpdaterPath);
                File.Copy(setupExe, UpdaterPath, true);
            }
            return UpdaterPath;
        }

        /// <summary>A running program cannot be overwritten but can be renamed; the .old copy is removed next start.</summary>
        private static void MoveAside(string path)
        {
            string old = path + ".old";
            if (File.Exists(old)) File.Delete(old);
            File.Move(path, old);
        }

        /// <summary>Puts the game's original file back and removes the mod (its settings too when keepSettings is false).</summary>
        public void Uninstall(bool keepSettings, Action<string> report)
        {
            report = report ?? (s => { });
            if (IsPatched)
            {
                if (!File.Exists(BackupPath)) throw new FileNotFoundException(SetupText.T("err_no_backup", "The backup of Assembly-CSharp.dll is missing. Use Steam's Verify integrity of game files instead."));
                File.Copy(BackupPath, AssemblyPath, true);
                report(SetupText.T("ev_restored", "Restored the game's original Assembly-CSharp.dll."));
            }
            if (File.Exists(BackupPath)) File.Delete(BackupPath);
            string boot = Path.Combine(ManagedDir, BootDll);
            if (File.Exists(boot)) File.Delete(boot);
            if (Directory.Exists(ModDir))
            {
                string running = Process.GetCurrentProcess().MainModule.FileName;
                foreach (string f in Directory.GetFiles(ModDir, "*", SearchOption.AllDirectories))
                {
                    string name = Path.GetFileName(f);
                    if (keepSettings && Keep.Contains(name)) continue;
                    if (string.Equals(f, running, StringComparison.OrdinalIgnoreCase)) continue;
                    try
                    {
                        File.Delete(f);
                    }
                    catch (IOException)
                    {
                        // In use (the speech helper of a running game); left behind.
                    }
                }
                foreach (string d in Directory.GetDirectories(ModDir, "*", SearchOption.AllDirectories).OrderByDescending(d => d.Length))
                {
                    if (Directory.GetFileSystemEntries(d).Length == 0) Directory.Delete(d);
                }
                if (Directory.GetFileSystemEntries(ModDir).Length == 0) Directory.Delete(ModDir);
            }
            report(SetupText.T("ev_uninstalled", "The mod was removed from the game."));
        }

        /// <summary>Writes "Language = code" into the mod's settings file (created when missing).</summary>
        public void WriteLanguage(string code)
        {
            Directory.CreateDirectory(ModDir);
            string ini = Path.Combine(ModDir, "CWAccess.ini");
            var lines = File.Exists(ini) ? File.ReadAllLines(ini, Encoding.UTF8).ToList() : new List<string>();
            int i = lines.FindIndex(l => Regex.IsMatch(l, @"^\s*Language\s*="));
            string line = "Language = " + code;
            if (i >= 0) lines[i] = line;
            else lines.Add(line);
            File.WriteAllLines(ini, lines, new UTF8Encoding(false));
        }

        /// <summary>The mod's Language setting from CWAccess.ini, or null.</summary>
        public string ReadLanguage()
        {
            string ini = Path.Combine(ModDir, "CWAccess.ini");
            if (!File.Exists(ini)) return null;
            foreach (string l in File.ReadAllLines(ini, Encoding.UTF8))
            {
                Match m = Regex.Match(l, @"^\s*Language\s*=\s*(\S+)\s*$");
                if (m.Success) return m.Groups[1].Value;
            }
            return null;
        }

        // ------------------------------------------------------------------ finding the game

        public static string FindGame(IEnumerable<string> extra = null)
        {
            var candidates = new List<string>();
            if (extra != null) candidates.AddRange(extra);
            foreach (string lib in SteamLibraries()) candidates.Add(Path.Combine(lib, "steamapps", "common", "ChainsawWarrior"));
            foreach (string c in candidates)
            {
                if (IsGameFolder(c)) return Path.GetFullPath(c);
            }
            return null;
        }

        public static IEnumerable<string> SteamLibraries()
        {
            var roots = new List<string>
            {
                Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ProgramFilesX86), "Steam"),
                Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ProgramFiles), "Steam"),
            };
            var result = new List<string>(roots);
            foreach (string root in roots)
            {
                string vdf = Path.Combine(root, "steamapps", "libraryfolders.vdf");
                if (File.Exists(vdf)) result.AddRange(LibraryPaths(File.ReadAllText(vdf)));
            }
            return result.Distinct(StringComparer.OrdinalIgnoreCase);
        }

        /// <summary>The "path" entries of Steam's libraryfolders.vdf.</summary>
        public static IEnumerable<string> LibraryPaths(string vdf)
        {
            foreach (Match m in Regex.Matches(vdf ?? string.Empty, "\"path\"\\s+\"([^\"]+)\""))
            {
                yield return m.Groups[1].Value.Replace("\\\\", "\\");
            }
        }

        // ------------------------------------------------------------------ starting the game

        [DllImport("user32.dll")]
        private static extern bool EnumChildWindows(IntPtr parent, EnumProc callback, IntPtr param);

        [DllImport("user32.dll", CharSet = CharSet.Unicode)]
        private static extern int GetWindowText(IntPtr hwnd, StringBuilder text, int max);

        [DllImport("user32.dll")]
        private static extern IntPtr PostMessage(IntPtr hwnd, uint msg, IntPtr w, IntPtr l);

        private delegate bool EnumProc(IntPtr hwnd, IntPtr param);

        /// <summary>
        /// Starts the game through Steam. When pressPlay is set, waits up to a minute for Unity's "Chainsaw Warrior
        /// Configuration" window and presses its Play! button (that window shows its buttons to screen readers only
        /// as unnamed panes). Returns true when Play was pressed.
        /// </summary>
        public static bool StartGame(bool pressPlay)
        {
            Process.Start(new ProcessStartInfo("steam://rungameid/" + SteamAppId) { UseShellExecute = true });
            if (!pressPlay) return false;
            for (int i = 0; i < 120; i++)
            {
                Thread.Sleep(500);
                foreach (Process p in Process.GetProcessesByName(Path.GetFileNameWithoutExtension(GameExe)))
                {
                    p.Refresh();
                    if (p.MainWindowHandle == IntPtr.Zero || !p.MainWindowTitle.Contains("Configuration")) continue;
                    IntPtr play = IntPtr.Zero;
                    EnumChildWindows(p.MainWindowHandle, (h, l) =>
                    {
                        var sb = new StringBuilder(64);
                        GetWindowText(h, sb, 64);
                        if (sb.ToString().StartsWith("Play")) play = h;
                        return true;
                    }, IntPtr.Zero);
                    if (play != IntPtr.Zero)
                    {
                        PostMessage(play, 0x00F5, IntPtr.Zero, IntPtr.Zero); // BM_CLICK
                        return true;
                    }
                }
            }
            return false;
        }
    }

    /// <summary>Adds / finds the call to CWAccess.Boot in the game's Assembly-CSharp.dll.</summary>
    public static class Patcher
    {
        private static ReaderParameters Reader(string managed)
        {
            var resolver = new DefaultAssemblyResolver();
            resolver.AddSearchDirectory(managed);
            return new ReaderParameters { AssemblyResolver = resolver, ReadingMode = ReadingMode.Immediate, InMemory = true };
        }

        private static MethodDefinition HookTarget(ModuleDefinition module)
        {
            TypeDefinition ui = module.GetType("CUIManager") ?? throw new InvalidOperationException("CUIManager not found: unknown game version.");
            return ui.Methods.FirstOrDefault(m => m.Name == "Initialise" && m.IsStatic && m.Parameters.Count == 1)
                   ?? throw new InvalidOperationException("CUIManager.Initialise not found: unknown game version.");
        }

        public static bool IsPatched(string assembly, string managed)
        {
            using (ModuleDefinition module = ModuleDefinition.ReadModule(assembly, Reader(managed)))
            {
                MethodDefinition m = HookTarget(module);
                return m.Body.Instructions.Take(3).Any(i => i.OpCode == OpCodes.Call && i.Operand is MethodReference r && r.DeclaringType.FullName == "CWAccess.Boot.Loader");
            }
        }

        public static void Patch(string assembly, string managed)
        {
            string temp = assembly + ".cwaccess-new";
            using (ModuleDefinition module = ModuleDefinition.ReadModule(assembly, Reader(managed)))
            using (ModuleDefinition boot = ModuleDefinition.ReadModule(Path.Combine(managed, ModInstaller.BootDll), Reader(managed)))
            {
                MethodDefinition start = boot.GetType("CWAccess.Boot.Loader").Methods.First(m => m.Name == "Start");
                MethodReference call = module.ImportReference(start);
                MethodDefinition hook = HookTarget(module);
                ILProcessor il = hook.Body.GetILProcessor();
                il.InsertBefore(hook.Body.Instructions[0], il.Create(OpCodes.Call, call));
                module.Write(temp);
            }
            File.Copy(temp, assembly, true);
            File.Delete(temp);
        }
    }
}
