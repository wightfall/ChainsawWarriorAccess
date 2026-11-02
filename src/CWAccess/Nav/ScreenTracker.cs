using System.Collections.Generic;
using CWAccess.Game;
using CWAccess.Screens;
using UnityEngine;

namespace CWAccess.Nav
{
    /// <summary>Finds out every frame which screen or dialog is on top and gives it the keyboard.</summary>
    internal static class ScreenTracker
    {
        private static readonly Dictionary<string, ScreenContext> cache = new Dictionary<string, ScreenContext>();

        // The screen of the last frame and its context: most frames the screen has not changed.
        private static CGameScreen lastScreen;
        private static ScreenContext lastContext;

        public static void Tick()
        {
            if (InfoPanel.Current != null)
            {
                if (Navigator.Context != InfoPanel.Current) Navigator.SetContext(InfoPanel.Current, announce: false);
                return;
            }
            ScreenContext want = Detect();
            if (want == null) return;
            if (Navigator.Context == null || Navigator.Context.Id != want.Id) Navigator.SetContext(want, announce: true);
        }

        /// <summary>Re-evaluate now (after a panel closed).</summary>
        public static void ForceRefresh(bool announce)
        {
            ScreenContext want = Detect();
            if (want != null) Navigator.SetContext(want, announce);
        }

        private static ScreenContext Detect()
        {
            if (G.IsOverlayActive("Confirm"))
            {
                GameObject c = CUIManager.GetOverlay("Confirm");
                return Get("confirm" + c.GetInstanceID(), () => new ConfirmDialog(c));
            }
            CGameScreen cur = G.CurrentScreen;
            if (cur == null || cur.gameObject == null) return null;
            if (ReferenceEquals(cur, lastScreen) && lastContext != null) return lastContext;
            lastContext = ForScreen(cur);
            lastScreen = cur;
            return lastContext;
        }

        private static ScreenContext ForScreen(CGameScreen cur)
        {
            string name = G.CurrentScreenName ?? cur.gameObject.name;
            GameObject root = cur.gameObject;
            string key = name + root.GetInstanceID();
            switch (name)
            {
                case "Game":
                    return Get(key, () => new Board(root));
                case "Equipment":
                    return Get(key, () => new EquipmentBuyScreen(root));
                case "EquipmentSelection":
                    return Get(key, () => new EquipmentPickScreen(root));
                case "CardSelect":
                    return Get(key, () => new CardPickScreen(root));
                case "EndGame":
                    return Get(key, () => new EndGameScreen(root));
                case "Achievement":
                    return Get(key, () => new AchievementScreen(root));
                case "MotionComic":
                    return Get(key, () => new MotionComicScreen(root));
                case "LoadScreen":
                    return Get(key, () => new LoadSaveScreen(root));
                case "Options":
                    return Get(key, () => new OptionsScreen(root));
                default:
                    return Get(key, () => new GenericScreen(name, root));
            }
        }

        private static ScreenContext Get(string key, System.Func<ScreenContext> make)
        {
            if (!cache.TryGetValue(key, out ScreenContext ctx))
            {
                if (cache.Count > 64)
                {
                    cache.Clear();
                    lastScreen = null;
                    lastContext = null;
                }
                ctx = make();
                cache[key] = ctx;
            }
            return ctx;
        }
    }
}
