using System;
using System.Collections.Generic;
using CWAccess.Game;
using CWAccess.Nav;
using CWAccess.Patches;
using CWAccess.Speech;
using UnityEngine;

namespace CWAccess.Screens
{
    /// <summary>
    /// The game's options: two volume sliders and three on/off switches. Their captions are separate labels
    /// on screen, so the items are built from the screen's own fields instead of the generic scan.
    /// </summary>
    internal sealed class OptionsScreen : GenericScreen
    {
        private readonly COptionsScreen screen;

        public OptionsScreen(GameObject root) : base("Options", root)
        {
            screen = root.GetComponent<COptionsScreen>();
        }

        public override List<NavItem> Build()
        {
            if (screen == null) return base.Build();
            var items = new List<NavItem>
            {
                Slider("music", screen.mpMusicSlider, screen.mpMusicTitle, "opt_music"),
                Slider("sfx", screen.mpSFXSlider, screen.mpSFXTitle, "opt_sfx"),
                Switch("dice", screen.mpDiceEnabledButton, "Dice Animations", "opt_dice", () => CGameManager.mbDiceEnabled),
                Switch("tutorial", screen.mpTutorialReplayButton, "Replay Tutorial", "opt_tutorial", () => CGameManager.mbReplayTutorial),
                Switch("newequipment", screen.mpNewEquipmentButton, "New Equipment", "opt_new_equipment", () => CGameManager.mbNewEquipment),
            };
            if (screen.mpBack != null)
            {
                GameObject back = screen.mpBack.gameObject;
                items.Add(NavItem.Button("back", () => Loc.Get("role_button", UIScan.Fallback(LabelUnder(back), back)), () => ClickPatches.Click(back)));
            }
            items.RemoveAll(i => i == null);
            return items;
        }

        private static string LabelUnder(GameObject go)
        {
            UILabel l = go.GetComponentInChildren<UILabel>();
            return l != null ? TextUtil.Clean(l.text) : string.Empty;
        }

        private NavItem Slider(string key, UISlider slider, UILabel title, string fallback)
        {
            if (slider == null) return null;
            Func<string> name = () =>
            {
                string t = G.LabelText(title);
                return t.Length > 0 ? t : Loc.Get(fallback);
            };
            return new NavItem
            {
                Key = key,
                Go = slider.gameObject,
                Text = () => Loc.Get("role_slider", name(), Mathf.RoundToInt(slider.sliderValue * 100f)),
                Adjust = dir =>
                {
                    slider.sliderValue = Mathf.Clamp01(slider.sliderValue + dir * 0.05f);
                    Speaker.Say(Mathf.RoundToInt(slider.sliderValue * 100f) + "%");
                },
            };
        }

        /// <summary>A switch; its caption is the game's label with that text if found, else the mod's own name.</summary>
        private NavItem Switch(string key, UICheckbox box, string gameCaption, string fallback, Func<bool> state)
        {
            if (box == null) return null;
            string caption = FindLabel(gameCaption) ?? Loc.Get(fallback);
            GameObject go = box.gameObject;
            return new NavItem
            {
                Key = key,
                Go = go,
                Text = () => Loc.Get("role_checkbox", caption, Loc.Get(state() ? "state_on" : "state_off")),
                Activate = () => ClickPatches.Click(go),
            };
        }

        private string FindLabel(string text)
        {
            foreach (UILabel l in root.GetComponentsInChildren<UILabel>())
            {
                string t = TextUtil.Clean(l.text);
                if (string.Equals(t, text, StringComparison.OrdinalIgnoreCase)) return t;
            }
            return null;
        }

        public override int InitialFocus(List<NavItem> items)
        {
            return 0;
        }
    }
}
