using System.Collections.Generic;

namespace CWAccess.Game
{
    /// <summary>Everything that happened in the game (messages, cards, dice, stat changes), for review with L.</summary>
    internal static class GameLog
    {
        public const int Max = 300;
        public static readonly List<string> Entries = new List<string>();

        public static void Add(string text)
        {
            text = TextUtil.Clean(text);
            if (text.Length == 0) return;
            if (Entries.Count > 0 && Entries[Entries.Count - 1] == text) return;
            Entries.Add(text);
            if (Entries.Count > Max) Entries.RemoveAt(0);
        }

        public static void Clear()
        {
            Entries.Clear();
        }
    }
}
