using System.Collections.Generic;
using UnityEngine;

namespace CWAccess.Game
{
    /// <summary>Read access to the game's screen manager and current game.</summary>
    internal static class G
    {
        public static CGame Game => CGameManager.mpGame;

        public static bool InGame => CGameManager.mpGame != null && CGameManager.mpGame.mpPlayer != null;

        /// <summary>The active screen object (menu page, game board...).</summary>
        public static CGameScreen CurrentScreen => R.GetStatic<CGameScreen>(typeof(CUIManager), "mpCurrentScreen");

        /// <summary>The name the game registered the active screen under ("MainMenu", "Game", "Pause"...).</summary>
        public static string CurrentScreenName
        {
            get
            {
                CGameScreen cur = CurrentScreen;
                if (cur == null) return null;
                var screens = R.GetStatic<Dictionary<string, GameObject>>(typeof(CUIManager), "mGameScreens");
                if (screens == null) return null;
                foreach (KeyValuePair<string, GameObject> kv in screens)
                {
                    if (kv.Value == cur.gameObject) return kv.Key;
                }
                return null;
            }
        }

        public static bool IsOverlayActive(string name)
        {
            var overlays = R.GetStatic<Dictionary<string, GameObject>>(typeof(CUIManager), "mGameOverlays");
            if (overlays == null || !overlays.ContainsKey(name)) return false;
            GameObject go = overlays[name];
            return go != null && go.activeInHierarchy && CUIManager.IsActiveOverlay(name);
        }

        public static CUIScreen Hud => InGame ? Game.mpUIScreen : null;

        public static CMessageBoxOverlay MessageBox
        {
            get
            {
                GameObject go = CUIManager.GetOverlay("MessageBox");
                return go != null ? go.GetComponent<CMessageBoxOverlay>() : null;
            }
        }

        /// <summary>The text currently shown in the game's message box.</summary>
        public static string MessageText
        {
            get
            {
                CMessageBoxOverlay box = MessageBox;
                if (box == null) return string.Empty;
                // SetText puts the main text into mpModifier and the extra lines into mpGeneral / mpRound.
                return TextUtil.Join(LabelText(R.Get<UILabel>(box, "mpModifier")), LabelText(R.Get<UILabel>(box, "mpGeneral")),
                    LabelText(R.Get<UILabel>(box, "mpRound")));
            }
        }

        public static string LabelText(UILabel l)
        {
            return l == null ? string.Empty : TextUtil.Clean(l.text);
        }

        /// <summary>A text of the game's table as it is on screen (English, not translated for speech).</summary>
        public static string ScreenText(string key)
        {
            if (string.IsNullOrEmpty(key)) return string.Empty;
            string t = CLocalisationManager.GetText(key);
            if (t == null || t.StartsWith("Cannot find Localication Text")) return string.Empty;
            return TextUtil.CleanRaw(t);
        }

        public static string Text(string key)
        {
            if (string.IsNullOrEmpty(key)) return string.Empty;
            string t = CLocalisationManager.GetText(key);
            // The game answers unknown keys with an error sentence.
            if (t == null || t.StartsWith("Cannot find Localication Text")) return string.Empty;
            return TextUtil.CleanRaw(GameText.ForKey(key, t));
        }

        /// <summary>The four action buttons of the game board, in order, with their actions.</summary>
        public static EActionType[] Actions
        {
            get
            {
                CUIScreen hud = Hud;
                return hud == null ? new EActionType[0] : R.Get<EActionType[]>(hud, "mlActions") ?? new EActionType[0];
            }
        }

        public static GameObject[] ActionButtons
        {
            get
            {
                CUIScreen hud = Hud;
                return hud == null ? new GameObject[0] : R.Get<GameObject[]>(hud, "mpButtons") ?? new GameObject[0];
            }
        }

        public static string ActionName(EActionType a)
        {
            int i = (int)a;
            if (i < 0 || i >= CGameManager.mActionNameLookup.Length) return a.ToString();
            string own = Loc.TryGet("action_" + a);
            return own ?? Text(CGameManager.mActionNameLookup[i]);
        }

        /// <summary>True while a scene is loading or screens are fading.</summary>
        public static bool Transitioning
        {
            get
            {
                GameObject alpha = CUIManager.mpAlpha;
                return alpha != null && alpha.activeInHierarchy;
            }
        }
    }
}
