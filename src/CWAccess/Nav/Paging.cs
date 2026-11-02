using System;
using System.Collections.Generic;

namespace CWAccess.Nav
{
    /// <summary>
    /// Page Up / Page Down: to the previous or next group of items (a section of a list, a part of the board, the
    /// cards of a card screen...). In a long list without groups they move ten items.
    /// </summary>
    internal static class Paging
    {
        public const int PageSize = 10;

        private static string GroupOf(IList<NavItem> items, int i)
        {
            return items[i] != null ? items[i].Group : null;
        }

        /// <summary>True when the items form at least two groups.</summary>
        public static bool HasGroups(IList<NavItem> items)
        {
            if (items == null || items.Count < 2) return false;
            bool any = false;
            foreach (NavItem item in items)
            {
                if (item != null && item.Group != null)
                {
                    any = true;
                    break;
                }
            }
            if (!any) return false;
            for (int i = 1; i < items.Count; i++)
            {
                if (GroupOf(items, i) != GroupOf(items, i - 1)) return true;
            }
            return false;
        }

        /// <summary>
        /// Where Page Up (dir -1) or Page Down (+1) goes from the item at index; -1 when it cannot move.
        /// Page Up first goes to the start of the current group, like going to the previous heading.
        /// </summary>
        public static int Target(IList<NavItem> items, int index, int dir)
        {
            int n = items != null ? items.Count : 0;
            if (n == 0) return -1;
            if (index < 0 || index >= n) index = 0;
            if (HasGroups(items))
            {
                string g = GroupOf(items, index);
                if (dir > 0)
                {
                    for (int i = index + 1; i < n; i++)
                    {
                        if (GroupOf(items, i) != g) return i;
                    }
                    return -1;
                }
                int start = index;
                while (start > 0 && GroupOf(items, start - 1) == g) start--;
                if (start < index) return start;
                if (start == 0) return -1;
                int prev = start - 1;
                string pg = GroupOf(items, prev);
                while (prev > 0 && GroupOf(items, prev - 1) == pg) prev--;
                return prev;
            }
            int t = Math.Max(0, Math.Min(n - 1, index + dir * PageSize));
            return t == index ? -1 : t;
        }

        /// <summary>Puts every item in the group of the heading before it.</summary>
        public static void GroupByHeadings(IList<NavItem> items, Func<NavItem, bool> isHeading)
        {
            string current = null;
            foreach (NavItem item in items)
            {
                if (item == null) continue;
                if (isHeading(item)) current = item.Key;
                if (current != null) item.Group = current;
            }
        }
    }
}
