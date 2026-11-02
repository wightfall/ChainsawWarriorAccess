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

    /// <summary>The intro comic: says when a scene has finished and the Next button is there.</summary>
    internal sealed class MotionComicScreen : GenericScreen
    {
        private bool nextWasShown;
        private float shownSince = -1f;
        private int scene = 1;

        public MotionComicScreen(GameObject root) : base("MotionComic", root)
        {
        }

        public override string Intro()
        {
            return Loc.Get("comic_intro");
        }

        public override void Tick()
        {
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
