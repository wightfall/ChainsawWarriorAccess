using System.Collections.Generic;
using CWAccess.Nav;
using CWAccess.Speech;

namespace CWAccess
{
    /// <summary>F2: the mod's options as a list; Enter switches, Left/Right change numbers. Saved at once.</summary>
    internal static class SettingsPanel
    {
        public static void Open()
        {
            var items = new List<NavItem>
            {
                Toggle("set_actions", Settings.AnnounceActions),
                Toggle("set_dice", Settings.AnnounceDice),
                Toggle("set_stats", Settings.AnnounceStats),
                Toggle("set_equipment", Settings.AnnounceEquipment),
                Toggle("set_tutorials", Settings.ReadTutorials),
                Number("set_time", Settings.TimeEveryMinutes, 0, 30, 5),
                Toggle("set_log", Settings.LogSpeech),
            };
            InfoPanel.Open(Loc.Get("panel_settings"), items);
        }

        private static NavItem Toggle(string key, Setting<bool> entry)
        {
            return new NavItem
            {
                Key = key,
                Text = () => Loc.Get("role_checkbox", Loc.Get(key), Loc.Get(entry.Value ? "state_on" : "state_off")),
                Activate = () =>
                {
                    entry.Value = !entry.Value;
                    Log.LogSpeech = Settings.LogSpeech.Value;
                    Speaker.Say(Loc.Get(entry.Value ? "state_on" : "state_off"));
                },
            };
        }

        private static NavItem Number(string key, Setting<int> entry, int min, int max, int step)
        {
            System.Action<int> change = dir =>
            {
                int v = entry.Value + dir * step;
                if (v < min) v = min;
                if (v > max) v = max;
                entry.Value = v;
                Speaker.Say(v == 0 ? Loc.Get("state_off") : v.ToString());
            };
            return new NavItem
            {
                Key = key,
                Text = () => Loc.Get("role_number", Loc.Get(key), entry.Value == 0 ? Loc.Get("state_off") : entry.Value.ToString()),
                Adjust = change,
                Activate = () => change(entry.Value >= max ? -max : 1),
            };
        }
    }
}
