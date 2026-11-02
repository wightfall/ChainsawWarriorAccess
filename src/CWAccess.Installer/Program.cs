using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.RegularExpressions;
using Mono.Cecil;
using Mono.Cecil.Cil;

namespace CWAccess.Installer
{
    /// <summary>
    /// Installs or removes Chainsaw Warrior Access.
    ///
    /// Unity 4 games cannot use the usual mod loaders, so the setup adds one line to the start of the game's
    /// CUIManager.Initialise (in your own copy of Assembly-CSharp.dll): a call to CWAccess.Boot, which loads the
    /// mod from the CWAccess folder. The untouched file is kept as Assembly-CSharp.dll.cwaccess-original and put
    /// back by --uninstall. If Steam updates or verifies the game, run the setup again.
    ///
    /// Usage: CWAccess-Setup.exe [game folder] [--uninstall] [--quiet]
    /// </summary>
    internal static class Program
    {
        private const string GameExe = "CWSteamPC.exe";
        private const string DataDir = "CWSteamPC_Data";
        private const string BootDll = "CWAccess.Boot.dll";
        private const string BackupSuffix = ".cwaccess-original";

        private static bool quiet;

        private static int Main(string[] args)
        {
            AppDomain.CurrentDomain.AssemblyResolve += (s, e) =>
            {
                if (!e.Name.StartsWith("Mono.Cecil,")) return null;
                using (Stream st = typeof(Program).Assembly.GetManifestResourceStream("Mono.Cecil.dll"))
                {
                    var bytes = new byte[st.Length];
                    st.Read(bytes, 0, bytes.Length);
                    return System.Reflection.Assembly.Load(bytes);
                }
            };
            return Run(args);
        }

        private static int Run(string[] args)
        {
            quiet = args.Contains("--quiet");
            bool uninstall = args.Contains("--uninstall");
            string gameDir = args.FirstOrDefault(a => !a.StartsWith("--"));
            try
            {
                gameDir = FindGame(gameDir);
                if (gameDir == null)
                {
                    Say("Chainsaw Warrior was not found. Run this setup from the CWAccess folder inside the game folder, or give the game folder as the first argument.");
                    return Finish(2);
                }
                Say("Game folder: " + gameDir);
                if (uninstall) Uninstall(gameDir);
                else Install(gameDir);
                return Finish(0);
            }
            catch (Exception e)
            {
                Say("Setup failed: " + e.Message);
                Say("Close the game if it is running and try again.");
                return Finish(1);
            }
        }

        private static int Finish(int code)
        {
            if (!quiet)
            {
                Console.WriteLine();
                Console.WriteLine("Press Enter to close.");
                Console.ReadLine();
            }
            return code;
        }

        private static void Say(string text)
        {
            Console.WriteLine(text);
        }

        private static string FindGame(string given)
        {
            var candidates = new List<string>();
            if (!string.IsNullOrEmpty(given)) candidates.Add(given);
            string here = AppDomain.CurrentDomain.BaseDirectory.TrimEnd('\\', '/');
            candidates.Add(here);
            candidates.Add(Path.GetDirectoryName(here));
            foreach (string lib in SteamLibraries()) candidates.Add(Path.Combine(lib, "steamapps", "common", "ChainsawWarrior"));
            foreach (string c in candidates)
            {
                if (!string.IsNullOrEmpty(c) && File.Exists(Path.Combine(c, GameExe)) && Directory.Exists(Path.Combine(c, DataDir))) return Path.GetFullPath(c);
            }
            return null;
        }

        private static IEnumerable<string> SteamLibraries()
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
                if (!File.Exists(vdf)) continue;
                foreach (Match m in Regex.Matches(File.ReadAllText(vdf), "\"path\"\\s+\"([^\"]+)\""))
                {
                    result.Add(m.Groups[1].Value.Replace("\\\\", "\\"));
                }
            }
            return result;
        }

        private static void Install(string gameDir)
        {
            string managed = Path.Combine(gameDir, DataDir, "Managed");
            string modDir = Path.Combine(gameDir, "CWAccess");
            string source = AppDomain.CurrentDomain.BaseDirectory;

            // When the setup is not already in the game's CWAccess folder, copy the mod files there.
            if (!SamePath(source, modDir))
            {
                Directory.CreateDirectory(modDir);
                foreach (string f in Directory.GetFiles(source))
                {
                    File.Copy(f, Path.Combine(modDir, Path.GetFileName(f)), true);
                }
                Say("Copied the mod to " + modDir);
            }

            string bootSource = Path.Combine(modDir, BootDll);
            if (!File.Exists(bootSource)) throw new FileNotFoundException(BootDll + " is missing from " + modDir);
            File.Copy(bootSource, Path.Combine(managed, BootDll), true);

            string target = Path.Combine(managed, "Assembly-CSharp.dll");
            string backup = target + BackupSuffix;
            if (IsPatched(target, managed))
            {
                Say("The game is already set up for the mod. Files updated.");
                return;
            }
            // The file on disk is the game's own: keep it (again, in case Steam updated the game).
            File.Copy(target, backup, true);
            Patch(target, managed);
            Say("Installed. Start the game: it will say \"Chainsaw Warrior Access loaded\".");
        }

        private static void Uninstall(string gameDir)
        {
            string managed = Path.Combine(gameDir, DataDir, "Managed");
            string target = Path.Combine(managed, "Assembly-CSharp.dll");
            string backup = target + BackupSuffix;
            if (IsPatched(target, managed))
            {
                if (!File.Exists(backup)) throw new FileNotFoundException("The original Assembly-CSharp.dll backup is missing. Use Steam's \"Verify integrity of game files\" instead.");
                File.Copy(backup, target, true);
                Say("Restored the game's original Assembly-CSharp.dll.");
            }
            if (File.Exists(backup)) File.Delete(backup);
            string boot = Path.Combine(managed, BootDll);
            if (File.Exists(boot)) File.Delete(boot);
            Say("Uninstalled. You can delete the CWAccess folder now.");
        }

        private static bool SamePath(string a, string b)
        {
            return string.Equals(Path.GetFullPath(a).TrimEnd('\\'), Path.GetFullPath(b).TrimEnd('\\'), StringComparison.OrdinalIgnoreCase);
        }

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

        private static bool IsPatched(string target, string managed)
        {
            using (ModuleDefinition module = ModuleDefinition.ReadModule(target, Reader(managed)))
            {
                MethodDefinition m = HookTarget(module);
                return m.Body.Instructions.Take(3).Any(i => i.OpCode == OpCodes.Call && i.Operand is MethodReference r && r.DeclaringType.FullName == "CWAccess.Boot.Loader");
            }
        }

        private static void Patch(string target, string managed)
        {
            string temp = target + ".cwaccess-new";
            using (ModuleDefinition module = ModuleDefinition.ReadModule(target, Reader(managed)))
            using (ModuleDefinition boot = ModuleDefinition.ReadModule(Path.Combine(managed, BootDll), Reader(managed)))
            {
                MethodDefinition start = boot.GetType("CWAccess.Boot.Loader").Methods.First(m => m.Name == "Start");
                MethodReference call = module.ImportReference(start);
                MethodDefinition hook = HookTarget(module);
                ILProcessor il = hook.Body.GetILProcessor();
                il.InsertBefore(hook.Body.Instructions[0], il.Create(OpCodes.Call, call));
                module.Write(temp);
            }
            File.Copy(temp, target, true);
            File.Delete(temp);
        }
    }
}
