using System.Collections.Generic;
using AurochDigital;
using CWAccess.Game;
using CWAccess.Nav;
using CWAccess.Speech;
using UnityEngine;

namespace CWAccess.Screens
{
    /// <summary>Game over: the outcome and the score are read right away.</summary>
    internal sealed class EndGameScreen : GenericScreen
    {
        private readonly CEndScreen screen;

        public EndGameScreen(GameObject root) : base("EndGame", root)
        {
            screen = root.GetComponent<CEndScreen>();
            backButtons = new string[0];
        }

        public override string Intro()
        {
            if (screen == null) return null;
            return TextUtil.Join(G.LabelText(screen.mpGameOutcomeText), G.LabelText(screen.mpPlayerScore),
                screen.mpCompetition != null && screen.mpCompetition.activeInHierarchy
                    ? TextUtil.Join(G.LabelText(screen.mpCompetitionTitle), G.LabelText(screen.mpCompetitionDesc), G.LabelText(screen.mpCompetitionScore))
                    : null);
        }

        public override int InitialFocus(List<NavItem> items)
        {
            int i = items.FindIndex(x => x.Go != null && x.Go.name == "Button_MainMenu");
            return i >= 0 ? i : base.InitialFocus(items);
        }
    }

    /// <summary>Achievements with their locked / unlocked state (the game shows it only as a picture).</summary>
    internal sealed class AchievementScreen : GenericScreen
    {
        private readonly CAchievementScreen screen;

        /// <summary>The Steam achievements, read once each time the screen opens.</summary>
        private List<KeyValuePair<string, bool>> steam;

        public override void OnEnter()
        {
            steam = null;
            base.OnEnter();
        }

        public AchievementScreen(GameObject root) : base("Achievement", root)
        {
            screen = root.GetComponent<CAchievementScreen>();
            options.Skip = go => IsInAchievement(go);
        }

        private bool IsInAchievement(GameObject go)
        {
            if (screen == null || screen.mpAchievements == null) return false;
            for (Transform t = go.transform; t != null; t = t.parent)
            {
                if (screen.mpAchievements.Contains(t.gameObject)) return true;
            }
            return false;
        }

        public override List<NavItem> Build()
        {
            List<NavItem> items = base.Build();
            if (screen == null || screen.mpAchievements == null) return items;
            var list = new List<NavItem>();
            foreach (GameObject a in screen.mpAchievements)
            {
                if (a == null) continue;
                GameObject go = a;
                list.Add(new NavItem
                {
                    Key = "ach" + go.GetInstanceID(),
                    Go = go,
                    Position = UIScan.ScreenPos(go.transform),
                    Text = () => Describe(go),
                });
            }
            items.AddRange(list);
            UIScan.SortReadingOrder(items);
            // Page Up / Page Down: past the achievements to the buttons and back.
            foreach (NavItem item in items) item.Group = item.Key != null && item.Key.StartsWith("ach") ? "achievements" : "screen";
            // The Steam achievements, which the game shows only in Steam's overlay, before the Back button.
            if (steam == null) steam = Patches.Achievements.SteamList();
            if (steam.Count > 0)
            {
                var more = new List<NavItem>();
                int unlocked = steam.FindAll(x => x.Value).Count, total = steam.Count;
                more.Add(new NavItem { Key = "steamhead", Group = "steam", Text = () => Loc.Get("steam_achievements", unlocked, total) });
                for (int i = 0; i < steam.Count; i++)
                {
                    string text = steam[i].Key;
                    more.Add(new NavItem { Key = "steam" + i, Group = "steam", Text = () => text });
                }
                int back = items.FindIndex(x => x.Go != null && x.Go.name == "Button_Back");
                items.InsertRange(back >= 0 ? back : items.Count, more);
            }
            return items;
        }

        private static string Describe(GameObject go)
        {
            string title = Label(go, "Text_Achievement_Title");
            string desc = Label(go, "Text_Achievement_Description");
            string state = string.Empty;
            Transform img = go.transform.FindChild("Image_Achievement");
            UISprite sprite = img != null ? img.GetComponent<UISprite>() : null;
            if (sprite != null)
            {
                foreach (Achievement a in CStatsManager.Achievements)
                {
                    if (sprite.spriteName == a.ImageComplete)
                    {
                        state = Loc.Get("ach_unlocked");
                        break;
                    }
                    if (sprite.spriteName == a.Image) state = Loc.Get("ach_locked");
                }
            }
            return TextUtil.Join(title, state, desc);
        }

        private static string Label(GameObject go, string child)
        {
            Transform t = go.transform.FindChild(child);
            return t != null ? G.LabelText(t.GetComponent<UILabel>()) : string.Empty;
        }
    }

    /// <summary>
    /// The intro comic. Its pictures and speech bubbles are images, so the mod describes each scene (all captions and
    /// dialogue included) when it starts, lists the description as the first item, and says when a scene has finished.
    /// </summary>
    internal sealed class MotionComicScreen : GenericScreen
    {
        private bool nextWasShown;
        private float shownSince = -1f;
        private int scene = 1;
        private string playing;

        public MotionComicScreen(GameObject root) : base("MotionComic", root)
        {
        }

        private int CurrentScene
        {
            get
            {
                // "Scene 01" ... "Scene 05" in the game's comic screen.
                string s = R.Get<string>(root.GetComponent<CMotionGraphicScreen>(), "mpCurrentScene") ?? "Scene 01";
                int n;
                return int.TryParse(s.Substring(s.Length - 2), out n) ? n : 1;
            }
        }

        public override string Intro()
        {
            playing = "Scene 01";
            return Settings.DescribeComic.Value ? TextUtil.Join(Loc.Get("comic_intro"), Loc.Get("comic_scene_1")) : Loc.Get("comic_intro");
        }

        public override List<NavItem> Build()
        {
            List<NavItem> items = base.Build();
            items.Insert(0, NavItem.Label("description", () => Loc.Get("comic_scene_title", CurrentScene, Loc.Get("comic_scene_" + CurrentScene))));
            return items;
        }

        public override int InitialFocus(List<NavItem> items)
        {
            return items.Count > 1 ? 1 : 0;
        }

        public override void Tick()
        {
            // A new scene started (the player pressed Next): describe it.
            string now = R.Get<string>(root.GetComponent<CMotionGraphicScreen>(), "mpCurrentScene");
            if (now != null && playing != null && now != playing)
            {
                playing = now;
                if (Settings.DescribeComic.Value) Speaker.Queue(Loc.Get("comic_scene_" + CurrentScene));
            }
            GameObject next = Board.FindChildNamed(root.transform, "Button_NextScene");
            bool active = next != null && next.activeInHierarchy;
            // The button flickers on for a frame before a scene starts; only count it when it stays.
            if (!active) shownSince = -1f;
            else if (shownSince < 0f) shownSince = Time.realtimeSinceStartup;
            bool shown = active && Time.realtimeSinceStartup - shownSince > 0.6f;
            if (shown && !nextWasShown)
            {
                Navigator.Rebuild();
                Navigator.FocusKey("ctl:" + next.GetInstanceID(), speak: false);
                Speaker.Queue(Loc.Get("comic_scene_done", scene));
                scene++;
            }
            nextWasShown = shown;
        }
    }

    /// <summary>Load / save slots: the title says which of the two it is.</summary>
    internal sealed class LoadSaveScreen : GenericScreen
    {
        private readonly CLoadScreen screen;

        public LoadSaveScreen(GameObject root) : base("LoadScreen", root)
        {
            screen = root.GetComponent<CLoadScreen>();
            options.NoTexts = true;
        }

        public override string Title()
        {
            string t = screen != null ? G.LabelText(R.Get<UILabel>(screen, "mpTitle")) : string.Empty;
            return t.Length > 0 ? t : base.Title();
        }

        public override List<NavItem> Build()
        {
            List<NavItem> items = base.Build();
            foreach (NavItem item in items)
            {
                if (item.Go == null || !item.Go.name.StartsWith("Button_Save_0")) continue;
                string n = item.Go.name.Substring("Button_Save_0".Length);
                System.Func<string> inner = item.Text;
                item.Text = () => Loc.Get("slot", n, inner());
            }
            return items;
        }
    }
}
