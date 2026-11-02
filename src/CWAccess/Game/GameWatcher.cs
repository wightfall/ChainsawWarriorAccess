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

        /// <summary>Reused for the next frame's values, so watching the stats makes no new objects.</summary>
        private static Snapshot spare = new Snapshot();

        /// <summary>Equipment and choices change rarely and cost more to read: they are checked this often (seconds).</summary>
        private const float SlowCheckEvery = 0.15f;

        private static float nextSlowCheck;
        private static Dictionary<WeaponInfo, string> lastEquipment = new Dictionary<WeaponInfo, string>();
        private static long lastActions;
        private static EGameState lastState = EGameState.GS_None;
        private static bool creating;

        /// <summary>The board has just said the choices itself: do not say them again.</summary>
        public static void MarkActionsAnnounced()
        {
            lastActions = ActionsSignature();
        }

        public static void Reset()
        {
            watchedGame = null;
            last = null;
            lastEquipment.Clear();
            lastActions = 0;
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
                last = Take(game, new Snapshot());
                lastEquipment = EquipmentState(game);
                lastActions = ActionsSignature();
                GameLog.Clear();
                return;
            }
            Snapshot now = Take(game, spare);
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
            spare = last;
            last = now;
            float t = UnityEngine.Time.realtimeSinceStartup;
            if (t < nextSlowCheck) return;
            nextSlowCheck = t + SlowCheckEvery;
            if (Settings.AnnounceEquipment.Value) SayEquipmentChanges(game);
            else lastEquipment = EquipmentState(game);
            SayActions();
        }

        /// <summary>Copies the player's values into s and returns it.</summary>
        private static Snapshot Take(CGame g, Snapshot s)
        {
            PlayerStats p = g.mpPlayer;
            s.Wounds = p.mnWounds;
            s.WoundsMax = p.mnWoundsMax;
            s.Endurance = p.mnEndurance;
            s.Poison = p.mnPoison;
            s.Radiation = p.mnRadiation;
            s.HtH = p.mnHtH;
            s.HtHMod = g.mnHtHModifier;
            s.Marksman = p.mnMarksman;
            s.Reflex = p.mnReflex;
            s.Points = p.mnEquipmentPoints;
            s.Time = p.mnCurrentTime;
            s.Skill = p.meSkill;
            s.SkillKnown = g.meGameState > EGameState.GS_StatRolls || p.mnSkill != 0;
            return s;
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
            if (b.Wounds < a.Wounds) Audio.CuePlayer.Play(Audio.Cue.Damage);
            else if (b.Wounds > a.Wounds && a.WoundsMax == b.WoundsMax) Audio.CuePlayer.Play(Audio.Cue.Heal);
            if (b.Poison > a.Poison || b.Radiation > a.Radiation) Audio.CuePlayer.Play(Audio.Cue.Contamination);
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
            // The game's stat titles are plain English strings; spoken in the mod's language when translated.
            string title = stat.ContainsKey("title") ? GameText.Speak(stat["title"]) : var;
            if (var == "EquipmentPoints") title = Loc.Get("stat_points", string.Empty).Trim();
            Say(Loc.Get("stat_rolled", title, value));
            return var;
        }

        private static void SayTime(int before, int after)
        {
            if (before > 60 && after <= 60) Audio.CuePlayer.Play(Audio.Cue.Night);
            int every = Settings.TimeEveryMinutes.Value;
            if (every <= 0 || after >= before || after <= 0) return;
            // One tick is 30 seconds; 120 ticks are the full hour.
            int step = every * 2;
            // Rounded up, so reaching exactly 50:00 counts as passing the 50 minute mark.
            if ((before + step - 1) / step != (after + step - 1) / step)
            {
                Audio.CuePlayer.Play(Audio.Cue.Time);
                Say(Status.TimeText(after));
            }
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
                    if (!SameItemExisted(kv.Key))
                    {
                        Audio.CuePlayer.Play(Audio.Cue.ItemGained);
                        parts.Add(Loc.Get("eq_gained", G.Text(kv.Key.mpEquipment.Name)));
                    }
                }
                else if (before != kv.Value && kv.Value.Length > 0)
                {
                    parts.Add(G.Text(kv.Key.mpEquipment.Name) + ": " + kv.Value);
                }
            }
            foreach (WeaponInfo w in lastEquipment.Keys)
            {
                if (!now.ContainsKey(w) && !SameItemNow(now, w))
                {
                    Audio.CuePlayer.Play(Audio.Cue.ItemLost);
                    parts.Add(Loc.Get("eq_lost", G.Text(w.mpEquipment.Name)));
                }
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

        /// <summary>The choices on offer as one number, in order (0: none); comparing it builds no text.</summary>
        private static long ActionsSignature()
        {
            long sig = 0;
            foreach (EActionType a in G.Actions)
            {
                if (a != EActionType.AT_None) sig = sig * 1000 + (int)a + 1;
            }
            return sig;
        }

        /// <summary>Says the choices once they change (and are not empty).</summary>
        private static void SayActions()
        {
            long key = ActionsSignature();
            if (key == lastActions) return;
            lastActions = key;
            if (key == 0) return;
            // On other screens the board says the choices when it comes back.
            if (!(Nav.Navigator.Context is Board)) return;
            Board.OnActionsChanged();
            Audio.CuePlayer.Play(Audio.Cue.Choices);
            if (!Settings.AnnounceActions.Value) return;
            Speaker.Queue(Board.ActionsSummary());
        }
    }
}
