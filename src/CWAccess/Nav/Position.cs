namespace CWAccess.Nav
{
    /// <summary>The place of an item in the list of the screen ("New Game, button, 1 of 10"), when the setting is on.</summary>
    internal static class Position
    {
        /// <summary>
        /// The text with ", 3 of 10" added, counted over the items of the screen. Unchanged when the setting is off, the
        /// item says its own position (the cards of a card screen: "2 of 7") or the place is unknown.
        /// </summary>
        public static string Add(string text, int index, int count, bool ownPosition, bool on)
        {
            if (!on || ownPosition || index < 0 || count <= 0 || index >= count || string.IsNullOrEmpty(text)) return text;
            return text + ", " + Loc.Get("position", index + 1, count);
        }
    }
}
