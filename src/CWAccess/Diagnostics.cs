using System.Text;
using CWAccess.Game;
using CWAccess.Nav;
using CWAccess.Speech;
using UnityEngine;

namespace CWAccess
{
    /// <summary>Ctrl+Shift+D: writes the current screen's object tree to the BepInEx log (for fixing labels).</summary>
    internal static class Diagnostics
    {
        public static void DumpScreen()
        {
            var sb = new StringBuilder();
            sb.AppendLine("=== Screen dump: " + G.CurrentScreenName + " / context " + (Navigator.Context != null ? Navigator.Context.Id : "none"));
            if (G.InGame) sb.AppendLine("Game state: " + G.Game.meGameState + " actions: " + string.Join(",", System.Array.ConvertAll(G.Actions, a => a.ToString())));
            CGameScreen cur = G.CurrentScreen;
            if (cur != null) Dump(cur.transform, 0, sb);
            foreach (string o in new[] { "Confirm", "MessageBox", "UIScreen" })
            {
                GameObject go = CUIManager.GetOverlay(o);
                if (go != null && go.activeInHierarchy)
                {
                    sb.AppendLine("--- overlay " + o);
                    Dump(go.transform, 0, sb);
                }
            }
            sb.AppendLine("--- items");
            foreach (NavItem i in Navigator.Items) sb.AppendLine(i.Key + " | " + i.Speak());
            Log.Info(sb.ToString());
            Speaker.Say(Loc.Get("dump_done"));
        }

        public static void DumpObject(string name)
        {
            var sb = new StringBuilder();
            GameObject go = GameObject.Find(name);
            sb.AppendLine("=== Object dump: " + name + (go == null ? " (not found)" : string.Empty));
            if (go != null) Dump(go.transform, 0, sb);
            Log.Info(sb.ToString());
        }

        private static void Dump(Transform t, int depth, StringBuilder sb)
        {
            if (depth > 14) return;
            sb.Append(' ', depth * 2).Append(t.name);
            if (!t.gameObject.activeSelf) sb.Append(" [inactive]");
            foreach (Component c in t.GetComponents<Component>())
            {
                if (c == null || c is Transform) continue;
                sb.Append(" <").Append(c.GetType().Name);
                TextMesh tm = c as TextMesh;
                if (tm != null) sb.Append(" \"").Append(tm.text).Append("\"");
                UILabel l = c as UILabel;
                if (l != null) sb.Append(" \"").Append(TextUtil.Clean(l.text)).Append("\" a=").Append(l.alpha.ToString("0.##"));
                Collider col = c as Collider;
                if (col != null && !col.enabled) sb.Append(" disabled");
                UISprite s = c as UISprite;
                if (s != null) sb.Append(" ").Append(s.spriteName);
                UICheckbox cb = c as UICheckbox;
                if (cb != null) sb.Append(cb.isChecked ? " checked" : " unchecked");
                UIButtonMessage m = c as UIButtonMessage;
                if (m != null) sb.Append(" ").Append(m.functionName).Append("@").Append(m.trigger).Append("->").Append(m.target != null ? m.target.name : "self");
                sb.Append(">");
            }
            sb.AppendLine();
            for (int i = 0; i < t.childCount; i++) Dump(t.GetChild(i), depth + 1, sb);
        }
    }
}
