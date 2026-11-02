using System;
using System.IO;
using System.Reflection;

namespace CWAccess.Boot
{
    /// <summary>
    /// The only code the installer adds a call to: the first line of CUIManager.Initialise, which the game runs
    /// at the start of every scene. Loads the mod from the CWAccess folder next to the game, once.
    /// Any failure is written to CWAccess\boot.log and the game carries on without the mod.
    /// </summary>
    public static class Loader
    {
        private static bool started;
        private static string modDir;

        public static void Start()
        {
            if (started) return;
            started = true;
            try
            {
                string dataDir = UnityEngine.Application.dataPath; // ...\ChainsawWarrior\CWSteamPC_Data
                modDir = Path.Combine(Path.GetDirectoryName(dataDir), "CWAccess");
                AppDomain.CurrentDomain.AssemblyResolve += Resolve;
                Assembly mod = Assembly.LoadFrom(Path.Combine(modDir, "CWAccess.dll"));
                Type entry = mod.GetType("CWAccess.Entry", true);
                entry.GetMethod("Init", BindingFlags.Public | BindingFlags.Static).Invoke(null, new object[] { modDir });
            }
            catch (Exception e)
            {
                Write("CWAccess failed to start: " + e);
            }
        }

        private static Assembly Resolve(object sender, ResolveEventArgs args)
        {
            try
            {
                string name = new AssemblyName(args.Name).Name;
                string path = Path.Combine(modDir, name + ".dll");
                return File.Exists(path) ? Assembly.LoadFrom(path) : null;
            }
            catch (Exception)
            {
                return null;
            }
        }

        private static void Write(string text)
        {
            try
            {
                if (modDir != null && Directory.Exists(modDir)) File.AppendAllText(Path.Combine(modDir, "boot.log"), DateTime.Now + " " + text + Environment.NewLine);
                UnityEngine.Debug.LogError(text);
            }
            catch (Exception)
            {
                // Nowhere left to report to.
            }
        }
    }
}
