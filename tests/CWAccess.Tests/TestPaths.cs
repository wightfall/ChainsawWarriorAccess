using System;
using System.IO;

// Several tests switch the mod's language and the game's text table, which are static: run them one at a time.
[assembly: Xunit.CollectionBehavior(DisableTestParallelization = true)]

namespace CWAccess.Tests
{
    internal static class TestPaths
    {
        /// <summary>The repository root (the folder with ChainsawWarriorAccess.sln).</summary>
        public static string Root
        {
            get
            {
                string dir = AppDomain.CurrentDomain.BaseDirectory;
                while (dir != null && !File.Exists(Path.Combine(dir, "ChainsawWarriorAccess.sln"))) dir = Path.GetDirectoryName(dir);
                return dir ?? throw new InvalidOperationException("Repository root not found");
            }
        }

        public static string ModSource => Path.Combine(Root, "src", "CWAccess");

        public static string InstallerSource => Path.Combine(Root, "src", "CWAccess.Installer");

        public static string Thai => Path.Combine(ModSource, "Languages", "th.txt");

        /// <summary>The game's Managed folder (from Directory.Build.props or CWACCESS_GAME_DIR).</summary>
        public static string Managed
        {
            get
            {
                string game = Environment.GetEnvironmentVariable("CWACCESS_GAME_DIR") ?? @"C:\Program Files (x86)\Steam\steamapps\common\ChainsawWarrior";
                return Path.Combine(game, "CWSteamPC_Data", "Managed");
            }
        }

        /// <summary>The game's own Assembly-CSharp.dll: the installer's backup when the game is set up for the mod.</summary>
        public static string OriginalAssembly
        {
            get
            {
                string backup = Path.Combine(Managed, "Assembly-CSharp.dll.cwaccess-original");
                return File.Exists(backup) ? backup : Path.Combine(Managed, "Assembly-CSharp.dll");
            }
        }

        public static string NewTempDir()
        {
            string d = Path.Combine(Path.GetTempPath(), "cwaccess-test-" + Guid.NewGuid().ToString("N"));
            Directory.CreateDirectory(d);
            return d;
        }
    }
}
