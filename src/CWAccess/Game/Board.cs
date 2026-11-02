using System.Collections.Generic;
using CWAccess.Nav;
using CWAccess.Patches;
using CWAccess.Speech;
using UnityEngine;

namespace CWAccess.Game
{
    /// <summary>
    /// The game board: the house card, the message box, the four action buttons and the stats around them.
    /// Up/Down walk through the choices; number keys pick an action directly; letters read information.
    /// </summary>
    internal sealed class Board : ScreenContext
    {
        private EGameState lastState = EGameState.GS_None;
        private float enteredAt;
        private readonly int rootId;

        public Board(GameObject root)
        {
            rootId = root != null ? root.GetInstanceID() : 0;
        }

        public override string Id => "board:" + rootId;

        public override string Title()
        {
            return Loc.Get("screen_board");
        }

        public override string Intro()
        {
            // The message box and choices are announced as they appear; on return from a menu repeat them.
            if (!G.InGame) return null;
            GameWatcher.MarkActionsAnnounced();
            // A message spoken a moment ago (the game just started or a dialog closed) is not repeated.
            bool fresh = Time.realtimeSinceStartup - MessageBoxPatch.LastTime < 2f;
            return TextUtil.Join(fresh || !Settings.RepeatMessageOnReturn.Value ? null : G.MessageText, ActionsSummary());
        }

        public override float RefreshInterval => 0.25f;

        public override bool AnnounceFocusOnEnter => false;

        public static bool CanTurnCard => G.InGame && G.Game.meGameState == EGameState.GS_PreAction && G.Game.mpHouseCard != null
                                          && G.Game.mpHouseCard.gameObject.activeInHierarchy;

        public override List<NavItem> Build()
        {
            var items = new List<NavItem>();
            if (!G.InGame) return items;
            items.Add(NavItem.Label("message", () => Loc.Get("item_message", G.MessageText)));
            if (CanTurnCard)
            {
                items.Add(NavItem.Button("turn", () => Loc.Get("item_turn_card", CardText.Room(G.Game.mpCurrentCard)), TurnCard));
            }
            EActionType[] actions = G.Actions;
            GameObject[] buttons = G.ActionButtons;
            for (int i = 0; i < actions.Length && i < buttons.Length; i++)
            {
                if (actions[i] == EActionType.AT_None) continue;
                int n = i;
                EActionType a = actions[i];
                items.Add(new NavItem
                {
                    Key = "action" + n + a,
                    Go = buttons[n],
                    Text = () => Loc.Get("item_action", n + 1, G.ActionName(a)),
                    Details = () => ActionHelp(a),
                    Activate = () => DoAction(n),
                });
            }
            items.Add(new NavItem { Key = "card", Text = CardLine, Details = CardDetails });
            items.Add(NavItem.Label("status", Status.Summary));
            CUIScreen hud = G.Hud;
            if (hud != null && hud.mpEquipmentScreenButton != null)
            {
                GameObject eq = hud.mpEquipmentScreenButton;
                items.Add(new NavItem { Key = "equipment", Go = eq, Text = () => Loc.Get("item_equipment_screen") + UIScan.Disabled(eq), Activate = () => ClickPatches.Click(eq) });
            }
            items.Add(NavItem.Button("pause", () => Loc.Get("item_pause"), OpenPause));
            return items;
        }

        public override void OnEnter()
        {
            enteredAt = Time.realtimeSinceStartup;
        }

        public override int InitialFocus(List<NavItem> items)
        {
            int i = items.FindIndex(x => x.Key == "turn" || x.Key.StartsWith("action"));
            return i >= 0 ? i : 0;
        }

        /// <summary>New choices: put the focus on the first of them (quietly; the choices are announced).</summary>
        public static void OnActionsChanged()
        {
            if (!(Navigator.Context is Board)) return;
            Navigator.Rebuild();
            IList<NavItem> items = Navigator.Items;
            for (int i = 0; i < items.Count; i++)
            {
                if (items[i].Key == "turn" || items[i].Key.StartsWith("action"))
                {
                    Navigator.FocusIndex(i, speak: false);
                    return;
                }
            }
        }

        public override void Tick()
        {
            if (!G.InGame) return;
            EGameState s = G.Game.meGameState;
            if (s != lastState)
            {
                lastState = s;
                if (s == EGameState.GS_PreAction && CanTurnCard)
                {
                    Navigator.Rebuild();
                    Navigator.FocusKey("turn", speak: false);
                    // Right after the board opens its title already listed "F turn over the card".
                    if (Time.realtimeSinceStartup - enteredAt > 1f) Speaker.Queue(Loc.Get("hint_turn_card"));
                }
            }
        }

        public static string ActionsSummary()
        {
            var names = new List<string>();
            EActionType[] actions = G.Actions;
            for (int i = 0; i < actions.Length; i++)
            {
                if (actions[i] != EActionType.AT_None) names.Add((i + 1) + " " + G.ActionName(actions[i]));
            }
            if (CanTurnCard) names.Insert(0, Loc.Get("choice_turn_card"));
            if (names.Count == 0) return string.Empty;
            return Loc.Get("choices", string.Join(", ", names.ToArray()));
        }

        private static string ActionHelp(EActionType a)
        {
            return Loc.TryGet("actionhelp_" + a) ?? G.ActionName(a);
        }

        public static void TurnCard()
        {
            if (!CanTurnCard)
            {
                Speaker.Say(Loc.Get("cannot_turn"));
                return;
            }
            // Same as clicking the card on the board.
            G.Game.GameState = EGameState.GS_TurnCard;
        }

        public static void DoAction(int n)
        {
            EActionType[] actions = G.Actions;
            GameObject[] buttons = G.ActionButtons;
            if (n < 0 || n >= actions.Length || actions[n] == EActionType.AT_None)
            {
                Speaker.Say(Loc.Get("no_action", n + 1));
                return;
            }
            Speaker.Stop();
            ClickPatches.Click(buttons[n]);
        }

        private static void OpenPause()
        {
            CUIScreen hud = G.Hud;
            if (hud == null) return;
            Transform t = hud.transform.FindChild("GUILayer");
            GameObject pause = FindChildNamed(hud.transform, "Button_Pause");
            if (pause != null)
            {
                ClickPatches.Click(pause);
                return;
            }
            // Fallback: what the pause button does.
            CGameManager.SetPaused(true);
            CUIManager.SetActiveScreen("Pause");
        }

        public static GameObject FindChildNamed(Transform root, string name)
        {
            foreach (Transform t in root.GetComponentsInChildren<Transform>(true))
            {
                if (t.name == name) return t.gameObject;
            }
            return null;
        }

        private static string CardLine()
        {
            return Loc.Get("item_card", CardSummary());
        }

        public static string CardSummary()
        {
            if (!G.InGame) return string.Empty;
            CGame g = G.Game;
            if (g.mpCurrentCard == null) return Loc.Get("card_none");
            if (!FlipCardPatch.Flipped) return CardText.Room(g.mpCurrentCard);
            StatsCardBase c = g.mpCurrentCombat ?? g.mpCurrentCard;
            string text = CardText.Short(c);
            if (g.mlCurrentCombat != null && g.mlCurrentCombat.Count > 1) text += ". " + Loc.Get("card_enemies_left", g.mlCurrentCombat.Count);
            return text;
        }

        public static string CardDetails()
        {
            if (!G.InGame) return string.Empty;
            CGame g = G.Game;
            if (g.mpCurrentCard == null) return Loc.Get("card_none");
            if (!FlipCardPatch.Flipped) return TextUtil.Join(CardText.Room(g.mpCurrentCard), Loc.Get("card_face_down"));
            StatsCardBase c = g.mpCurrentCombat ?? g.mpCurrentCard;
            var parts = new List<string> { CardText.Room(g.mpCurrentCard), CardText.Full(c, R.Get<bool>(g, "mbCardIsEscapeable")) };
            if (g.mlCurrentCombat != null && g.mlCurrentCombat.Count > 1)
            {
                var others = new List<string>();
                foreach (StatsCardBase e in g.mlCurrentCombat) if (e != c) others.Add(CardText.Short(e));
                parts.Add(Loc.Get("card_also_here", string.Join("; ", others.ToArray())));
            }
            if (g.mnRound > 0) parts.Add(Loc.Get("card_round", g.mnRound));
            return TextUtil.Join(parts);
        }

        public override bool HandleKey(KeyCode key, bool shift, bool ctrl)
        {
            if (!G.InGame || ctrl) return false;
            switch (key)
            {
                case KeyCode.Alpha1:
                case KeyCode.Keypad1:
                    DoAction(0);
                    return true;
                case KeyCode.Alpha2:
                case KeyCode.Keypad2:
                    DoAction(1);
                    return true;
                case KeyCode.Alpha3:
                case KeyCode.Keypad3:
                    DoAction(2);
                    return true;
                case KeyCode.Alpha4:
                case KeyCode.Keypad4:
                    DoAction(3);
                    return true;
                case KeyCode.F:
                    TurnCard();
                    return true;
                case KeyCode.S:
                    Speaker.Say(Status.Summary());
                    return true;
                case KeyCode.W:
                    Speaker.Say(TextUtil.Join(Status.Wounds(), Status.Endurance()));
                    return true;
                case KeyCode.H:
                    Speaker.Say(TextUtil.Join(Status.Fighting(), Status.Skill()));
                    return true;
                case KeyCode.T:
                    Speaker.Say(Status.TimeText(G.Game.GameTime));
                    return true;
                case KeyCode.C:
                    Speaker.Say(shift ? CardDetails() : CardDetails());
                    return true;
                case KeyCode.N:
                    Speaker.Say(Status.Deck());
                    return true;
                case KeyCode.D:
                    Speaker.Say(Dice.LastRoll.Length > 0 ? Dice.LastRoll : Loc.Get("no_dice_yet"));
                    return true;
                case KeyCode.R:
                    Speaker.Say(G.MessageText);
                    return true;
                case KeyCode.A:
                    string summary = ActionsSummary();
                    Speaker.Say(summary.Length > 0 ? summary : Loc.Get("no_choices"));
                    return true;
                case KeyCode.P:
                    Speaker.Say(Status.Points());
                    return true;
                case KeyCode.E:
                    if (shift)
                    {
                        CUIScreen hud = G.Hud;
                        if (hud != null && UIScan.IsEnabled(hud.mpEquipmentScreenButton)) ClickPatches.Click(hud.mpEquipmentScreenButton);
                        else Speaker.Say(Loc.Get("state_unavailable"));
                    }
                    else
                    {
                        InfoPanel.Open(() => Loc.Get("panel_equipment"), EquipmentList());
                    }
                    return true;
                case KeyCode.L:
                    InfoPanel.Open(() => Loc.Get("panel_log"), LogList(), startAtEnd: true);
                    return true;
            }
            return false;
        }

        public static List<NavItem> EquipmentList()
        {
            var items = new List<NavItem>();
            if (!G.InGame) return items;
            int i = 0;
            foreach (WeaponInfo w in G.Game.Equipment)
            {
                WeaponInfo item = w;
                items.Add(new NavItem { Key = "eq" + (i++), Text = () => CardText.EquipmentShort(item), Details = () => CardText.EquipmentFull(item, false) });
            }
            if (items.Count == 0) items.Add(NavItem.Label("none", () => Loc.Get("eq_none")));
            return items;
        }

        public static List<NavItem> LogList()
        {
            var items = new List<NavItem>();
            for (int i = 0; i < GameLog.Entries.Count; i++)
            {
                string text = GameLog.Entries[i];
                items.Add(NavItem.Label("log" + i, () => text));
            }
            if (items.Count == 0) items.Add(NavItem.Label("none", () => Loc.Get("log_empty")));
            return items;
        }

        public override bool Back()
        {
            OpenPause();
            return true;
        }

        public override string KeyHelp()
        {
            return Loc.Get("help_board");
        }
    }
}
