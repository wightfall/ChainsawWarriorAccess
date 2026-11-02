using System.Collections.Generic;
using CWAccess.Game;
using CWAccess.Nav;
using CWAccess.Patches;
using UnityEngine;

namespace CWAccess.Screens
{
    /// <summary>
    /// Any menu page of the game, read straight from its NGUI objects: buttons, checkboxes, sliders and texts.
    /// </summary>
    internal class GenericScreen : ScreenContext
    {
        protected readonly string name;
        protected readonly GameObject root;
        protected readonly UIScan.Options options = new UIScan.Options();

        /// <summary>Buttons Escape presses, first found wins.</summary>
        protected string[] backButtons = { "Button_Back", "Button_Cancel", "Button_No" };

        /// <summary>What F1 says Escape does while one of the back buttons shows.</summary>
        protected string backHelp = "keys_back";

        /// <summary>Start on the first item (texts included) instead of the first button.</summary>
        protected bool focusFirstItem;

        public GenericScreen(string name, GameObject root)
        {
            this.name = name;
            this.root = root;
            switch (name)
            {
                case "Help":
                case "Credits":
                case "Achievement":
                case "EndGame":
                case "SteamError":
                    focusFirstItem = true;
                    break;
                case "MotionComic":
                    backButtons = new[] { "Button_SkipComic" };
                    backHelp = "keys_back_skip";
                    break;
                case "Difficulty":
                    options.Descriptions["Button_Easy"] = "Text_Easy";
                    options.Descriptions["Button_Medium"] = "Text_Medium";
                    options.Descriptions["Button_Hard"] = "Text_Hard";
                    break;
            }
        }

        public override string Id => "screen:" + name + ":" + (root != null ? root.GetInstanceID() : 0);

        public override string Title()
        {
            return Loc.TryGet("screen_" + name) ?? TextUtil.FromObjectName(name);
        }

        public override List<NavItem> Build()
        {
            return UIScan.Scan(root, options);
        }

        public override int InitialFocus(List<NavItem> items)
        {
            if (focusFirstItem) return 0;
            return base.InitialFocus(items);
        }

        /// <summary>The back button Escape presses now, or null.</summary>
        private GameObject BackButton()
        {
            foreach (string b in backButtons)
            {
                GameObject go = Board.FindChildNamed(root.transform, b);
                if (go != null && go.activeInHierarchy && UIScan.IsEnabled(go)) return go;
            }
            return null;
        }

        public override bool Back()
        {
            GameObject go = BackButton();
            if (go == null) return false;
            ClickPatches.Click(go);
            return true;
        }

        public override string BackHelp()
        {
            return BackButton() != null ? Loc.Get(backHelp) : null;
        }
    }

    /// <summary>The yes / no question box (quit, overwrite save, reveal cards).</summary>
    internal sealed class ConfirmDialog : GenericScreen
    {
        public ConfirmDialog(GameObject root) : base("Confirm", root)
        {
            backButtons = new[] { "Button_No" };
            backHelp = "keys_back_no";
            options.NoTexts = true;
        }

        public override string Title()
        {
            return Loc.Get("screen_Confirm");
        }

        public override string Intro()
        {
            CConfirmOverlay c = root.GetComponent<CConfirmOverlay>();
            return c != null ? G.LabelText(R.Get<UILabel>(c, "mpText")) : null;
        }

        public override List<NavItem> Build()
        {
            List<NavItem> items = base.Build();
            string question = Intro();
            items.Insert(0, NavItem.Label("question", () => Intro()));
            return items;
        }

        public override int InitialFocus(List<NavItem> items)
        {
            return items.Count > 1 ? 1 : 0;
        }
    }
}
