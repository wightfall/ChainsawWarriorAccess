using System.Collections.Generic;

namespace CWAccess.Game
{
    /// <summary>Spoken summaries of the player's state (what the top bar, clock and gauges show).</summary>
    internal static class Status
    {
        public static string TimeText(int ticks)
        {
            int minutes = ticks / 2;
            string clock = minutes + ":" + (ticks % 2 == 1 ? "30" : "00");
            return Loc.Get(ticks <= 60 ? "time_left_night" : "time_left_day", clock);
        }

        public static string SkillName(ESkillType skill)
        {
            string t = G.Text(skill.ToString());
            if (t.Length > 0) return t;
            return Loc.TryGet("skill_" + skill) ?? skill.ToString();
        }

        public static string Wounds()
        {
            PlayerStats p = G.Game.mpPlayer;
            return Loc.Get("stat_wounds", p.mnWounds, p.mnWoundsMax);
        }

        public static string Endurance()
        {
            PlayerStats p = G.Game.mpPlayer;
            return Loc.Get("stat_endurance", p.mnEndurance, p.mnPoison, p.mnRadiation);
        }

        public static string Fighting()
        {
            CGame g = G.Game;
            PlayerStats p = g.mpPlayer;
            var parts = new List<string>();
            string hth = Loc.Get("stat_hth", p.mnHtH);
            if (g.mnHtHModifier != 0) hth += " " + Loc.Get("stat_hth_mod", GameWatcher.Signed(g.mnHtHModifier));
            parts.Add(hth);
            parts.Add(Loc.Get("stat_marksman", GameWatcher.Signed(p.mnMarksman)));
            parts.Add(Loc.Get("stat_reflex", p.mnReflex));
            return TextUtil.Join(parts);
        }

        public static string Skill()
        {
            CGame g = G.Game;
            if (g.meGameState <= EGameState.GS_StatRolls && g.mpPlayer.mnSkill == 0 && g.mnStatVar <= 5) return Loc.Get("stat_skill_none");
            return Loc.Get("stat_skill", SkillName(g.mpPlayer.meSkill));
        }

        public static string Points()
        {
            return Loc.Get("stat_points", G.Game.mpPlayer.mnEquipmentPoints);
        }

        public static string Deck()
        {
            CGame g = G.Game;
            if (g.mlCurrentDeck == null || g.mlCurrentDiscard == null) return string.Empty;
            int current = g.mlCurrentDiscard.Count + 1;
            int total = g.mlCurrentDiscard.Count + g.mlCurrentDeck.Count;
            string deck = Loc.Get(g.mpPlayer.mbSecondDeck ? "deck_two" : "deck_one");
            string text = Loc.Get("deck_position", deck, current, total);
            if (g.ExitRounds >= 0) text += ". " + Loc.Get("deck_exit_rounds", g.ExitRounds + 1);
            if (g.BypassRounds > 0) text += ". " + Loc.Get("deck_bypass", g.BypassRounds);
            return text;
        }

        /// <summary>Everything at once (S key).</summary>
        public static string Summary()
        {
            if (!G.InGame) return Loc.Get("not_in_game");
            CGame g = G.Game;
            return TextUtil.Join(Wounds(), Endurance(), Fighting(), Skill(), TimeText(g.GameTime), Points(),
                Loc.Get("difficulty_" + g.Difficulty));
        }
    }
}
