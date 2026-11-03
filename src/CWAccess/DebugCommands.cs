using System;
using System.Collections.Generic;
using System.IO;
using System.Text;
using CWAccess.Game;
using CWAccess.Nav;
using CWAccess.Speech;
using UnityEngine;

namespace CWAccess
{
    /// <summary>
    /// Developer testing only (setting CommandFile, off by default): reads commands from CWAccess\commands.txt so
    /// the mod can be driven, and rare game situations set up, without the game window having focus.
    /// Commands, one per line:
    ///   key UpArrow [shift] [ctrl]   press a key            say text        speak
    ///   dump / dumpgo name           log the screen / an object tree
    ///   state                        log the screen and game state
    ///   endscreen [type]             show the game-over screen (no scores or Steam stats are written)
    ///   speech host|auto             force the SAPI/JAWS helper / back to automatic
    ///   dumptexts / dumplabels       write the game's text table / every label of the scene to the mod folder
    ///   dumpcards                    write all house and equipment cards to the mod folder
    ///   card id                      put that house card on top of the deck (while a card lies face down)
    ///   give name                    add an equipment card (part of its name) to the player
    ///   stat field value             set a PlayerStats field (mnWounds, mnPoison, mnCurrentTime ...)
    ///   skill ST_Climbing            change the player's skill
    ///   gamestate GS_ResponceLucky   set the game's state (rare branches)
    ///   screen name                  switch to a screen of the scene (SteamError, LoadScreen ...)
    ///   gametemplate                 rewrite Languages\game\template.txt
    ///   shot name                    screenshot
    /// </summary>
    internal static class DebugCommands
    {
        private static float nextPoll;
        private static string path;

        public static void Tick()
        {
            if (!Settings.CommandFile.Value) return;
            if (Time.realtimeSinceStartup < nextPoll) return;
            nextPoll = Time.realtimeSinceStartup + 0.2f;
            if (path == null) path = Path.Combine(Entry.ModDir, "commands.txt");
            if (!File.Exists(path)) return;
            string[] lines;
            try
            {
                lines = File.ReadAllLines(path);
                File.Delete(path);
            }
            catch (Exception)
            {
                return;
            }
            foreach (string raw in lines)
            {
                string line = raw.Trim();
                if (line.Length == 0) continue;
                Log.Info("[cmd] " + line);
                try
                {
                    Run(line);
                }
                catch (Exception e)
                {
                    Log.Error("Command failed: " + e);
                }
            }
        }

        private static string Rest(string line, string[] p)
        {
            return p.Length > 1 ? line.Substring(p[0].Length + 1).Trim() : string.Empty;
        }

        private static void Run(string line)
        {
            string[] p = line.Split(' ');
            switch (p[0])
            {
                case "key":
                    {
                        var key = (KeyCode)Enum.Parse(typeof(KeyCode), p[1], true);
                        bool shift = Array.IndexOf(p, "shift") > 0;
                        bool ctrl = Array.IndexOf(p, "ctrl") > 0;
                        AccessController.InjectKey(key, shift, ctrl);
                        break;
                    }
                case "dump":
                    Diagnostics.DumpScreen();
                    break;
                case "state":
                    Log.Info("Screen " + G.CurrentScreenName + " context " + (Navigator.Context != null ? Navigator.Context.Id : "none")
                             + (G.InGame ? " game state " + G.Game.meGameState + " actions " + string.Join(",", Array.ConvertAll(G.Actions, a => a.ToString())) : string.Empty));
                    break;
                case "endscreen":
                    {
                        GameObject end = CUIManager.GetScreen("EndGame");
                        if (end == null) break;
                        var type = p.Length > 1 ? (EEndGameType)Enum.Parse(typeof(EEndGameType), p[1], true) : EEndGameType.lose_time;
                        CEndScreen screen = end.GetComponent<CEndScreen>();
                        screen.SetCompetitionState(false);
                        screen.SetEndText(type, 1234);
                        CUIManager.SetActiveScreen("EndGame", 0.1f, 0.1f, false, true);
                        break;
                    }
                case "speech":
                    Speaker.Force(p.Length > 1 ? p[1] : string.Empty);
                    break;
                case "dumptexts":
                    {
                        var table = GameText.Table;
                        var sb = new StringBuilder();
                        if (table != null) foreach (var kv in table) sb.Append(kv.Key).Append(" = ").Append(kv.Value.Replace("\r", "").Replace("\n", "\\n")).Append('\n');
                        File.WriteAllText(Path.Combine(Entry.ModDir, "game-texts.txt"), sb.ToString(), new UTF8Encoding(false));
                        Log.Info("Wrote " + (table != null ? table.Count : 0) + " game texts");
                        break;
                    }
                case "dumplabels":
                    {
                        // Every label text loaded in this scene (active or not) that is not in the game's text table.
                        var known = new HashSet<string>();
                        if (GameText.Table != null) foreach (string v in GameText.Table.Values) known.Add(TextUtil.CleanRaw(v));
                        var found = new SortedDictionary<string, string>(StringComparer.Ordinal);
                        foreach (UnityEngine.Object o in Resources.FindObjectsOfTypeAll(typeof(UILabel)))
                        {
                            var l = o as UILabel;
                            if (l == null) continue;
                            string t = TextUtil.CleanRaw(l.text);
                            if (t.Length == 0 || known.Contains(t)) continue;
                            found[t] = l.name;
                        }
                        var sb = new StringBuilder();
                        foreach (var kv in found) sb.Append(kv.Key).Append("\t").Append(kv.Value).Append('\n');
                        File.WriteAllText(Path.Combine(Entry.ModDir, "labels-" + Application.loadedLevelName + ".txt"), sb.ToString(), new UTF8Encoding(false));
                        Log.Info("Wrote " + found.Count + " labels of scene " + Application.loadedLevelName);
                        break;
                    }
                case "dumpcards":
                    DumpCards();
                    break;
                case "card":
                    ForceCard(Rest(line, p));
                    break;
                case "give":
                    Give(Rest(line, p));
                    break;
                case "stat":
                    {
                        System.Reflection.FieldInfo f = typeof(PlayerStats).GetField(p[1]);
                        if (f == null || !G.InGame) break;
                        f.SetValue(G.Game.mpPlayer, Convert.ChangeType(p[2], f.FieldType));
                        Log.Info("Set " + p[1] + " = " + p[2]);
                        break;
                    }
                case "gamestate":
                    // Testing rare branches (lucky shot after a tied round ...): sets the game's state directly.
                    if (G.InGame) G.Game.GameState = (EGameState)Enum.Parse(typeof(EGameState), p[1], true);
                    break;
                case "skill":
                    if (G.InGame) G.Game.Skill = (ESkillType)Enum.Parse(typeof(ESkillType), p[1], true);
                    break;
                case "screen":
                    CUIManager.SetActiveScreen(p[1], 0.1f, 0.1f, false, true);
                    break;
                case "shot":
                    Application.CaptureScreenshot(Path.Combine(Entry.ModDir, (p.Length > 1 ? p[1] : "shot") + ".png"));
                    break;
                case "gametemplate":
                    Log.Info("Wrote " + GameText.WriteTemplate(Entry.ModDir));
                    break;
                case "dumpgo":
                    Diagnostics.DumpObject(Rest(line, p));
                    break;
                case "achievements":
                    // Read only: the game's achievements and whether Steam has them unlocked.
                    foreach (Achievement a in CStatsManager.Achievements)
                    {
                        bool had = AurochDigital.StatsAndAchievements.instance != null && AurochDigital.StatsAndAchievements.instance.DidGetAchievement(a.ID);
                        Log.Info("achievement " + a.ID + " = " + G.Text(a.Name) + (had ? " (unlocked)" : string.Empty) + ": " + Patches.Achievements.Unlocked(a.ID));
                    }
                    break;
                case "achievementprogress":
                    {
                        // Shows Steam's progress pop-up for an achievement (changes nothing): Steam then reports it,
                        // which checks that the mod hears Steam's reports and says them.
                        string id = p.Length > 1 ? p[1] : "ACHIEVEMENT_KILLS_TOTAL";
                        Log.Info("IndicateAchievementProgress " + id + ": " + Steamworks.SteamUserStats.IndicateAchievementProgress(id, 1, 100));
                        break;
                    }
                case "achievementsay":
                    // What the mod says when this achievement is unlocked (speaks only; Steam is not touched).
                    Speaker.Say(Patches.Achievements.Unlocked(p.Length > 1 ? p[1] : "ACHIEVEMENT_WIN_HARD"));
                    break;
                case "say":
                    Speaker.Say(Rest(line, p));
                    break;
                case "perf":
                    Perf.Start(Time.realtimeSinceStartup, p.Length > 1 ? float.Parse(p[1], System.Globalization.CultureInfo.InvariantCulture) : 10f);
                    break;
            }
        }

        private static void DumpCards()
        {
            var sb = new StringBuilder();
            foreach (StatsCardBase c in CStatsManager.AllHouseCards) sb.Append(CardLine(c)).Append('\n');
            CardData data = CStatsManager.Data;
            if (data != null && data.WanderingZombies != null)
            {
                foreach (StatsEnemy z in data.WanderingZombies) sb.Append("WANDERING ").Append(CardLine(z)).Append('\n');
            }
            foreach (StatsEquipmentBase e in CStatsManager.GetAllEquipment)
            {
                var effects = new List<string>();
                if (e.CardEffects != null) foreach (CardEffect ce in e.CardEffects) effects.Add(ce.IType + "/" + ce.IAbility + "/" + ce.IStat);
                sb.Append("EQUIP ").Append(e.Name).Append(" | ").Append(e.Code).Append(" | uses ").Append(e.Uses).Append(" | cats ").Append(e.CATS)
                  .Append(" | new ").Append(e.New).Append(" | ").Append(string.Join(",", effects.ToArray())).Append('\n');
            }
            foreach (string code in new[] { "C", "D", "G", "H", "W", "S" })
            {
                StatsEquipmentType t = CStatsManager.GetEquipmentTypeFromCode(code);
                if (t != null) sb.Append("TYPE ").Append(code).Append(" | ").Append(t.Name).Append(" | ").Append(t.Cost).Append('\n');
            }
            File.WriteAllText(Path.Combine(Entry.ModDir, "cards.txt"), sb.ToString(), new UTF8Encoding(false));
            Log.Info("Wrote cards.txt");
        }

        private static string CardLine(StatsCardBase c)
        {
            var effects = new List<string>();
            var enemy = c as StatsEnemy;
            if (enemy != null && enemy.EncounterEffects != null) foreach (CombatEffect e in enemy.EncounterEffects) effects.Add("enc:" + e.TType + "=" + e.TValue);
            if (enemy != null && enemy.MeleeEffects != null) foreach (CombatEffect e in enemy.MeleeEffects) effects.Add("melee:" + e.TType + "=" + e.TValue);
            if (enemy != null && enemy.DeathEffects != null) foreach (CombatEffect e in enemy.DeathEffects) effects.Add("death:" + e.TType + "=" + e.TValue);
            var trap = c as StatsTrap;
            if (trap != null && trap.TrapEffects != null) foreach (CombatEffect e in trap.TrapEffects) effects.Add("trap:" + e.TType + "=" + e.TValue);
            return c.Identifier + " | " + c.Class + " | " + c.Type + " | " + c.Name + " | back " + c.CardBack + " | rating " + c.Rating + " | escape " + c.Escape
                   + " | n " + c.NumberAppearing + " | " + string.Join(",", effects.ToArray());
        }

        /// <summary>Puts a house card on top of the current deck and lays it face down (only while a card is face down).</summary>
        private static void ForceCard(string id)
        {
            if (!G.InGame || G.Game.meGameState != EGameState.GS_PreAction)
            {
                Log.Warn("card: only while a card lies face down");
                return;
            }
            CGame g = G.Game;
            StatsCardBase card = CStatsManager.AllHouseCards.Find(c => c.Identifier == id || c.Name == id);
            if (card == null)
            {
                Log.Warn("card: no card " + id);
                return;
            }
            // The current face-down card goes back into the deck under the forced one.
            g.mlCurrentDeck.Remove(card);
            g.mlCurrentDeck.Insert(0, card);
            g.GameState = EGameState.GS_SetCard;
            Log.Info("Card on top: " + id);
        }

        private static void Give(string name)
        {
            if (!G.InGame) return;
            StatsEquipmentBase e = CStatsManager.GetAllEquipment.Find(x => x.Name.IndexOf(name, StringComparison.OrdinalIgnoreCase) >= 0
                                                                          || G.Text(x.Name).IndexOf(name, StringComparison.OrdinalIgnoreCase) >= 0);
            if (e == null)
            {
                Log.Warn("give: no equipment " + name);
                return;
            }
            G.Game.Equipment.Add(new WeaponInfo(e));
            Log.Info("Gave " + e.Name);
        }
    }
}
