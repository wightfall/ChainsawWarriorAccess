using System;
using CWAccess.Speech;
using HarmonyLib;
using UnityEngine;

namespace CWAccess
{
    /// <summary>Started by CWAccess.Boot when the game opens its first screen.</summary>
    public static class Entry
    {
        public const string Version = "0.3.3";

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
            Loc.Init(modDir, Settings.Language.Value);
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
            Audio.CuePlayer.Init(go);
            Speaker.Queue(Loc.Get("mod_loaded"));
        }
    }

    /// <summary>Runs the mod every frame, in every scene.</summary>
    internal sealed class ModBehaviour : MonoBehaviour
    {
        private int scene = -1;
        private float collectAt;

        private void Update()
        {
            long started = Perf.Measuring ? Perf.Now() : 0;
            try
            {
                // Each new scene (menu, comic, game): collect its texts for the translation template a moment later,
                // when its screens have set their labels.
                int level = Application.loadedLevel;
                if (level != scene)
                {
                    scene = level;
                    collectAt = Time.realtimeSinceStartup + 2f;
                }
                if (collectAt > 0f && Time.realtimeSinceStartup >= collectAt)
                {
                    collectAt = 0f;
                    Game.GameText.CollectScene();
                }
                Speaker.Tick(Time.realtimeSinceStartup);
                // A language file in use was saved: it is already reloaded; say so.
                if (Loc.WatchFiles(Time.realtimeSinceStartup)) Speaker.Queue(Loc.Get("lang_reloaded", Loc.LanguageName));
                AccessController.Update();
            }
            catch (Exception e)
            {
                Log.Error("Update: " + e);
            }
            if (started != 0) Perf.Frame(started, Time.realtimeSinceStartup);
        }

        private void OnApplicationQuit()
        {
            Settings.Save();
            Speaker.Shutdown();
            Log.Close();
        }
    }
}
