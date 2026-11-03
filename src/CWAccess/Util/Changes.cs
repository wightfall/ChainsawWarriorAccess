using System.Collections.Generic;

namespace CWAccess
{
    /// <summary>Differences between two lists that may hold the same thing more than once.</summary>
    internal static class Changes
    {
        /// <summary>What "after" has more of than "before" (each extra copy once), in the order of "after".</summary>
        public static List<T> Added<T>(IEnumerable<T> before, IEnumerable<T> after)
        {
            var left = new Dictionary<T, int>();
            foreach (T x in before)
            {
                left.TryGetValue(x, out int n);
                left[x] = n + 1;
            }
            var added = new List<T>();
            foreach (T x in after)
            {
                if (left.TryGetValue(x, out int n) && n > 0) left[x] = n - 1;
                else added.Add(x);
            }
            return added;
        }
    }
}
