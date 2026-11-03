using System;
using AurochDigital;
using CWAccess.Game;
using CWAccess.Speech;
using HarmonyLib;
using Steamworks;

namespace CWAccess.Patches
{
    /// <summary>
    /// An achievement the game unlocks during play (winning, Fisto, Chain of Skulls...) is said at once with its name and
    /// description; sighted players see it in Steam's pop-up. Only achievements that were not unlocked before count.
    /// </summary>
    [HarmonyPatch(typeof(StatsAndAchievementsSteam), nameof(StatsAndAchievementsSteam.SetAchievement))]
    internal static class AchievementPatch
    {
        private static void Prefix(StatsAndAchievements __instance, string achievement, out bool __state)
        {
            __state = Had(__instance, achievement);
            // Already unlocked: Steam's report of storing it again must not be announced either.
            if (__state) Achievements.FirstTime(achievement);
        }

        private static void Postfix(StatsAndAchievements __instance, string achievement, bool __state)
        {
            try
            {
                if (__state || !Had(__instance, achievement)) return;
                Achievements.Announce(achievement, "game");
            }
            catch (Exception e)
            {
                Log.Error("AchievementPatch: " + e);
            }
        }

        private static bool Had(StatsAndAchievements stats, string id)
        {
            try
            {
                return stats != null && !string.IsNullOrEmpty(id) && stats.DidGetAchievement(id);
            }
            catch (Exception)
            {
                return false;
            }
        }
    }

    /// <summary>
    /// Steam reports every achievement it stores; with no progress numbers that means it has just been unlocked. This
    /// catches the achievements Steam unlocks by itself from statistics (kill counts, damage taken, endings seen), which
    /// never pass through the game: they are said as soon as Steam has stored them.
    /// </summary>
    [HarmonyPatch(typeof(StatsAndAchievementsSteam), "UserAchievementStored")]
    internal static class AchievementStoredPatch
    {
        private static void Postfix(UserAchievementStored_t callbackResult)
        {
            try
            {
                if (callbackResult.m_nGameID != SteamUtils.GetAppID().m_AppId) return;
                if (!Achievements.IsUnlock(callbackResult.m_nCurProgress, callbackResult.m_nMaxProgress)) return;
                Achievements.Announce(callbackResult.m_rgchAchievementName, "Steam");
            }
            catch (Exception e)
            {
                Log.Error("AchievementStoredPatch: " + e);
            }
        }
    }

    internal static class Achievements
    {
        // Achievements already said (or already unlocked when the game set them again) in this session.
        private static readonly System.Collections.Generic.HashSet<string> said = new System.Collections.Generic.HashSet<string>(StringComparer.OrdinalIgnoreCase);

        /// <summary>True the first time an achievement comes up in this session (the game and Steam both report it).</summary>
        public static bool FirstTime(string id)
        {
            return !string.IsNullOrEmpty(id) && said.Add(id);
        }

        /// <summary>Steam's report means "unlocked" when it has no progress numbers; with numbers it is only progress.</summary>
        public static bool IsUnlock(uint current, uint max)
        {
            return current == 0 && max == 0;
        }

        public static void Announce(string id, string source)
        {
            if (!FirstTime(id)) return;
            Log.Info("Achievement unlocked (" + source + "): " + id);
            Speaker.Queue(Unlocked(id));
        }

        /// <summary>
        /// "Achievement unlocked: title. description": the name and description Steam shows in its pop-up, else the
        /// game's own list (most of its entries have no title), else a readable form of the id.
        /// </summary>
        public static string Unlocked(string id)
        {
            string name = null, desc = null;
            try
            {
                if (SteamManager.Initialized)
                {
                    name = Steamworks.SteamUserStats.GetAchievementDisplayAttribute(id, "name");
                    desc = Steamworks.SteamUserStats.GetAchievementDisplayAttribute(id, "desc");
                }
            }
            catch (Exception)
            {
                // Steam is not running: use the game's texts.
            }
            if (string.IsNullOrEmpty(name))
            {
                foreach (Achievement a in CStatsManager.Achievements)
                {
                    if (!string.Equals(a.ID, id, StringComparison.OrdinalIgnoreCase)) continue;
                    name = G.Text(a.Name);
                    desc = G.Text(a.Description);
                    break;
                }
            }
            if (string.IsNullOrEmpty(name)) name = NameOf(id);
            return TextUtil.Join(Loc.Get("achievement_unlocked", name, desc ?? string.Empty).TrimEnd(' ', '.') + ".");
        }

        /// <summary>
        /// Every Steam achievement of the game as "name, unlocked/locked. description" (the game's own screen shows only
        /// three; the rest are only in Steam's overlay, which screen readers cannot read). Empty without Steam.
        /// </summary>
        public static System.Collections.Generic.List<System.Collections.Generic.KeyValuePair<string, bool>> SteamList()
        {
            var list = new System.Collections.Generic.List<System.Collections.Generic.KeyValuePair<string, bool>>();
            try
            {
                if (!SteamManager.Initialized) return list;
                uint n = Steamworks.SteamUserStats.GetNumAchievements();
                for (uint i = 0; i < n; i++)
                {
                    string id = Steamworks.SteamUserStats.GetAchievementName(i);
                    if (string.IsNullOrEmpty(id)) continue;
                    Steamworks.SteamUserStats.GetAchievement(id, out bool had);
                    string name = Steamworks.SteamUserStats.GetAchievementDisplayAttribute(id, "name");
                    string desc = Steamworks.SteamUserStats.GetAchievementDisplayAttribute(id, "desc");
                    list.Add(new System.Collections.Generic.KeyValuePair<string, bool>(TextUtil.Join(string.IsNullOrEmpty(name) ? NameOf(id) : name, Loc.Get(had ? "ach_unlocked" : "ach_locked"), desc), had));
                }
            }
            catch (Exception e)
            {
                Log.Warn("Steam achievements: " + e.Message);
            }
            return list;
        }

        /// <summary>"ACHIEVEMENT_WIN_EASY" -> "Win easy", when the game's list does not have the achievement.</summary>
        public static string NameOf(string id)
        {
            string s = (id ?? string.Empty).Replace("ACHIEVEMENT_", string.Empty).Replace('_', ' ').Trim().ToLowerInvariant();
            return s.Length > 0 ? char.ToUpperInvariant(s[0]) + s.Substring(1) : s;
        }
    }
}
