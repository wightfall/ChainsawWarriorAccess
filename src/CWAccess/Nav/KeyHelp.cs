using System;
using System.Collections.Generic;
using CWAccess.Audio;
using CWAccess.Speech;

namespace CWAccess.Nav
{
    /// <summary>
    /// F1 says the keys that work on the current screen, worked out from what is on it; nothing opens.
    /// Shift+F1 lists every key of the mod and every sound cue.
    /// </summary>
    internal static class KeyHelp
    {
        public static void SayScreen(ScreenContext ctx)
        {
            Speaker.Say(ScreenText(ctx, Navigator.Items));
        }

        /// <summary>The F1 text: the keys of the screen, then where to find all of them.</summary>
        internal static string ScreenText(ScreenContext ctx, IList<NavItem> items)
        {
            var lines = new List<string>();
            if (ctx != null) lines.AddRange(ctx.HelpLines(items ?? new List<NavItem>()));
            // Page Up / Page Down where the list has groups or is long, after the line about moving.
            string page = PageLine(items);
            if (page != null && lines.Count > 0 && !lines.Contains(page)) lines.Insert(1, page);
            if (lines.Count == 0) lines.Add(Loc.Get("keys_nothing"));
            if (ctx == null || !ctx.IsKeyList) lines.Add(Loc.Get("keys_more"));
            return TextUtil.Join(lines);
        }

        /// <summary>What Page Up / Page Down do in these items, or null when they do nothing useful.</summary>
        internal static string PageLine(IList<NavItem> items)
        {
            if (Paging.HasGroups(items)) return Loc.Get("keys_page_groups");
            if (items != null && items.Count > Paging.PageSize) return Loc.Get("keys_page_items", Paging.PageSize);
            return null;
        }

        /// <summary>The keys of a list of items: moving, pressing, switching, changing, details and letters.</summary>
        internal static List<string> ForItems(IList<NavItem> items, bool letters)
        {
            var lines = new List<string>();
            if (items == null) return lines;
            bool press = false, switches = false, adjust = false, details = false;
            foreach (NavItem i in items)
            {
                if (i == null) continue;
                if (i.IsSwitch) switches = true;
                else if (i.Activate != null) press = true;
                if (i.Adjust != null) adjust = true;
                if (i.Details != null) details = true;
            }
            if (items.Count > 1) lines.Add(Loc.Get("keys_move"));
            if (press) lines.Add(Loc.Get("keys_press"));
            if (switches) lines.Add(Loc.Get("keys_switch"));
            if (adjust) lines.Add(Loc.Get("keys_adjust"));
            if (details) lines.Add(Loc.Get("keys_details"));
            if (letters && items.Count > 3) lines.Add(Loc.Get("keys_letters"));
            return lines;
        }

        /// <summary>The keys of a list panel: its own help text, or the keys of its items and Escape to close.</summary>
        internal static List<string> ForPanel(string help, IList<NavItem> items)
        {
            if (help != null) return Lines(help);
            List<string> lines = ForItems(items, letters: false);
            lines.Add(Loc.Get("keys_back_close"));
            return lines;
        }

        /// <summary>The lines of a help text with "\n" between them.</summary>
        internal static List<string> Lines(string key)
        {
            var lines = new List<string>();
            foreach (string line in Loc.Get(key).Split('\n'))
            {
                string l = line.Trim();
                if (l.Length > 0) lines.Add(l);
            }
            return lines;
        }

        public static void ShowAll(ScreenContext ctx)
        {
            if (ctx is InfoPanel) InfoPanel.Close();
            InfoPanel.Open(() => Loc.Get("panel_all_keys"), AllKeysItems(), help: "help_panel_keys");
        }

        /// <summary>The Shift+F1 list: the keys by section, then the sound cues. Each section is a Page Up / Page Down group.</summary>
        internal static List<NavItem> AllKeysItems()
        {
            var items = new List<NavItem>();
            foreach (string section in new[] { "global", "menu", "board", "cards", "panel", "settings" })
            {
                string s = section;
                items.Add(new NavItem { Key = "h_" + s, Text = () => Loc.Get("keys_section_" + s), Group = s });
                foreach (NavItem line in LineItems(Loc.Get("help_" + s), s))
                {
                    line.Group = s;
                    items.Add(line);
                }
            }
            items.Add(new NavItem { Key = "h_cues", Text = () => Loc.Get("keys_section_cues"), Group = "cues" });
            foreach (Cue c in Enum.GetValues(typeof(Cue)))
            {
                Cue cue = c;
                items.Add(new NavItem
                {
                    Key = "cue" + c,
                    Group = "cues",
                    Text = () => Loc.Get("cue_line", SettingsPanel.CueName(cue), Loc.TryGet("cuedesc_" + cue) ?? string.Empty),
                    Activate = () => CuePlayer.Preview(cue),
                });
            }
            return items;
        }

        private static List<NavItem> LineItems(string text, string prefix)
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
