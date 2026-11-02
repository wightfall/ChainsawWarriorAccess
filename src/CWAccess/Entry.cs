using System;
using CWAccess.Speech;
using HarmonyLib;
using UnityEngine;

namespace CWAccess
{
    /// <summary>Started by CWAccess.Boot when the game opens its first screen.</summary>
    public static class Entry
    {
        public const string Version = "0.1.0";

        internal static string ModDir;
        private static bool initialised;

        public static void Init(string modDir)
        {
            if (initialised) return;
            initialised = true;
            ModDir = modDir;
            Log.Init(modDir);
            Log.Info("Chainsaw Warrior Access " + Version + " starting, Unity " + Application.unityVersion);
            Settings.Load(modDir);
            Loc.LoadOverrides(modDir);
            Speaker.Initialize(modDir);
            try
            {
                new Harmony("cwaccess.chainsawwarrior").PatchAll(typeof(Entry).Assembly);
                Log.Info("Harmony patches applied");
            }
            catch (Exception e)
            {
                Log.Error("Patching failed: " + e);
            }
            var go = new GameObject("CWAccess");
            UnityEngine.Object.DontDestroyOnLoad(go);
            go.AddComponent<ModBehaviour>();
            Speaker.Queue(Loc.Get("mod_loaded"));
        }
    }

    /// <summary>Runs the mod every frame, in every scene.</summary>
    internal sealed class ModBehaviour : MonoBehaviour
    {
        private void Update()
        {
            try
            {
                Speaker.Tick(Time.realtimeSinceStartup);
                AccessController.Update();
            }
            catch (Exception e)
            {
                Log.Error("Update: " + e);
            }
        }

        private void OnApplicationQuit()
        {
            Settings.Save();
            Speaker.Shutdown();
            Log.Close();
        }
    }
}
