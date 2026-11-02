using System.Collections.Generic;
using CWAccess.Speech;
using UnityEngine;

namespace CWAccess.Nav
{
    /// <summary>
    /// A list that exists only in speech (equipment, event log, key help). It sits on top of the
    /// current screen until Escape closes it; the game keeps running underneath.
    /// </summary>
    internal sealed class InfoPanel : ScreenContext
    {
        private static InfoPanel open;
        private static int opened;
        private readonly System.Func<string> title;
        private readonly List<NavItem> items;
        private readonly bool startAtEnd;
        private readonly int number;
        private readonly string help;

        private InfoPanel(System.Func<string> title, List<NavItem> items, bool startAtEnd, string help)
        {
            this.title = title;
            this.items = items;
            this.startAtEnd = startAtEnd;
            this.help = help;
            number = ++opened;
        }

        public static InfoPanel Current => open;

        public static void Open(string title, List<NavItem> items, bool startAtEnd = false, string help = null)
        {
            Open(() => title, items, startAtEnd, help);
        }

        /// <summary>
        /// A panel whose title is looked up each time it is spoken (it follows a language change).
        /// help: the text key of its keys for F1 (null: worked out from its items).
        /// </summary>
        public static void Open(System.Func<string> title, List<NavItem> items, bool startAtEnd = false, string help = null)
        {
            open = new InfoPanel(title, items, startAtEnd, help);
            Speaker.Stop();
            Audio.CuePlayer.Play(Audio.Cue.PanelOpen);
            Navigator.SetContext(open, announce: false);
            Navigator.AnnounceScreen(interrupt: true);
        }

        public static void Close()
        {
            if (open == null) return;
            open = null;
            Audio.CuePlayer.Play(Audio.Cue.PanelClose);
            Speaker.Say(Loc.Get("panel_closed"));
            ScreenTracker.ForceRefresh(announce: false);
            Navigator.SayFocused();
        }

        public override string Id => "panel:" + number;

        public override string Title()
        {
            return items.Count == 1 ? Loc.Get("panel_title_one", title()) : Loc.Get("panel_title", title(), items.Count);
        }

        public override List<NavItem> Build()
        {
            return items;
        }

        public override int InitialFocus(List<NavItem> list)
        {
            return startAtEnd ? list.Count - 1 : 0;
        }

        public override float RefreshInterval => 3600f;

        public override bool HandleKey(KeyCode key, bool shift, bool ctrl)
        {
            // Pressing the key that opened the panel again closes it.
            return false;
        }

        public override bool Back()
        {
            Close();
            return true;
        }

        /// <summary>The Shift+F1 list itself: F1 there does not point to Shift+F1.</summary>
        public override bool IsKeyList => help == "help_panel_keys";

        public override List<string> HelpLines(IList<NavItem> list)
        {
            return KeyHelp.ForPanel(help, list);
        }
    }
}
