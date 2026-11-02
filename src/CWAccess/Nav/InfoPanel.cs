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
        private readonly string title;
        private readonly List<NavItem> items;
        private readonly bool startAtEnd;

        private InfoPanel(string title, List<NavItem> items, bool startAtEnd)
        {
            this.title = title;
            this.items = items;
            this.startAtEnd = startAtEnd;
        }

        public static InfoPanel Current => open;

        public static void Open(string title, List<NavItem> items, bool startAtEnd = false)
        {
            open = new InfoPanel(title, items, startAtEnd);
            Speaker.Stop();
            Navigator.SetContext(open, announce: false);
            Navigator.AnnounceScreen(interrupt: true);
        }

        public static void Close()
        {
            if (open == null) return;
            open = null;
            Speaker.Say(Loc.Get("panel_closed"));
            ScreenTracker.ForceRefresh(announce: false);
            Navigator.SayFocused();
        }

        public override string Id => "panel:" + title;

        public override string Title()
        {
            return Loc.Get("panel_title", title, items.Count);
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

        public override string KeyHelp()
        {
            return Loc.Get("help_panel");
        }
    }
}
