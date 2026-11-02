using System.Collections.Generic;
using System.Linq;
using CWAccess.Game;
using CWAccess.Nav;
using CWAccess.Patches;
using CWAccess.Speech;
using UnityEngine;

namespace CWAccess.Screens
{
    /// <summary>
    /// Base for the card pickers. The game shows two cards at a time in a carousel and selects them by
    /// mouse click; here every card of the list is an item, Enter selects it, and the screen's buttons follow.
    /// </summary>
    internal abstract class CardListScreen : GenericScreen
    {
        protected CardListScreen(string name, GameObject root) : base(name, root)
        {
            options.NoTexts = true;
            options.SkipNames.Add("Button_ArrowLeft");
            options.SkipNames.Add("Button_ArrowRight");
            backButtons = new[] { "Button_Cancel" };
            backHelp = "keys_back_cancel";
        }

        public override int InitialFocus(List<NavItem> items)
        {
            int i = items.FindIndex(x => x.Key.StartsWith("card"));
            return i >= 0 ? i : 0;
        }

        protected static void SetGlow<T>(GameObject screenRoot, System.Func<T, bool> isSelected) where T : SelectionCard
        {
            foreach (T card in screenRoot.GetComponentsInChildren<T>())
            {
                EquipmentCard ec = card as EquipmentCard;
                if (ec != null) ec.SetGlowState(isSelected(card));
            }
        }

        protected static void EnableContinue(GameObject cont, bool on)
        {
            if (cont == null) return;
            UIButton b = cont.GetComponent<UIButton>();
            if (b != null) b.isEnabled = on;
        }

        /// <summary>F1 on a card picker (a weapon, an item, an enemy...): only what this one offers.</summary>
        public override List<string> HelpLines(IList<NavItem> items)
        {
            var lines = new List<string> { Loc.Get("keys_cards_move") };
            if (items.Any(i => IsCard(i) && i.Activate != null)) lines.Add(Loc.Get("keys_choose_card"));
            if (items.Any(i => IsCard(i) && i.Toggle != null)) lines.Add(Loc.Get("keys_select_card"));
            if (items.Any(i => IsCard(i) && i.Details != null)) lines.Add(Loc.Get("keys_card_details"));
            if (items.Any(i => i != null && i.Key != null && i.Key.StartsWith("tab"))) lines.Add(Loc.Get("keys_tab_enter"));
            string back = BackHelp();
            if (back != null) lines.Add(back);
            return lines;
        }

        protected static bool IsCard(NavItem i)
        {
            return i != null && i.Key != null && i.Key.StartsWith("card");
        }

        /// <summary>Equipment kinds in the order of the game's tabs.</summary>
        protected static readonly string[] TabCodes = { "C", "D", "G", "H", "W", "S" };

        /// <summary>The six equipment tabs as items: name, cost per card and which one is showing.</summary>
        protected static List<NavItem> TabItems(GameObject[] tabs, System.Func<string> currentCode)
        {
            var items = new List<NavItem>();
            for (int i = 0; i < TabCodes.Length && i < tabs.Length; i++)
            {
                GameObject tab = tabs[i];
                if (tab == null || !tab.activeInHierarchy) continue;
                string code = TabCodes[i];
                StatsEquipmentType type = CStatsManager.GetEquipmentTypeFromCode(code);
                items.Add(new NavItem
                {
                    Key = "tab" + code,
                    Go = tab,
                    Position = UIScan.ScreenPos(tab.transform),
                    Text = () => Loc.Get(type != null && type.Cost == 1 ? "tab_one" : "tab", CardText.CategoryName(code), type != null ? type.Cost : 0)
                                 + (currentCode() == code ? ", " + Loc.Get("tab_current") : string.Empty),
                    Activate = () =>
                    {
                        ClickPatches.Click(tab);
                        Navigator.Rebuild(keepFocus: true);
                        Speaker.Say(Loc.Get("tab_current"));
                    },
                });
            }
            return items;
        }

        protected static string CountText(string category, int count)
        {
            return count == 1 ? Loc.Get("cards_in_category_one", category) : Loc.Get("cards_in_category", category, count);
        }
    }

    /// <summary>Buying equipment with points (start of the game, leaving the house) or taking an item from a dump.</summary>
    internal sealed class EquipmentBuyScreen : CardListScreen
    {
        private readonly CEquipmentScreen screen;

        public EquipmentBuyScreen(GameObject root) : base("Equipment", root)
        {
            screen = root.GetComponent<CEquipmentScreen>();
            options.Skip = go => { int i = System.Array.IndexOf(Tabs, go); return i >= 0 && i < 6; };
        }

        private GameObject[] Tabs => R.Get<GameObject[]>(screen, "mpRadioButtons") ?? new GameObject[0];

        private bool Revealed => G.InGame && G.Game.CardsRevealed;

        public override string Intro()
        {
            return TextUtil.Join(InfoText(), CategoryText());
        }

        private string InfoText()
        {
            return TextUtil.Join(G.LabelText(R.Get<UILabel>(screen, "mpTextPoints")), G.LabelText(R.Get<UILabel>(screen, "mpTextInfo")));
        }

        private string CurrentCode()
        {
            var codes = R.Get<List<string>>(screen, "mlAllowedCodes");
            return codes != null && codes.Count > 0 ? codes[0] : null;
        }

        private string CategoryText()
        {
            List<StatsEquipmentBase> cards = screen.mlEquipmentCards;
            string code = CurrentCode();
            string cat = code != null ? CardText.CategoryName(code) : string.Empty;
            int count = cards != null ? cards.Count : 0;
            // "Show the cards you selected" lists your picks of every kind under the old tab.
            if (cards != null && cards.Exists(c => c.Code != code)) cat = Loc.Get("cards_selected_list");
            return CountText(cat, count) + (Revealed ? string.Empty : ". " + Loc.Get("cards_face_down"));
        }

        public override List<NavItem> Build()
        {
            var items = new List<NavItem> { NavItem.Label("info", InfoText) };
            List<StatsEquipmentBase> cards = screen.mlEquipmentCards;
            if (cards != null)
            {
                for (int i = 0; i < cards.Count; i++)
                {
                    StatsEquipmentBase e = cards[i];
                    int n = i + 1;
                    int total = cards.Count;
                    items.Add(new NavItem
                    {
                        Key = "card" + e.Name,
                        Text = () => CardLabel(e, n, total),
                        Details = () => Revealed ? CardText.EquipmentFull(new WeaponInfo(e), true) : CardLabel(e, n, total),
                        Activate = () => Toggle(e),
                    });
                }
            }
            if (!R.Get<bool>(screen, "mbLockToggle")) items.AddRange(TabItems(Tabs, CurrentCode));
            items.AddRange(base.Build());
            return items;
        }

        private string CardLabel(StatsEquipmentBase e, int n, int total)
        {
            bool selected = screen.mlSelectedCards.Contains(e);
            string state = selected ? ", " + Loc.Get("state_selected") : string.Empty;
            if (!Revealed) return Loc.Get("card_hidden", n, total, CardText.CategoryName(e.Code), CardText.Cost(e)) + state;
            return Loc.Get("card_buy", G.Text(e.Name), CardText.Cost(e), n, total) + state;
        }

        /// <summary>The game's click logic for a card on this screen, with spoken reasons when it refuses.</summary>
        private void Toggle(StatsEquipmentBase e)
        {
            CGame game = G.Game;
            if (game == null) return;
            List<StatsEquipmentBase> selected = screen.mlSelectedCards;
            int cost = CardText.Cost(e);
            bool pointBased = screen.PointSelection;
            if (e.Code == "S")
            {
                CSoundManager.PlaySFX("SFX_Fail");
                Speaker.Say(Loc.Get("buy_special"));
                return;
            }
            int current = R.Get<int>(screen, "mnEquipmentCurrent");
            int guns = R.Get<int>(screen, "mnGuns");
            int heavies = R.Get<int>(screen, "mnHeavies");
            int total = R.Get<int>(screen, "mnTotal");
            int selectedCost = screen.CurrentCost;
            if (!selected.Contains(e))
            {
                string refuse = null;
                if (total >= 13) refuse = "buy_full";
                else if (e.Code == "G" && guns > 0) refuse = "buy_one_gun";
                else if (e.Code == "W" && heavies > 0) refuse = "buy_one_heavy";
                else if (pointBased && selectedCost + cost > game.EquipmentPoints) refuse = "buy_no_points";
                if (refuse != null)
                {
                    CSoundManager.PlaySFX("SFX_Fail");
                    Audio.CuePlayer.Play(Audio.Cue.Unavailable);
                    Speaker.Say(Loc.Get(refuse));
                    return;
                }
                if (pointBased)
                {
                    R.Set(screen, "mnSelectedCost", selectedCost + cost);
                    selected.Add(e);
                    current++;
                }
                else
                {
                    selected.Clear();
                    current = 1;
                    selected.Add(e);
                }
                if (e.Code == "G") guns = 1;
                else if (e.Code == "W") heavies = 1;
                CSoundManager.PlaySFX("SFX_Select");
                if (R.Get<bool>(screen, "mbRequiresSelect")) EnableContinue(R.Get<GameObject>(screen, "mpContinue"), true);
            }
            else
            {
                CSoundManager.PlaySFX("SFX_DeSelect");
                R.Set(screen, "mnSelectedCost", selectedCost - cost);
                selected.Remove(e);
                if (R.Get<bool>(screen, "mbRequiresSelect") && selected.Count == 0) EnableContinue(R.Get<GameObject>(screen, "mpContinue"), false);
                if (e.Code == "G") guns = 0;
                else if (e.Code == "W") heavies = 0;
                current--;
            }
            R.Set(screen, "mnEquipmentCurrent", current);
            R.Set(screen, "mnGuns", guns);
            R.Set(screen, "mnHeavies", heavies);
            R.Set(screen, "mnTotal", game.Equipment.Count + current);
            R.Call(screen, "SetTextItems");
            if (CGameManager.meGameDifficulty == EGameDifficulty.GD_Medium)
            {
                GameObject reveal = R.Get<GameObject>(screen, "mpReveal");
                EnableContinue(reveal, game.EquipmentPoints - screen.CurrentCost >= 2);
            }
            SetGlow<EquipmentCard>(root, c => c.mpStatsCard != null && selected.Contains(c.mpStatsCard.mpEquipment));
            Audio.CuePlayer.Play(selected.Contains(e) ? Audio.Cue.Select : Audio.Cue.Deselect);
            Speaker.Say(TextUtil.Join(Loc.Get(selected.Contains(e) ? "state_selected" : "state_not_selected"), G.LabelText(R.Get<UILabel>(screen, "mpTextPoints"))));
        }

        /// <summary>F1 in the equipment shop or at a dump.</summary>
        public override List<string> HelpLines(IList<NavItem> items)
        {
            var lines = new List<string> { Loc.Get("keys_cards_move") };
            if (items.Any(IsCard))
            {
                lines.Add(Loc.Get(R.Get<bool>(screen, "mbRequiresSelect") ? "keys_take_card" : "keys_buy_card"));
                if (items.Any(i => IsCard(i) && i.Details != null)) lines.Add(Loc.Get("keys_card_details"));
            }
            if (!R.Get<bool>(screen, "mbLockToggle")) lines.Add(Loc.Get("keys_tabs"));
            lines.Add(Loc.Get("keys_points"));
            string back = BackHelp();
            if (back != null) lines.Add(back);
            return lines;
        }

        public override bool HandleKey(KeyCode key, bool shift, bool ctrl)
        {
            if (key == KeyCode.LeftArrow || key == KeyCode.RightArrow)
            {
                NavItem f = Navigator.Focused;
                if (f != null && f.Adjust != null) return false;
                SwitchCategory(key == KeyCode.RightArrow ? 1 : -1);
                return true;
            }
            if (key == KeyCode.P)
            {
                Speaker.Say(InfoText());
                return true;
            }
            return false;
        }

        private void SwitchCategory(int dir)
        {
            if (R.Get<bool>(screen, "mbLockToggle"))
            {
                Speaker.Say(TextUtil.Join(Loc.Get("category_locked"), CategoryText()));
                return;
            }
            string code = CurrentCode();
            int i = System.Array.IndexOf(TabCodes, code);
            int next = (i + dir + TabCodes.Length) % TabCodes.Length;
            GameObject[] radios = Tabs;
            if (radios == null || radios[next] == null) return;
            ClickPatches.Click(radios[next]);
            Navigator.Rebuild(keepFocus: false);
            int first = Navigator.Items.Count > 1 && Navigator.Items[1].Key.StartsWith("card") ? 1 : 0;
            Navigator.FocusIndex(first, speak: false);
            NavItem f = Navigator.Focused;
            Speaker.Say(TextUtil.Join(CategoryText(), f != null && f.Key.StartsWith("card") ? f.Speak() : null));
        }
    }

    /// <summary>Choosing one of your own cards: weapon to shoot with, item to use, item to drop, clip to refill...</summary>
    internal sealed class EquipmentPickScreen : CardListScreen
    {
        private readonly CEquipmentSelectionScreen screen;

        public EquipmentPickScreen(GameObject root) : base("EquipmentSelection", root)
        {
            screen = root.GetComponent<CEquipmentSelectionScreen>();
            options.Skip = go => System.Array.IndexOf(Tabs, go) >= 0;
        }

        private GameObject[] Tabs => new[]
        {
            R.Get<GameObject>(screen, "mpC"), R.Get<GameObject>(screen, "mpD"), R.Get<GameObject>(screen, "mpG"),
            R.Get<GameObject>(screen, "mpH"), R.Get<GameObject>(screen, "mpW"), R.Get<GameObject>(screen, "mpS"),
        };

        private string CurrentCode()
        {
            var codes = R.Get<List<string>>(screen, "mlAllowedCodes");
            return codes != null && codes.Count == 1 ? codes[0] : null;
        }

        private List<WeaponInfo> Cards => R.Get<List<WeaponInfo>>(screen, "mlEquipmentAllowed") ?? new List<WeaponInfo>();

        private WeaponInfo Selected => R.Get<WeaponInfo>(screen, "mpSelectedStats");

        public override string Title()
        {
            return Loc.Get("screen_EquipmentSelection");
        }

        public override string Intro()
        {
            List<WeaponInfo> cards = Cards;
            string info = G.LabelText(R.Get<UILabel>(screen, "mpTextInfo"));
            return TextUtil.Join(info, cards.Count == 0 ? Loc.Get("cards_none") : cards.Count == 1 ? Loc.Get("cards_count_one") : Loc.Get("cards_count", cards.Count));
        }

        private string shownCards;

        public override void OnEnter()
        {
            shownCards = CardsSignature();
        }

        /// <summary>Which cards the list holds (the game can swap them while the screen stays open).</summary>
        private string CardsSignature()
        {
            var names = new List<string>();
            foreach (WeaponInfo w in Cards) names.Add(w.mpEquipment.Name);
            return string.Join("|", names.ToArray());
        }

        /// <summary>
        /// The game reuses this screen for a second question without leaving it (after choosing C.A.T.S it asks for
        /// the weapon to attach it to): when the cards change, start again at the first card and say the new question.
        /// </summary>
        public override void Tick()
        {
            string now = CardsSignature();
            if (now == shownCards) return;
            shownCards = now;
            Navigator.Rebuild(keepFocus: false);
            Navigator.FocusIndex(InitialFocus(new List<NavItem>(Navigator.Items)), speak: false);
            NavItem f = Navigator.Focused;
            Speaker.Queue(TextUtil.Join(Intro(), f != null && f.Key.StartsWith("card") ? f.Speak() : null));
        }

        public override List<NavItem> Build()
        {
            var items = new List<NavItem> { NavItem.Label("info", () => G.LabelText(R.Get<UILabel>(screen, "mpTextInfo"))) };
            List<WeaponInfo> cards = Cards;
            for (int i = 0; i < cards.Count; i++)
            {
                WeaponInfo w = cards[i];
                int n = i + 1;
                int total = cards.Count;
                items.Add(new NavItem
                {
                    Key = "card" + i + w.mpEquipment.Name,
                    Text = () => Loc.Get("card_pick", CardText.EquipmentShort(w), n, total) + (Selected == w ? ", " + Loc.Get("state_selected") : string.Empty),
                    Details = () => CardText.EquipmentFull(w, false),
                    Activate = () => Choose(w),
                    Toggle = () => Toggle(w),
                });
            }
            if (!R.Get<bool>(screen, "mbLockToggle")) items.AddRange(TabItems(Tabs, CurrentCode));
            items.AddRange(base.Build());
            return items;
        }

        /// <summary>Enter on a card: select it and press Continue (when the screen has one).</summary>
        private void Choose(WeaponInfo w)
        {
            if (Selected != w) Toggle(w, quiet: true);
            GameObject cont = R.Get<GameObject>(screen, "mpContinue");
            if (cont != null && cont.activeInHierarchy && UIScan.IsEnabled(cont)) ClickPatches.Click(cont);
            else Speaker.Say(Loc.Get("state_selected"));
        }

        private void Toggle(WeaponInfo w)
        {
            Toggle(w, quiet: false);
        }

        private void Toggle(WeaponInfo w, bool quiet)
        {
            bool requires = R.Get<bool>(screen, "mbRequiresSelect");
            GameObject cont = R.Get<GameObject>(screen, "mpContinue");
            if (Selected == w)
            {
                R.Set(screen, "mpSelectedStats", null);
                R.Set(screen, "mpSelected", null);
                CSoundManager.PlaySFX("SFX_DeSelect");
                Audio.CuePlayer.Play(Audio.Cue.Deselect);
                if (requires) EnableContinue(cont, false);
                SetGlow<EquipmentCard>(root, c => false);
                Speaker.Say(Loc.Get("state_not_selected"));
                return;
            }
            R.Set(screen, "mpSelectedStats", w);
            EquipmentCard shown = null;
            foreach (EquipmentCard c in root.GetComponentsInChildren<EquipmentCard>()) if (c.mpStatsCard == w) shown = c;
            R.Set(screen, "mpSelected", shown);
            SetGlow<EquipmentCard>(root, c => c.mpStatsCard == w);
            CSoundManager.PlaySFX("SFX_Select");
            Audio.CuePlayer.Play(Audio.Cue.Select);
            if (requires) EnableContinue(cont, true);
            bool canContinue = cont != null && cont.activeInHierarchy;
            if (!quiet) Speaker.Say(TextUtil.Join(Loc.Get("state_selected"), canContinue ? Loc.Get("hint_continue") : null));
        }
    }

    /// <summary>Choosing which of several enemies to fight.</summary>
    internal sealed class CardPickScreen : CardListScreen
    {
        private readonly CCardSelectScreen screen;

        public CardPickScreen(GameObject root) : base("CardSelect", root)
        {
            screen = root.GetComponent<CCardSelectScreen>();
        }

        private StatsCardBase Selected => R.Get<StatsCardBase>(screen, "mpSelectedStats");

        public override string Title()
        {
            string t = G.LabelText(R.Get<UILabel>(screen, "mpTitleText"));
            return t.Length > 0 ? t : Loc.Get("screen_CardSelect");
        }

        public override string Intro()
        {
            return G.LabelText(R.Get<UILabel>(screen, "mpInfoText"));
        }

        public override List<NavItem> Build()
        {
            var items = new List<NavItem> { NavItem.Label("info", Intro) };
            List<StatsCardBase> cards = screen.mlHouseCards;
            for (int i = 0; i < cards.Count; i++)
            {
                StatsCardBase c = cards[i];
                int n = i + 1;
                int total = cards.Count;
                items.Add(new NavItem
                {
                    Key = "card" + i + c.Identifier,
                    Text = () => Loc.Get("card_pick", CardText.Short(c), n, total) + (Selected == c ? ", " + Loc.Get("state_selected") : string.Empty),
                    Details = () => CardText.Full(c, c.Escape),
                    Activate = () => Choose(c),
                    Toggle = () => Toggle(c, quiet: false),
                });
            }
            items.AddRange(base.Build());
            return items;
        }

        /// <summary>Enter on an enemy: fight this one.</summary>
        private void Choose(StatsCardBase c)
        {
            if (Selected != c) Toggle(c, quiet: true);
            GameObject cont = R.Get<GameObject>(screen, "mpContinue");
            if (cont != null && cont.activeInHierarchy && UIScan.IsEnabled(cont)) ClickPatches.Click(cont);
        }

        private void Toggle(StatsCardBase c, bool quiet)
        {
            bool requires = R.Get<bool>(screen, "mbRequiresSelect");
            GameObject cont = R.Get<GameObject>(screen, "mpContinue");
            if (Selected == c)
            {
                R.Set(screen, "mpSelectedStats", null);
                R.Set(screen, "mpSelected", null);
                if (requires) EnableContinue(cont, false);
                Speaker.Say(Loc.Get("state_not_selected"));
                return;
            }
            R.Set(screen, "mpSelectedStats", c);
            HouseCard shown = null;
            foreach (HouseCard h in root.GetComponentsInChildren<HouseCard>()) if (h.mpStatsCard == c) shown = h;
            R.Set(screen, "mpSelected", shown);
            if (requires) EnableContinue(cont, true);
            if (!quiet) Speaker.Say(TextUtil.Join(Loc.Get("state_selected"), Loc.Get("hint_continue")));
        }
    }
}
