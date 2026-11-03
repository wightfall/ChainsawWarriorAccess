using System.Collections.Generic;
using CWAccess.Speech;
using UnityEngine;

namespace CWAccess.Nav
{
    /// <summary>Moves a virtual focus through the items of the active screen and speaks them.</summary>
    internal static class Navigator
    {
        private static ScreenContext context;
        private static List<NavItem> items = new List<NavItem>();
        private static int index;
        private static float nextRefresh;
        private static string watchKey;
        private static string watchText;
        private static float watchUntil;

        public static ScreenContext Context => context;

        public static NavItem Focused => index >= 0 && index < items.Count ? items[index] : null;

        public static IList<NavItem> Items => items;

        /// <summary>Switches to a screen. Announces it unless quiet.</summary>
        public static void SetContext(ScreenContext ctx, bool announce = true)
        {
            if (announce && ctx != null && !(ctx is InfoPanel)) Audio.CuePlayer.Play(Audio.Cue.Screen);
            if (context != null) context.OnLeave();
            context = ctx;
            items = new List<NavItem>();
            index = 0;
            if (context == null) return;
            context.OnEnter();
            Rebuild(keepFocus: false);
            index = Mathf.Clamp(context.InitialFocus(items), 0, Mathf.Max(0, items.Count - 1));
            nextRefresh = Time.realtimeSinceStartup + context.RefreshInterval;
            if (announce) AnnounceScreen(interrupt: false);
        }

        public static void AnnounceScreen(bool interrupt)
        {
            if (context == null) return;
            var parts = new List<string> { context.Title(), context.Intro() };
            NavItem f = Focused;
            if (f != null && context.AnnounceFocusOnEnter) parts.Add(f.Speak());
            Speaker.Output(TextUtil.Join(parts), interrupt);
        }

        /// <summary>Rebuild the item list now (after something changed on screen).</summary>
        public static void Rebuild(bool keepFocus = true)
        {
            if (context == null) return;
            string key = keepFocus && Focused != null ? Focused.Key : null;
            int oldIndex = index;
            List<NavItem> fresh;
            try
            {
                fresh = context.Build() ?? new List<NavItem>();
            }
            catch (System.Exception e)
            {
                Log.Error("Building items for " + context.Id + " failed: " + e);
                fresh = new List<NavItem>();
            }
            items = fresh;
            if (key != null)
            {
                int found = items.FindIndex(i => i.Key == key);
                index = found >= 0 ? found : Mathf.Clamp(oldIndex, 0, Mathf.Max(0, items.Count - 1));
            }
            else
            {
                index = Mathf.Clamp(index, 0, Mathf.Max(0, items.Count - 1));
            }
        }

        public static void Tick()
        {
            if (context == null) return;
            context.Tick();
            if (Time.realtimeSinceStartup >= nextRefresh)
            {
                nextRefresh = Time.realtimeSinceStartup + context.RefreshInterval;
                Rebuild();
                CheckWatched();
            }
        }

        /// <summary>Says the pressed item again if its text changed because of the press.</summary>
        private static void CheckWatched()
        {
            if (watchUntil <= 0f) return;
            if (Time.realtimeSinceStartup > watchUntil)
            {
                watchUntil = 0f;
                return;
            }
            NavItem f = Focused;
            if (f != null && f.Key == watchKey)
            {
                string now = f.Speak();
                if (now != watchText)
                {
                    watchUntil = 0f;
                    Speaker.Queue(now);
                    return;
                }
            }
            nextRefresh = Time.realtimeSinceStartup + 0.1f;
        }

        /// <summary>Focus an item by key and say it.</summary>
        public static bool FocusKey(string key, bool speak = true)
        {
            int found = items.FindIndex(i => i.Key == key);
            if (found < 0) return false;
            index = found;
            if (speak) SayFocused();
            return true;
        }

        public static void FocusIndex(int i, bool speak = true)
        {
            if (items.Count == 0) return;
            index = Mathf.Clamp(i, 0, items.Count - 1);
            if (speak) SayFocused();
        }

        public static void SayFocused()
        {
            NavItem f = Focused;
            if (f == null)
            {
                Speaker.Say(Loc.Get("nothing_here"));
                return;
            }
            Speaker.Say(f.Speak());
        }

        public static void Move(int delta)
        {
            if (items.Count == 0)
            {
                Speaker.Say(Loc.Get("nothing_here"));
                return;
            }
            int next = index + delta;
            if (next < 0 || next >= items.Count)
            {
                // Stay at the edge and say the item again so the user knows where they are.
                Audio.CuePlayer.Play(Audio.Cue.Edge);
                Speaker.Say(Loc.Get(next < 0 ? "edge_top" : "edge_bottom") + " " + Focused.Speak());
                return;
            }
            index = next;
            Audio.CuePlayer.Play(Audio.Cue.Move);
            SayFocused();
        }

        public static void Home()
        {
            FocusIndex(0);
        }

        public static void End()
        {
            FocusIndex(items.Count - 1);
        }

        /// <summary>Page Up / Page Down: the previous or next group of items; ten items in a long list without groups.</summary>
        public static void Page(int dir)
        {
            if (items.Count == 0)
            {
                Speaker.Say(Loc.Get("nothing_here"));
                return;
            }
            int next = Paging.Target(items, index, dir);
            if (next < 0)
            {
                Audio.CuePlayer.Play(Audio.Cue.Edge);
                Speaker.Say(Loc.Get(dir < 0 ? "edge_top" : "edge_bottom") + " " + Focused.Speak());
                return;
            }
            index = next;
            Audio.CuePlayer.Play(Audio.Cue.Move);
            SayFocused();
        }

        /// <summary>Jump to the next item whose text starts with the letter.</summary>
        public static bool TypeAhead(char c)
        {
            if (items.Count == 0) return false;
            c = char.ToLowerInvariant(c);
            for (int step = 1; step <= items.Count; step++)
            {
                int i = (index + step) % items.Count;
                if (Letters.Matches(items[i], c))
                {
                    index = i;
                    SayFocused();
                    return true;
                }
            }
            return false;
        }

        public static void Activate()
        {
            NavItem f = Focused;
            if (f == null) return;
            if (f.Activate == null)
            {
                Speaker.Say(f.Speak());
                return;
            }
            if (f.Go != null && !UIScan.IsEnabled(f.Go))
            {
                Audio.CuePlayer.Play(Audio.Cue.Unavailable);
                Speaker.Say(Loc.Get("state_unavailable"));
                return;
            }
            Speaker.Stop();
            ScreenContext before = context;
            watchKey = f.Key;
            watchText = f.Speak();
            UICheckbox box = f.Go != null ? f.Go.GetComponent<UICheckbox>() : null;
            if (box == null) Audio.CuePlayer.Play(Audio.Cue.Activate);
            f.Activate();
            if (box != null) Audio.CuePlayer.Play(box.isChecked ? Audio.Cue.ToggleOn : Audio.Cue.ToggleOff);
            // If the item still exists and reads differently right after the press (checkbox state, a save
            // slot's name...), say it again so the result is heard.
            nextRefresh = Time.realtimeSinceStartup + 0.1f;
            // Only game controls: the mod's own items say their result themselves.
            watchUntil = context == before && f.Go != null ? Time.realtimeSinceStartup + 0.6f : 0f;
        }

        /// <summary>Space: the item's toggle if it has one, else the same as Enter.</summary>
        public static void ToggleFocused()
        {
            NavItem f = Focused;
            if (f != null && f.Toggle != null)
            {
                Speaker.Stop();
                f.Toggle();
                return;
            }
            Activate();
        }

        public static void Adjust(int dir)
        {
            NavItem f = Focused;
            if (f == null || f.Adjust == null) return;
            f.Adjust(dir);
        }

        public static void Details()
        {
            NavItem f = Focused;
            if (f == null) return;
            string d = f.Details != null ? f.Details() : null;
            Speaker.Say(string.IsNullOrEmpty(d) ? f.Speak() : d);
        }
    }
}
