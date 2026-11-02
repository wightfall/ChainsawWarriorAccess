using System;
using System.Collections.Generic;
using CWAccess.Audio;
using CWAccess.Speech;

namespace CWAccess.Nav
{
    /// <summary>F1: the keys of this screen (spoken and listed). Shift+F1: every key of the mod and every sound cue.</summary>
    internal static class KeyHelp
    {
        public static void ShowScreen(ScreenContext ctx)
        {
            if (ctx is InfoPanel) InfoPanel.Close();
            ScreenContext screen = Navigator.Context;
            string text = (screen != null ? screen.KeyHelp() : Loc.Get("help_menu")) + "\n" + Loc.Get("help_global");
            List<NavItem> items = Lines(text, "help");
            InfoPanel.Open(Loc.Get("panel_help"), items);
            // Say the whole help at once; Up and Down still go line by line.
            var all = new List<string>();
            for (int i = 1; i < items.Count; i++) all.Add(items[i].Speak());
            Speaker.Queue(TextUtil.Join(all));
        }

        public static void ShowAll(ScreenContext ctx)
        {
            if (ctx is InfoPanel) InfoPanel.Close();
            var items = new List<NavItem>();
            foreach (string section in new[] { "global", "menu", "board", "cards", "panel", "settings" })
            {
                items.Add(NavItem.Label("h_" + section, () => Loc.Get("keys_section_" + section)));
                items.AddRange(Lines(Loc.Get("help_" + section), section));
            }
            items.Add(NavItem.Label("h_cues", () => Loc.Get("keys_section_cues")));
            foreach (Cue c in Enum.GetValues(typeof(Cue)))
            {
                Cue cue = c;
                items.Add(new NavItem
                {
                    Key = "cue" + c,
                    Text = () => Loc.Get("cue_line", SettingsPanel.CueName(cue), Loc.TryGet("cuedesc_" + cue) ?? string.Empty),
                    Activate = () => CuePlayer.Preview(cue),
                });
            }
            InfoPanel.Open(Loc.Get("panel_all_keys"), items);
        }

        private static List<NavItem> Lines(string text, string prefix)
        {
            var items = new List<NavItem>();
            int i = 0;
            foreach (string line in text.Split('\n'))
            {
                string l = line.Trim();
                if (l.Length == 0) continue;
                items.Add(NavItem.Label(prefix + (i++), () => l));
            }
            return items;
        }
    }
}
