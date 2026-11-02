using System;
using System.Collections.Generic;
using CWAccess.Game;
using CWAccess.Speech;
using HarmonyLib;
using UnityEngine;

namespace CWAccess.Patches
{
    /// <summary>The game's message box: almost every event of a turn is written here.</summary>
    [HarmonyPatch(typeof(CMessageBoxOverlay), nameof(CMessageBoxOverlay.SetText))]
    internal static class MessageBoxPatch
    {
        public static string Last = string.Empty;
        public static float LastTime = -10f;

        private static void Postfix(string generalText, string modText, string roundText)
        {
            try
            {
                string text = TextUtil.Join(generalText, modText, roundText);
                if (text.Length == 0) return;
                Last = text;
                LastTime = UnityEngine.Time.realtimeSinceStartup;
                GameLog.Add(text);
                Speaker.Queue(text);
            }
            catch (Exception e)
            {
                Log.Error("MessageBoxPatch: " + e);
            }
        }
    }

    /// <summary>Remembers the dice of each visible roll so it can be said after the roll.</summary>
    internal static class Dice
    {
        public static readonly List<int> Pending = new List<int>();
        public static string LastRoll = string.Empty;
        public static int Depth;

        public static void Announce(string text)
        {
            LastRoll = text;
            GameLog.Add(text);
            if (Settings.AnnounceDice.Value) Speaker.Queue(text);
        }

        public static string JoinValues(List<int> values)
        {
            var s = new string[values.Count];
            for (int i = 0; i < values.Count; i++) s[i] = values[i].ToString();
            if (s.Length <= 1) return s.Length == 1 ? s[0] : string.Empty;
            return string.Join(", ", s, 0, s.Length - 1) + " " + Loc.Get("and") + " " + s[s.Length - 1];
        }
    }

    [HarmonyPatch(typeof(DiceRoller), nameof(DiceRoller.SetDice))]
    internal static class DiceRollerPatch
    {
        private static void Prefix(int diceRoll)
        {
            Dice.Pending.Add(diceRoll);
        }
    }

    [HarmonyPatch(typeof(CGameManager), nameof(CGameManager.DiceRoll))]
    internal static class PlayerDicePatch
    {
        private static void Prefix()
        {
            if (Dice.Depth++ == 0) Dice.Pending.Clear();
        }

        private static void Postfix(int numRolls, int numDice, int extra, bool rollDice, int __result)
        {
            if (--Dice.Depth > 0) return;
            if (!rollDice || Dice.Pending.Count == 0) return;
            try
            {
                var values = new List<int>(Dice.Pending);
                Dice.Pending.Clear();
                string text;
                if (numRolls > 1 && numDice == 1)
                {
                    // Character creation "best of" rolls: each die is one try.
                    text = Loc.Get("dice_best_of", Dice.JoinValues(values), __result - extra);
                }
                else if (values.Count == 1)
                {
                    text = Loc.Get("dice_one", values[0]);
                }
                else
                {
                    text = Loc.Get("dice_many", Dice.JoinValues(values), __result - extra);
                }
                if (extra != 0) text += " " + Loc.Get("dice_plus", extra, __result);
                Dice.Announce(text);
            }
            catch (Exception e)
            {
                Log.Error("PlayerDicePatch: " + e);
            }
        }
    }

    [HarmonyPatch(typeof(CGameManager), nameof(CGameManager.EnemyDiceRoll))]
    internal static class EnemyDicePatch
    {
        private static void Prefix()
        {
            if (Dice.Depth++ == 0) Dice.Pending.Clear();
        }

        private static void Postfix(int extra, int __result)
        {
            if (--Dice.Depth > 0) return;
            try
            {
                var values = new List<int>(Dice.Pending);
                Dice.Pending.Clear();
                if (values.Count == 0) return;
                Dice.Announce(Loc.Get("dice_enemy", Dice.JoinValues(values), __result - extra));
            }
            catch (Exception e)
            {
                Log.Error("EnemyDicePatch: " + e);
            }
        }
    }

    /// <summary>Card turned over: read its front.</summary>
    [HarmonyPatch(typeof(CGame), nameof(CGame.FlipCard))]
    internal static class FlipCardPatch
    {
        public static bool Flipped;

        private static void Postfix(CGame __instance)
        {
            Flipped = true;
            try
            {
                StatsCardBase card = __instance.mpHouseCard != null ? __instance.mpHouseCard.mpStatsCard : null;
                if (card == null) return;
                bool escapable = R.Get<bool>(__instance, "mbCardIsEscapeable");
                string text = CardText.Full(card, escapable);
                GameLog.Add(text);
                Speaker.Queue(text);
            }
            catch (Exception e)
            {
                Log.Error("FlipCardPatch: " + e);
            }
        }
    }

    /// <summary>New card at the top of the deck (face down).</summary>
    [HarmonyPatch(typeof(CGame), nameof(CGame.SetCard))]
    internal static class SetCardPatch
    {
        private static void Postfix(CGame __instance)
        {
            FlipCardPatch.Flipped = false;
            try
            {
                if (__instance.mpCurrentCard == null) return;
                string text = CardText.Room(__instance.mpCurrentCard);
                GameLog.Add(text);
                Speaker.Queue(text);
            }
            catch (Exception e)
            {
                Log.Error("SetCardPatch: " + e);
            }
        }
    }

    /// <summary>A different enemy became the current opponent (multiple enemies, wandering monsters).</summary>
    [HarmonyPatch(typeof(HouseCard), nameof(HouseCard.SetCardData))]
    internal static class HouseCardPatch
    {
        private static void Postfix(HouseCard __instance, StatsCardBase theCard, bool isEscapeable, bool bPrefabFlip)
        {
            try
            {
                // Only the board's card, and only when the game swaps in a new opponent face up.
                if (!G.InGame || __instance != G.Game.mpHouseCard || !bPrefabFlip || theCard == null) return;
                if (G.Game.mpCurrentCard == theCard) return;
                Speaker.Queue(CardText.Full(theCard, isEscapeable));
            }
            catch (Exception e)
            {
                Log.Error("HouseCardPatch: " + e);
            }
        }
    }

    /// <summary>Tutorial boxes that point at parts of the screen: read their texts too.</summary>
    [HarmonyPatch(typeof(CGameManager), nameof(CGameManager.ShowTutorialBox))]
    internal static class TutorialBoxPatch
    {
        private static void Postfix(string tutorialNo, bool bHide)
        {
            if (bHide || tutorialNo == null || !Settings.ReadTutorials.Value) return;
            try
            {
                Transform t = GameObject.Find("UI Root (2D)").transform.FindChild("2D Camera/TutorialOverlay/GUILayer/Anchor - Center/TutorialBoxOverlay0" + tutorialNo);
                if (t == null) return;
                var parts = new List<string>();
                foreach (UILabel l in t.GetComponentsInChildren<UILabel>())
                {
                    string s = TextUtil.Clean(l.text);
                    // Lone numbers belong to the pictures in the box (example stat values).
                    if (s.Length > 0 && !IsNumber(s) && !parts.Contains(s)) parts.Add(s);
                }
                string text = TextUtil.Join(parts);
                GameLog.Add(text);
                Speaker.Queue(text);
            }
            catch (Exception e)
            {
                Log.Error("TutorialBoxPatch: " + e);
            }
        }

        private static bool IsNumber(string s)
        {
            foreach (char c in s)
            {
                if (!char.IsDigit(c) && c != '+' && c != '-' && c != '/' && c != ' ') return false;
            }
            return true;
        }
    }

    /// <summary>NGUI's own keyboard handling (Enter clicks the last clicked button, WASD/arrows move sliders) would fight the mod.</summary>
    [HarmonyPatch(typeof(UICamera), nameof(UICamera.ProcessOthers))]
    internal static class NguiKeyboardPatch
    {
        private static bool Prefix()
        {
            return false;
        }
    }
}
