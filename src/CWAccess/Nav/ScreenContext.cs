using System.Collections.Generic;
using UnityEngine;

namespace CWAccess.Nav
{
    /// <summary>
    /// What the keyboard works on right now: a menu screen, the game board, a card picker, a dialog...
    /// The navigator asks it for its items, and gives it the first chance at every key.
    /// </summary>
    internal abstract class ScreenContext
    {
        /// <summary>Identity: when it changes the navigator announces the new screen.</summary>
        public abstract string Id { get; }

        /// <summary>Spoken when the screen opens.</summary>
        public abstract string Title();

        /// <summary>The items in reading order. Called on entry and every refresh.</summary>
        public abstract List<NavItem> Build();

        /// <summary>Spoken after the title when the screen opens (e.g. a dialog's question). Optional.</summary>
        public virtual string Intro()
        {
            return null;
        }

        /// <summary>Index of the item to focus first.</summary>
        public virtual int InitialFocus(List<NavItem> items)
        {
            for (int i = 0; i < items.Count; i++)
            {
                if (items[i].Activate != null && (items[i].Go == null || UIScan.IsEnabled(items[i].Go))) return i;
            }
            return 0;
        }

        /// <summary>Screen-specific keys. Return true when the key was used.</summary>
        public virtual bool HandleKey(KeyCode key, bool shift, bool ctrl)
        {
            return false;
        }

        /// <summary>Escape. Return true when handled.</summary>
        public virtual bool Back()
        {
            return false;
        }

        /// <summary>Called every frame while active.</summary>
        public virtual void Tick()
        {
        }

        /// <summary>Called when the screen becomes active / stops being active.</summary>
        public virtual void OnEnter()
        {
        }

        public virtual void OnLeave()
        {
        }

        /// <summary>Whether the focused item is read after the title when the screen opens.</summary>
        public virtual bool AnnounceFocusOnEnter => true;

        /// <summary>How often (seconds) the item list is rebuilt to follow changes on screen.</summary>
        public virtual float RefreshInterval => 0.4f;

        /// <summary>F1: the keys that work on this screen now, one sentence each (by default from its items).</summary>
        public virtual List<string> HelpLines(IList<NavItem> items)
        {
            List<string> lines = KeyHelp.ForItems(items, letters: true);
            string back = BackHelp();
            if (back != null) lines.Add(back);
            return lines;
        }

        /// <summary>True for the list of every key (Shift+F1).</summary>
        public virtual bool IsKeyList => false;

        /// <summary>What Escape does on this screen, for F1; null when it does nothing.</summary>
        public virtual string BackHelp()
        {
            return null;
        }
    }
}
