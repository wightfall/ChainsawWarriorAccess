using System;
using System.Collections.Generic;
using System.IO;
using System.Text;

namespace CWAccess
{
    /// <summary>
    /// The mod's own texts. English is built in; a file "strings.txt" next to the mod (lines "key=text",
    /// "\n" for a line break) replaces any of them, so the mod can be translated without rebuilding it.
    /// </summary>
    internal static class Loc
    {
        private static readonly Dictionary<string, string> texts = new Dictionary<string, string>
        {
            ["mod_loaded"] = "Chainsaw Warrior Access loaded. Press F1 for keys.",
            ["speech_using"] = "Speech through {0}.",
            ["dump_done"] = "Screen written to the log.",
            ["and"] = "and",

            // Roles and states
            ["role_button"] = "{0}, button",
            ["role_checkbox"] = "{0}, {1}",
            ["role_radio"] = "{0}, {1}",
            ["role_slider"] = "{0}, slider, {1} percent",
            ["role_number"] = "{0}, {1}",
            ["state_on"] = "on",
            ["state_off"] = "off",
            ["state_unavailable"] = "unavailable",
            ["state_selected"] = "selected",
            ["state_not_selected"] = "not selected",
            ["nothing_here"] = "Nothing here.",
            ["edge_top"] = "Top.",
            ["edge_bottom"] = "Bottom.",
            ["no_back"] = "No way back from here.",
            ["no_item_letter"] = "Nothing starting with {0}.",

            // Screens
            ["screen_GTNSplash"] = "Splash screen. Press any key to continue.",
            ["screen_MainMenu"] = "Main menu",
            ["screen_Difficulty"] = "Choose difficulty",
            ["screen_LoadScreen"] = "Load game",
            ["screen_About"] = "About",
            ["screen_Options"] = "Options",
            ["screen_Credits"] = "Credits",
            ["screen_Help"] = "Help",
            ["screen_Achievement"] = "Achievements and best scores",
            ["screen_LoadScene"] = "Loading",
            ["screen_SteamError"] = "Steam error",
            ["screen_MotionComic"] = "Intro comic",
            ["screen_board"] = "Game board",
            ["screen_Equipment"] = "Equipment cards",
            ["screen_EquipmentSelection"] = "Choose a card",
            ["screen_Pause"] = "Paused",
            ["screen_CardSelect"] = "Choose an enemy",
            ["screen_EndGame"] = "Game over",
            ["screen_Confirm"] = "Question",
            ["comic_intro"] = "The story is told in pictures. Wait for each scene to finish, then press Enter on Next scene, or Escape to skip the comic.",
            ["comic_scene_done"] = "Scene {0} finished. Next scene available.",

            // Named objects without a caption
            ["obj_Button_Pause"] = "Pause menu",
            ["obj_Button_Equipment"] = "Equipment",
            ["obj_Button_Options"] = "Options",
            ["obj_Button_Achievements"] = "Achievements and best scores",
            ["obj_Button_Twitter"] = "Twitter, opens web page",
            ["obj_Button_Facebook"] = "Facebook, opens web page",
            ["obj_Button_Chainsaw_Warrior_LOTN"] = "Chainsaw Warrior: Lords of the Night on Steam, opens web page",
            ["obj_Button_ArrowLeft"] = "Previous card",
            ["obj_Button_ArrowRight"] = "Next card",

            // Board
            ["item_message"] = "Message: {0}",
            ["item_turn_card"] = "Turn over the card. {0}",
            ["item_action"] = "{0}: {1}",
            ["item_card"] = "Card: {0}",
            ["item_equipment_screen"] = "Equipment cards screen",
            ["item_pause"] = "Pause menu",
            ["hint_turn_card"] = "Press F or Enter to turn over the card.",
            ["choice_turn_card"] = "F turn over the card",
            ["choices"] = "Choices: {0}.",
            ["no_choices"] = "No choices right now.",
            ["no_action"] = "No choice {0}.",
            ["cannot_turn"] = "The card cannot be turned now.",
            ["no_dice_yet"] = "No dice rolled yet.",
            ["not_in_game"] = "Not in a game.",
            ["hint_continue"] = "Press Continue to confirm.",

            // Cards
            ["card_room"] = "Room: {0}",
            ["card_room_unknown"] = "unknown room",
            ["card_face_down"] = "The card is face down.",
            ["card_none"] = "No card.",
            ["card_enemy"] = "{0}, {1}",
            ["card_enemy_plain"] = "{0}, enemy",
            ["card_enemy_short"] = "{0}, hand-to-hand {1}",
            ["card_trap"] = "Trap: {0}",
            ["card_trap_short"] = "Trap: {0}",
            ["card_rating"] = "Hand-to-hand rating {0}",
            ["card_can_escape"] = "You can escape from it",
            ["card_no_escape"] = "No escape",
            ["card_enemies_left"] = "{0} enemies here",
            ["card_also_here"] = "Also here: {0}",
            ["card_round"] = "Combat round {0}",
            ["card_class_Empty"] = "Empty room",
            ["card_class_Clear"] = "Clear",
            ["card_class_FirstAid"] = "First aid",
            ["card_class_Dump"] = "Dump",
            ["card_class_Secret"] = "Secret passage",
            ["card_hidden"] = "Face-down card {0} of {1}, {2}, cost {3}",
            ["card_buy"] = "{0}, cost {1}, {2} of {3}",
            ["card_pick"] = "{0}, {1} of {2}",
            ["cards_in_category"] = "{0}: {1} cards",
            ["cards_in_category_one"] = "{0}: 1 card",
            ["tab"] = "{0} tab, {1} points each",
            ["opt_music"] = "Music volume",
            ["opt_sfx"] = "Sound effects volume",
            ["opt_dice"] = "Dice animations",
            ["opt_tutorial"] = "Replay tutorial",
            ["opt_new_equipment"] = "New equipment cards",
            ["tab_one"] = "{0} tab, 1 point each",
            ["tab_current"] = "current",
            ["slot"] = "Slot {0}: {1}",
            ["obj_Button_Select"] = "Show the cards you selected",
            ["cards_face_down"] = "Cards are face down on this difficulty",
            ["cards_selected_list"] = "Your selected cards",
            ["cards_count"] = "{0} cards",
            ["cards_count_one"] = "1 card",
            ["cards_none"] = "No cards to choose from",
            ["category_locked"] = "Only this kind of card can be taken here.",
            ["buy_special"] = "Special cards cannot be bought.",
            ["buy_full"] = "You cannot carry more than 13 cards.",
            ["buy_one_gun"] = "You can have only one gun.",
            ["buy_one_heavy"] = "You can have only one heavy weapon.",
            ["buy_no_points"] = "Not enough points.",

            ["type_Zombie"] = "zombie",
            ["type_ChaosAgent"] = "chaos agent",
            ["type_Door"] = "door",
            ["type_MeatMachine"] = "meat machine",
            ["type_Mutant"] = "mutant",
            ["type_RatPack"] = "rat pack",
            ["hits_Zombie"] = "zombies",
            ["hits_ChaosAgent"] = "chaos agents",
            ["hits_Door"] = "doors",
            ["hits_MeatMachine"] = "meat machines",
            ["hits_Mutant"] = "mutants",
            ["hits_RatPack"] = "rat packs",
            ["type_ContaminatedRatPack"] = "contaminated rat pack",
            ["type_Darkness"] = "Darkness",
            ["type_Slime"] = "slime",

            // Equipment
            ["eq_uses"] = "{0} of {1} uses left",
            ["eq_clips"] = "{0} spare clips",
            ["eq_cats_on"] = "with CATS",
            ["eq_scrambled"] = "scrambled for {0} more turns",
            ["eq_cost"] = "Cost {0} points",
            ["eq_electrical"] = "electrical",
            ["eq_cats_attached"] = "CATS sight attached",
            ["eq_cats_possible"] = "can take a CATS sight",
            ["eq_discard"] = "discarded after use",
            ["eq_gained"] = "Gained {0}",
            ["eq_lost"] = "Lost {0}",
            ["eq_none"] = "No equipment.",
            ["hit_numbers"] = "Score needed with 2 dice: {0}",
            ["hit_entry"] = "{0} {1}",

            // Dice
            ["dice_one"] = "Dice: {0}.",
            ["dice_many"] = "Dice: {0}, total {1}.",
            ["dice_best_of"] = "Rolls: {0}, best {1}.",
            ["dice_plus"] = "Plus {0} makes {1}.",
            ["dice_enemy"] = "Enemy dice: {0}, total {1}.",

            // Stats
            ["stat_wounds"] = "Wounds {0} of {1}",
            ["stat_endurance"] = "Endurance {0}, venom {1}, radiation {2}",
            ["stat_hth"] = "Hand-to-hand {0}",
            ["stat_hth_mod"] = "({0} this fight)",
            ["stat_marksman"] = "Marksmanship {0}",
            ["stat_reflex"] = "Reflexes {0}",
            ["stat_skill"] = "Skill: {0}",
            ["stat_skill_none"] = "Skill not rolled yet",
            ["stat_points"] = "Equipment points {0}",
            ["stat_wounds_change"] = "Wounds {0} of {1} {2}",
            ["stat_endurance_change"] = "Endurance {0} {1}",
            ["stat_venom_change"] = "Venom {0} of {1} {2}",
            ["stat_radiation_change"] = "Radiation {0} of {1} {2}",
            ["stat_hth_change"] = "Hand-to-hand {0}",
            ["stat_hthmod_change"] = "Hand-to-hand modifier {0}",
            ["stat_marksman_change"] = "Marksmanship {0}",
            ["stat_reflex_change"] = "Reflexes {0}",
            ["stat_points_change"] = "Equipment points {0}",
            ["stat_rolled"] = "{0}: {1}",
            ["time_left_day"] = "{0} left",
            ["time_left_night"] = "{0} left, night",
            ["deck_one"] = "deck one",
            ["deck_two"] = "deck two",
            ["deck_position"] = "{0}, card {1} of {2}",
            ["deck_exit_rounds"] = "Escaping the building: {0} cards to go",
            ["deck_bypass"] = "Secret passage: {0} cards can be bypassed",
            ["difficulty_GD_Easy"] = "Easy",
            ["difficulty_GD_Medium"] = "Medium",
            ["difficulty_GD_Classic"] = "Classic",
            ["skill_ST_Marksman"] = "Marksman",
            ["skill_ST_Endurance"] = "Endurance",
            ["skill_ST_Reflexes"] = "Agility",
            ["skill_ST_Melee"] = "Strength",
            ["skill_ST_Climbing"] = "Climbing",
            ["skill_ST_Hiding"] = "Hiding",

            // Achievements
            ["ach_unlocked"] = "unlocked",
            ["ach_locked"] = "locked",

            // Panels
            ["panel_title"] = "{0}, {1} items",
            ["panel_closed"] = "Closed.",
            ["panel_equipment"] = "Your equipment",
            ["panel_log"] = "Game log",
            ["panel_help"] = "Keys",
            ["panel_settings"] = "Mod settings",
            ["log_empty"] = "Nothing has happened yet.",
            ["set_actions"] = "Announce choices",
            ["set_dice"] = "Announce dice",
            ["set_stats"] = "Announce stat changes",
            ["set_equipment"] = "Announce equipment changes",
            ["set_tutorials"] = "Read tutorial boxes",
            ["set_time"] = "Announce time left every so many minutes",
            ["set_log"] = "Write speech to the log file",

            // Help
            ["help_menu"] = "Up and Down arrows, or Tab: move through the screen.\nHome and End: first and last item.\nEnter or Space: press the button.\nLeft and Right arrows: change a slider.\nI: details of the item.\nEscape or Backspace: back.\nA letter: jump to the next item starting with it.",
            ["help_cards"] = "Up and Down arrows: move through the cards and buttons.\nWhen buying equipment: Enter or Space on a card buys it or puts it back; press Continue when done.\nWhen choosing one card (a weapon, an item, an enemy): Enter on a card chooses it at once; Space only selects it.\nI: everything printed on the card.\nLeft and Right arrows: previous or next kind of equipment, where the screen has tabs.\nP: points and items left.\nEscape: back without choosing.",
            ["help_board"] = "Up and Down arrows: move through message, choices, card, status and menus.\nEnter or Space: use the choice.\n1 to 4: use choice 1 to 4 directly.\nF: turn over the card.\nA: say the choices again.\nR: repeat the last message.\nC: describe the card.\nS: full status.\nW: wounds, endurance, venom and radiation.\nH: hand-to-hand, marksmanship, reflexes and skill.\nT: time left.\nN: which card of the deck this is.\nD: last dice roll.\nP: equipment points.\nE: list your equipment; Shift+E: open the equipment cards screen.\nL: game log.\nI: details of the focused item.\nEscape: pause menu.",
            ["help_panel"] = "Up and Down arrows: move through the list.\nI: details.\nEscape: close the list.",
            ["help_global"] = "F1: this help.\nF2: mod settings.\nF5: say where you are.\nF12: find the screen reader again.\nControl: stop speech.",
        };

        private static readonly HashSet<string> reportedMissing = new HashSet<string>();

        public static void LoadOverrides(string dir)
        {
            string path = Path.Combine(dir, "strings.txt");
            if (!File.Exists(path)) return;
            try
            {
                foreach (string raw in File.ReadAllLines(path, Encoding.UTF8))
                {
                    string line = raw.Trim();
                    if (line.Length == 0 || line.StartsWith("#")) continue;
                    int eq = line.IndexOf('=');
                    if (eq <= 0) continue;
                    texts[line.Substring(0, eq).Trim()] = line.Substring(eq + 1).Replace("\\n", "\n");
                }
                Log.Info("Loaded texts from " + path);
            }
            catch (Exception e)
            {
                Log.Error("Could not read " + path + ": " + e.Message);
            }
        }

        public static string TryGet(string key)
        {
            return texts.TryGetValue(key, out string s) ? s : null;
        }

        public static string Get(string key, params object[] args)
        {
            if (!texts.TryGetValue(key, out string s))
            {
                if (reportedMissing.Add(key)) Log.Warn("Missing text: " + key);
                return args.Length == 0 ? key : key + " " + string.Join(" ", Array.ConvertAll(args, a => Convert.ToString(a)));
            }
            if (args.Length == 0) return s;
            try
            {
                return string.Format(s, args);
            }
            catch (FormatException)
            {
                return s;
            }
        }

        /// <summary>All built-in texts, for writing a translation template.</summary>
        public static IEnumerable<KeyValuePair<string, string>> All => texts;
    }
}
