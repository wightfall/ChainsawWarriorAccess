using System.Collections.Generic;
using CWAccess.Game;
using CWAccess.Nav;
using CWAccess.Speech;
using UnityEngine;

namespace CWAccess
{
    /// <summary>Reads the keyboard every frame and drives the navigator and the announcements.</summary>
    internal static class AccessController
    {
        private static readonly KeyCode[] Watched =
        {
            KeyCode.UpArrow, KeyCode.DownArrow, KeyCode.LeftArrow, KeyCode.RightArrow, KeyCode.Home, KeyCode.End,
            KeyCode.Return, KeyCode.KeypadEnter, KeyCode.Space, KeyCode.Escape, KeyCode.Backspace, KeyCode.Tab,
            KeyCode.F1, KeyCode.F2, KeyCode.F5, KeyCode.F12, KeyCode.I,
            KeyCode.Alpha1, KeyCode.Alpha2, KeyCode.Alpha3, KeyCode.Alpha4,
            KeyCode.Keypad1, KeyCode.Keypad2, KeyCode.Keypad3, KeyCode.Keypad4,
            KeyCode.A, KeyCode.B, KeyCode.C, KeyCode.D, KeyCode.E, KeyCode.F, KeyCode.G, KeyCode.H, KeyCode.J, KeyCode.K,
            KeyCode.L, KeyCode.M, KeyCode.N, KeyCode.O, KeyCode.P, KeyCode.Q, KeyCode.R, KeyCode.S, KeyCode.T, KeyCode.U,
            KeyCode.V, KeyCode.W, KeyCode.X, KeyCode.Y, KeyCode.Z,
        };

        private static KeyCode heldKey = KeyCode.None;
        private static float nextRepeat;

        public static void Update()
        {
            ScreenTracker.Tick();
            GameWatcher.Tick();
            Navigator.Tick();
            DebugCommands.Tick();
            HandleInput();
        }

        /// <summary>A key from the debug command file, handled as if it was pressed.</summary>
        public static void InjectKey(KeyCode key, bool shift, bool ctrl)
        {
            OnKey(key, shift, ctrl);
        }

        private static void HandleInput()
        {
            bool shift = Input.GetKey(KeyCode.LeftShift) || Input.GetKey(KeyCode.RightShift);
            bool ctrl = Input.GetKey(KeyCode.LeftControl) || Input.GetKey(KeyCode.RightControl);
            bool alt = Input.GetKey(KeyCode.LeftAlt) || Input.GetKey(KeyCode.RightAlt);

            if (Input.GetKeyDown(KeyCode.LeftControl) || Input.GetKeyDown(KeyCode.RightControl)) Speaker.Stop();
            if (alt) return;

            foreach (KeyCode key in Watched)
            {
                if (Input.GetKeyDown(key))
                {
                    heldKey = key;
                    nextRepeat = Time.realtimeSinceStartup + 0.45f;
                    OnKey(key, shift, ctrl);
                    return;
                }
            }

            // Holding Up / Down repeats, like in a normal list.
            if (heldKey == KeyCode.UpArrow || heldKey == KeyCode.DownArrow)
            {
                if (!Input.GetKey(heldKey))
                {
                    heldKey = KeyCode.None;
                }
                else if (Time.realtimeSinceStartup >= nextRepeat)
                {
                    nextRepeat = Time.realtimeSinceStartup + 0.12f;
                    Navigator.Move(heldKey == KeyCode.UpArrow ? -1 : 1);
                }
            }
        }

        private static void OnKey(KeyCode key, bool shift, bool ctrl)
        {
            ScreenContext ctx = Navigator.Context;

            // Keys that work everywhere.
            switch (key)
            {
                case KeyCode.F1:
                    if (shift) KeyHelp.ShowAll(ctx);
                    else KeyHelp.ShowScreen(ctx);
                    return;
                case KeyCode.F2:
                    SettingsPanel.Open();
                    return;
                case KeyCode.F12:
                    Speaker.Detect();
                    Speaker.Say(Loc.Get("speech_using", Speaker.BackendName));
                    return;
                case KeyCode.F5:
                    if (ctx != null) Navigator.AnnounceScreen(interrupt: true);
                    return;
            }
            if (ctrl && shift && key == KeyCode.D)
            {
                Diagnostics.DumpScreen();
                return;
            }
            if (ctx == null) return;

            if (ctx.HandleKey(key, shift, ctrl)) return;

            switch (key)
            {
                case KeyCode.UpArrow:
                    Navigator.Move(-1);
                    return;
                case KeyCode.DownArrow:
                    Navigator.Move(1);
                    return;
                case KeyCode.Tab:
                    Navigator.Move(shift ? -1 : 1);
                    return;
                case KeyCode.Home:
                    Navigator.Home();
                    return;
                case KeyCode.End:
                    Navigator.End();
                    return;
                case KeyCode.Return:
                case KeyCode.KeypadEnter:
                    Navigator.Activate();
                    return;
                case KeyCode.Space:
                    Navigator.ToggleFocused();
                    return;
                case KeyCode.LeftArrow:
                    Navigator.Adjust(-1);
                    return;
                case KeyCode.RightArrow:
                    Navigator.Adjust(1);
                    return;
                case KeyCode.I:
                    Navigator.Details();
                    return;
                case KeyCode.Escape:
                case KeyCode.Backspace:
                    if (!ctx.Back() && key == KeyCode.Escape) Speaker.Say(Loc.Get("no_back"));
                    return;
            }

            // Letters jump to the next item starting with that letter (menus only; the board uses letters as commands).
            if (key >= KeyCode.A && key <= KeyCode.Z && !ctrl)
            {
                char c = (char)('a' + (key - KeyCode.A));
                if (!Navigator.TypeAhead(c)) Speaker.Say(Loc.Get("no_item_letter", char.ToUpperInvariant(c)));
            }
        }


    }
}
