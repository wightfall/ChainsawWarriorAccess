using System.Collections.Generic;
using CWAccess.Speech;

namespace CWAccess.Game
{
    /// <summary>
    /// Compares the player's numbers every frame and says what changed: wounds, endurance, venom,
    /// radiation, skills, equipment points, time, equipment, and the choices offered.
    /// </summary>
    internal static class GameWatcher
    {
        private sealed class Snapshot
        {
            public int Wounds, WoundsMax, Endurance, Poison, Radiation, HtH, HtHMod, Marksman, Reflex, Points, Time;
            public ESkillType Skill;
            public bool SkillKnown;
        }

        private static CGame watchedGame;
        private static Snapshot last;
        private static Dictionary<WeaponInfo, string> lastEquipment = new Dictionary<WeaponInfo, string>();
        private static string lastActions = string.Empty;
        private static EGameState lastState = EGameState.GS_None;
        private static bool creating;

        /// <summary>The board has just said the choices itself: do not say them again.</summary>
        public static void MarkActionsAnnounced()
        {
            lastActions = ActionsKey();
        }

        public static void Reset()
        {
            watchedGame = null;
            last = null;
            lastEquipment.Clear();
            lastActions = string.Empty;
        }

        public static void Tick()
        {
            if (!G.InGame)
            {
                if (watchedGame != null) Reset();
                return;
            }
            CGame game = G.Game;
            if (game != watchedGame)
            {
                watchedGame = game;
                last = Take(game);
                lastEquipment = EquipmentState(game);
                lastActions = ActionsKey();
                GameLog.Clear();
                return;
            }
            Snapshot now = Take(game);
            creating = game.meGameState <= EGameState.GS_InitEnd;
            bool rolled = game.meGameState == EGameState.GS_RollEnd && lastState != EGameState.GS_RollEnd;
            lastState = game.meGameState;
            if (rolled)
            {
                string var = SayCreationRoll(game);
                // Side effects of the roll (the Strength skill adds 1 to hand-to-hand) are still said.
                if (Settings.AnnounceStats.Value) SayStatChanges(last, now, var);
            }
            else if (Settings.AnnounceStats.Value) SayStatChanges(last, now, null);
            SayTime(last.Time, now.Time);
            last = now;
            if (Settings.AnnounceEquipment.Value) SayEquipmentChanges(game);
            else lastEquipment = EquipmentState(game);
            SayActions();
        }

        private static Snapshot Take(CGame g)
        {
            PlayerStats p = g.mpPlayer;
            return new Snapshot
            {
                Wounds = p.mnWounds,
                WoundsMax = p.mnWoundsMax,
                Endurance = p.mnEndurance,
                Poison = p.mnPoison,
                Radiation = p.mnRadiation,
                HtH = p.mnHtH,
                HtHMod = g.mnHtHModifier,
                Marksman = p.mnMarksman,
                Reflex = p.mnReflex,
                Points = p.mnEquipmentPoints,
                Time = p.mnCurrentTime,
                Skill = p.meSkill,
                SkillKnown = g.meGameState > EGameState.GS_StatRolls || p.mnSkill != 0,
            };
        }

        private static void Say(string text)
        {
            GameLog.Add(text);
            Speaker.Queue(text);
        }

        private static void SayStatChanges(Snapshot a, Snapshot b, string except)
        {
            var parts = new List<string>();
            if (except == "Endurance") a.Endurance = b.Endurance;
            if (except == "InitialWoundsMax")
            {
                a.Wounds = b.Wounds;
                a.WoundsMax = b.WoundsMax;
            }
            if (except == "HtH") a.HtH = b.HtH;
            if (except == "InitialMarksman") a.Marksman = b.Marksman;
            if (except == "Reflex") a.Reflex = b.Reflex;
            if (except == "EquipmentPoints") a.Points = b.Points;
            if (a.Wounds != b.Wounds || a.WoundsMax != b.WoundsMax) parts.Add(Loc.Get("stat_wounds_change", b.Wounds, b.WoundsMax, Delta(b.Wounds - a.Wounds)));
            if (a.Endurance != b.Endurance) parts.Add(Loc.Get("stat_endurance_change", b.Endurance, Delta(b.Endurance - a.Endurance)));
            if (a.Poison != b.Poison) parts.Add(Loc.Get("stat_venom_change", b.Poison, b.Endurance, Delta(b.Poison - a.Poison)));
            if (a.Radiation != b.Radiation) parts.Add(Loc.Get("stat_radiation_change", b.Radiation, b.Endurance, Delta(b.Radiation - a.Radiation)));
            if (a.HtH != b.HtH) parts.Add(Loc.Get("stat_hth_change", b.HtH));
            if (a.HtHMod != b.HtHMod && b.HtHMod != 0) parts.Add(Loc.Get("stat_hthmod_change", Signed(b.HtHMod)));
            if (a.Marksman != b.Marksman) parts.Add(Loc.Get("stat_marksman_change", Signed(b.Marksman)));
            if (a.Reflex != b.Reflex) parts.Add(Loc.Get("stat_reflex_change", b.Reflex));
            if (a.Points != b.Points) parts.Add(Loc.Get("stat_points_change", b.Points));
            if (parts.Count > 0) Say(TextUtil.Join(parts));
        }

        /// <summary>Character creation: say the stat just rolled, even when it is 0 or unchanged.</summary>
        private static string SayCreationRoll(CGame g)
        {
            int i = g.mnStatVar - 1;
            if (g.mpPlayer.mlStats == null || i < 0 || i >= g.mpPlayer.mlStats.Count) return null;
            System.Collections.Generic.Dictionary<string, string> stat = g.mpPlayer.mlStats[i];
            string var = stat.ContainsKey("var") ? stat["var"] : string.Empty;
            PlayerStats p = g.mpPlayer;
            string value;
            switch (var)
            {
                case "Endurance": value = p.mnEndurance.ToString(); break;
                case "InitialWoundsMax": value = p.mnWoundsMax.ToString(); break;
                case "HtH": value = p.mnHtH.ToString(); break;
                case "InitialMarksman": value = Signed(p.mnMarksman); break;
                case "Reflex": value = p.mnReflex.ToString(); break;
                case "InitialSkill": value = Status.SkillName(p.meSkill); break;
                case "EquipmentPoints": value = p.mnEquipmentPoints.ToString(); break;
                default: return null;
            }
            string title = stat.ContainsKey("title") ? stat["title"] : var;
            if (var == "EquipmentPoints") title = Loc.Get("stat_points", string.Empty).Trim();
            Say(Loc.Get("stat_rolled", title, value));
            return var;
        }

        private static void SayTime(int before, int after)
        {
            int every = Settings.TimeEveryMinutes.Value;
            if (every <= 0 || after >= before || after <= 0) return;
            // One tick is 30 seconds; 120 ticks are the full hour.
            int step = every * 2;
            // Rounded up, so reaching exactly 50:00 counts as passing the 50 minute mark.
            if ((before + step - 1) / step != (after + step - 1) / step) Say(Status.TimeText(after));
        }

        private static string Delta(int d)
        {
            if (d == 0 || creating) return string.Empty;
            return "(" + (d > 0 ? "+" : string.Empty) + d + ")";
        }

        public static string Signed(int v)
        {
            return v > 0 ? "+" + v : v.ToString();
        }

        private static Dictionary<WeaponInfo, string> EquipmentState(CGame g)
        {
            var d = new Dictionary<WeaponInfo, string>();
            foreach (WeaponInfo w in g.Equipment) d[w] = string.Join(", ", new List<string>(CardText.State(w)).ToArray());
            return d;
        }

        private static void SayEquipmentChanges(CGame g)
        {
            Dictionary<WeaponInfo, string> now = EquipmentState(g);
            var parts = new List<string>();
            foreach (KeyValuePair<WeaponInfo, string> kv in now)
            {
                if (!lastEquipment.TryGetValue(kv.Key, out string before))
                {
                    // Copies of the same card are new objects when a game is loaded; only real gains count.
                    if (!SameItemExisted(kv.Key)) parts.Add(Loc.Get("eq_gained", G.Text(kv.Key.mpEquipment.Name)));
                }
                else if (before != kv.Value && kv.Value.Length > 0)
                {
                    parts.Add(G.Text(kv.Key.mpEquipment.Name) + ": " + kv.Value);
                }
            }
            foreach (WeaponInfo w in lastEquipment.Keys)
            {
                if (!now.ContainsKey(w) && !SameItemNow(now, w)) parts.Add(Loc.Get("eq_lost", G.Text(w.mpEquipment.Name)));
            }
            lastEquipment = now;
            if (parts.Count > 0) Say(TextUtil.Join(parts));
        }

        private static bool SameItemExisted(WeaponInfo w)
        {
            foreach (WeaponInfo old in lastEquipment.Keys) if (old.mpEquipment == w.mpEquipment) return true;
            return false;
        }

        private static bool SameItemNow(Dictionary<WeaponInfo, string> now, WeaponInfo w)
        {
            foreach (WeaponInfo cur in now.Keys) if (cur.mpEquipment == w.mpEquipment) return true;
            return false;
        }

        private static string ActionsKey()
        {
            var names = new List<string>();
            foreach (EActionType a in G.Actions)
            {
                if (a != EActionType.AT_None) names.Add(a.ToString());
            }
            return string.Join(",", names.ToArray());
        }

        /// <summary>Says the choices once they change (and are not empty).</summary>
        private static void SayActions()
        {
            string key = ActionsKey();
            if (key == lastActions) return;
            lastActions = key;
            if (key.Length == 0) return;
            // On other screens the board says the choices when it comes back.
            if (!(Nav.Navigator.Context is Board)) return;
            Board.OnActionsChanged();
            if (!Settings.AnnounceActions.Value) return;
            Speaker.Queue(Board.ActionsSummary());
        }
    }
}
