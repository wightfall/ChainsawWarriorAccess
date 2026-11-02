using System;
using UnityEngine;

namespace CWAccess.Nav
{
    /// <summary>One entry of a keyboard list: a button, a card, a setting or a piece of text.</summary>
    internal sealed class NavItem
    {
        /// <summary>Stable identity so the focus survives a refresh of the list.</summary>
        public string Key;

        /// <summary>What is spoken when the item gets focus.</summary>
        public Func<string> Text;

        /// <summary>Extra detail spoken with I (card description, item text...). Optional.</summary>
        public Func<string> Details;

        /// <summary>Enter / Space. Null for plain text.</summary>
        public Action Activate;

        /// <summary>Space, when it should differ from Enter (pick cards: Space only selects, Enter selects and confirms).</summary>
        public Action Toggle;

        /// <summary>Left / Right with -1 / +1. Null when the item cannot be adjusted.</summary>
        public Action<int> Adjust;

        /// <summary>A checkbox or radio button: Enter switches it (said by F1).</summary>
        public bool IsSwitch;

        /// <summary>The group the item belongs to, for Page Up / Page Down (null: none).</summary>
        public string Group;

        /// <summary>For sorting by screen position (screen pixels, y grows upwards).</summary>
        public Vector2 Position;

        /// <summary>The game object behind the item, if any.</summary>
        public GameObject Go;

        public bool IsText => Activate == null && Adjust == null;

        public string Speak()
        {
            try
            {
                return Text != null ? Text() : string.Empty;
            }
            catch (Exception e)
            {
                Log.Error("Item text failed: " + e.Message);
                return string.Empty;
            }
        }

        public static NavItem Label(string key, Func<string> text)
        {
            return new NavItem { Key = key, Text = text };
        }

        public static NavItem Button(string key, Func<string> text, Action activate)
        {
            return new NavItem { Key = key, Text = text, Activate = activate };
        }
    }
}
