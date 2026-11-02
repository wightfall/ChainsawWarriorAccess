using System;
using System.Collections.Generic;
using CWAccess.Audio;
using CWAccess.Nav;
using CWAccess.Speech;

namespace CWAccess
{
    /// <summary>
    /// F2: every option of the mod as a list. Enter switches on/off, Left/Right change numbers and the language;
    /// on a sound cue's volume Enter plays the cue. Everything is saved at once to CWAccess.ini.
    /// </summary>
    internal static class SettingsPanel
    {
        private static float resetArmedUntil;

        public static void Open()
        {
            InfoPanel.Open(() => Loc.Get("panel_settings"), Build());
        }

        /// <summary>The items; every text is looked up when spoken, so a language change shows at once.</summary>
        internal static List<NavItem> Build()
        {
            var items = new List<NavItem>
            {
                Heading("set_group_speech"),
                LanguageItem(),
                Toggle("set_roles", Settings.SpeakRoles),
                Toggle("set_actions", Settings.AnnounceActions),
                Toggle("set_dice", Settings.AnnounceDice),
                Toggle("set_stats", Settings.AnnounceStats),
                Toggle("set_equipment", Settings.AnnounceEquipment),
                Toggle("set_room", Settings.AnnounceRoom),
                Toggle("set_fullcard", Settings.FullCardOnTurn),
                Toggle("set_tutorials", Settings.ReadTutorials),
                Toggle("set_repeat", Settings.RepeatMessageOnReturn),
                Toggle("set_comic", Settings.DescribeComic),
                Number("set_time", Settings.TimeEveryMinutes, 0, 30, 5, null),
                Heading("set_group_sound"),
                Toggle("set_cues", Settings.SoundCues),
                Number("set_cue_volume", Settings.CueVolume, 0, 100, 10, Cue.Activate),
            };
            foreach (Cue c in Enum.GetValues(typeof(Cue)))
            {
                Cue cue = c;
                Setting<int> s = Settings.CueVolumes[c];
                NavItem item = Number("cue_" + c, s, 0, 100, 10, cue);
                // The cue's name is looked up each time: it must follow a change of language made in this list.
                item.Text = () => Loc.Get("set_cue_item", CueName(cue), s.Value);
                item.Details = () => Loc.TryGet("cuedesc_" + cue) ?? CueName(cue);
                items.Add(item);
            }
            items.Add(Heading("set_group_other"));
            items.Add(Toggle("set_log", Settings.LogSpeech));
            items.Add(new NavItem
            {
                Key = "reset",
                Text = () => Loc.Get("set_reset"),
                Activate = Reset,
            });
            return items;
        }

        public static string CueName(Cue c)
        {
            return Loc.TryGet("cue_" + c) ?? c.ToString();
        }

        private static NavItem Heading(string key)
        {
            return NavItem.Label(key, () => Loc.Get(key));
        }

        private static NavItem Toggle(string key, Setting<bool> entry)
        {
            return new NavItem
            {
                Key = key,
                Text = () => Loc.Role("role_checkbox", Loc.Get(key), Loc.Get(entry.Value ? "state_on" : "state_off")),
                Activate = () =>
                {
                    entry.Value = !entry.Value;
                    Log.LogSpeech = Settings.LogSpeech.Value;
                    // The sound cue setting's own cue plays after switching it on.
                    CuePlayer.Play(entry.Value ? Cue.ToggleOn : Cue.ToggleOff);
                    Speaker.Say(Loc.Get(entry.Value ? "state_on" : "state_off"));
                },
            };
        }

        private static NavItem Number(string key, Setting<int> entry, int min, int max, int step, Cue? preview)
        {
            Action<int> change = dir =>
            {
                int v = Math.Max(min, Math.Min(max, entry.Value + dir * step));
                entry.Value = v;
                if (preview.HasValue) CuePlayer.Preview(preview.Value);
                Speaker.Say(v == 0 && min == 0 && !preview.HasValue ? Loc.Get("state_off") : v.ToString());
            };
            return new NavItem
            {
                Key = key,
                Text = () => Loc.Role("role_number", Loc.Get(key), entry.Value == 0 && !preview.HasValue ? Loc.Get("state_off") : entry.Value.ToString()),
                Adjust = change,
                Activate = () =>
                {
                    if (preview.HasValue) CuePlayer.Preview(preview.Value);
                    else change(entry.Value >= max ? -(max - min) / step : 1);
                },
            };
        }

        /// <summary>Left/Right or Enter go through English and every language file; the change is immediate.</summary>
        private static NavItem LanguageItem()
        {
            Action<int> change = dir =>
            {
                List<Loc.Choice> list = Loc.Available();
                var codes = new List<string> { "auto" };
                foreach (Loc.Choice c in list) codes.Add(c.Code);
                int i = codes.FindIndex(c => string.Equals(c, Settings.Language.Value, StringComparison.OrdinalIgnoreCase));
                i = (i < 0 ? 0 : i + dir + codes.Count) % codes.Count;
                Settings.Language.Value = codes[i];
                Loc.Use(codes[i]);
                Speaker.Say(LanguageText());
            };
            return new NavItem
            {
                Key = "language",
                Text = () => Loc.Role("role_number", Loc.Get("set_language"), LanguageText()),
                Adjust = change,
                Activate = () => change(1),
            };
        }

        private static string LanguageText()
        {
            string setting = Settings.Language.Value;
            if (string.Equals(setting, "auto", StringComparison.OrdinalIgnoreCase)) return Loc.Get("set_language_auto", Loc.LanguageName);
            return Loc.LanguageName;
        }

        /// <summary>Enter twice within 3 seconds puts every option back to its default.</summary>
        private static void Reset()
        {
            float now = UnityEngine.Time.realtimeSinceStartup;
            if (now > resetArmedUntil)
            {
                resetArmedUntil = now + 3f;
                Speaker.Say(Loc.Get("set_reset_confirm"));
                return;
            }
            resetArmedUntil = 0f;
            Settings.ResetAll();
            Loc.Use(Settings.Language.Value);
            Speaker.Say(Loc.Get("set_reset_done"));
        }
    }
}
