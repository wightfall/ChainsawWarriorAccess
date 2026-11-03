namespace CWAccess.Nav
{
    /// <summary>
    /// Jumping to an item by letter. The letter is compared with what is said and with the item's text on screen, so
    /// when the mod speaks another language (Thai) the letters still find the game's English names that are on screen.
    /// </summary>
    internal static class Letters
    {
        public static bool Matches(NavItem item, char letter)
        {
            if (item == null) return false;
            char c = char.ToLowerInvariant(letter);
            return StartsWith(item.Speak(), c) || (item.ScreenText != null && StartsWith(Safe(item), c));
        }

        private static bool StartsWith(string text, char c)
        {
            return !string.IsNullOrEmpty(text) && char.ToLowerInvariant(text[0]) == c;
        }

        private static string Safe(NavItem item)
        {
            try
            {
                return item.ScreenText();
            }
            catch (System.Exception)
            {
                return string.Empty;
            }
        }
    }
}
