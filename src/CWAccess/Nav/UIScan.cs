using System;
using System.Collections.Generic;
using System.Text;
using CWAccess.Patches;
using UnityEngine;

namespace CWAccess.Nav
{
    /// <summary>
    /// Turns any NGUI screen into a list of keyboard items: every clickable control (buttons,
    /// checkboxes, sliders) and every visible text that is not part of a control, in reading order.
    /// </summary>
    internal static class UIScan
    {
        public sealed class Options
        {
            /// <summary>Object names to leave out (e.g. carousel arrows when the cards are listed directly).</summary>
            public HashSet<string> SkipNames = new HashSet<string>();

            /// <summary>Leave out all plain texts.</summary>
            public bool NoTexts;

            /// <summary>Leave out controls under these objects (e.g. card prefabs handled elsewhere).</summary>
            public Func<GameObject, bool> Skip;

            /// <summary>Checkboxes that act as plain buttons (no on/off state is read).</summary>
            public HashSet<string> PlainButtons = new HashSet<string> { "Button_Select" };

            /// <summary>Button name -> name of a label elsewhere on the screen that describes it (read after the button).</summary>
            public Dictionary<string, string> Descriptions = new Dictionary<string, string>();
        }

        private static readonly Options Default = new Options();

        public static List<NavItem> Scan(GameObject root, Options opt = null)
        {
            opt = opt ?? Default;
            var items = new List<NavItem>();
            if (root == null || !root.activeInHierarchy) return items;

            var controlRoots = new HashSet<Transform>();
            var usedLabels = new HashSet<UILabel>();
            var descriptions = new Dictionary<string, UILabel>();
            if (opt.Descriptions.Count > 0)
            {
                foreach (UILabel l in root.GetComponentsInChildren<UILabel>())
                {
                    foreach (KeyValuePair<string, string> kv in opt.Descriptions)
                    {
                        if (l.name == kv.Value)
                        {
                            descriptions[kv.Key] = l;
                            usedLabels.Add(l);
                        }
                    }
                }
            }

            // Sliders first: their thumb / background colliders must not show up as buttons.
            foreach (UISlider slider in root.GetComponentsInChildren<UISlider>())
            {
                if (!slider.gameObject.activeInHierarchy || Skipped(slider.gameObject, opt)) continue;
                controlRoots.Add(slider.transform);
                UISlider s = slider;
                string name = OwnLabel(s.gameObject, usedLabels, false);
                items.Add(new NavItem
                {
                    Key = "slider:" + s.GetInstanceID(),
                    Go = s.gameObject,
                    Position = ScreenPos(s.transform),
                    Text = () => Loc.Role("role_slider", Fallback(name, s.gameObject), Mathf.RoundToInt(s.sliderValue * 100f)),
                    Adjust = dir => AdjustSlider(s, dir),
                });
            }

            foreach (Collider col in root.GetComponentsInChildren<Collider>())
            {
                GameObject go = col.gameObject;
                if (!go.activeInHierarchy || Skipped(go, opt)) continue;
                if (UnderAny(go.transform, controlRoots)) continue;
                UICheckbox box = go.GetComponent<UICheckbox>();
                UIButton button = go.GetComponent<UIButton>();
                bool clickable = box != null || button != null || go.GetComponent<UIButtonMessage>() != null
                                 || go.GetComponent<UIEventListener>() != null || go.GetComponent<UIImageButton>() != null
                                 || go.GetComponent<UIButtonKeyBinding>() != null || go.GetComponent<UIForwardEvents>() != null;
                if (!clickable) continue;
                if (!col.enabled && button == null && box == null) continue;
                controlRoots.Add(go.transform);
                GameObject target = go;
                UICheckbox checkbox = box;
                string own = OwnLabel(go, usedLabels, true);
                var item = new NavItem
                {
                    Key = "ctl:" + go.GetInstanceID(),
                    Go = go,
                    Position = ScreenPos(col),
                    Activate = () => ClickPatches.Click(target),
                };
                if (checkbox != null && !opt.PlainButtons.Contains(go.name))
                {
                    item.IsSwitch = true;
                    item.Text = () => Loc.Role(checkbox.radioButtonRoot != null && !checkbox.optionCanBeNone && IsRadio(checkbox) ? "role_radio" : "role_checkbox",
                        LiveLabel(target, own), Loc.Get(checkbox.isChecked ? "state_on" : "state_off")) + Disabled(target);
                }
                else
                {
                    item.Text = () => Loc.Role("role_button", LiveLabel(target, own)) + Disabled(target);
                }
                if (descriptions.TryGetValue(go.name, out UILabel desc))
                {
                    Func<string> baseText = item.Text;
                    item.Text = () => TextUtil.Join(baseText(), TextUtil.Clean(desc.text));
                }
                items.Add(item);
            }

            if (!opt.NoTexts)
            {
                var seen = new HashSet<string>();
                foreach (UILabel label in root.GetComponentsInChildren<UILabel>())
                {
                    if (usedLabels.Contains(label) || !Visible(label)) continue;
                    if (UnderAny(label.transform, controlRoots) || Skipped(label.gameObject, opt)) continue;
                    string text = TextUtil.Clean(label.text);
                    if (text.Length == 0 || IsNoise(text) || !seen.Add(text)) continue;
                    UILabel l = label;
                    items.Add(new NavItem
                    {
                        Key = "txt:" + l.GetInstanceID(),
                        Go = l.gameObject,
                        Position = ScreenPos(l.transform),
                        Text = () => TextUtil.Clean(l.text),
                    });
                }
            }

            SortReadingOrder(items);
            // Back, Cancel and Quit sit in screen corners; in a list they belong at the end (scrolled texts
            // further down the page would otherwise come after them).
            List<NavItem> quit = items.FindAll(i => i.Go != null && TrailingButtons.Contains(i.Go.name));
            foreach (NavItem q in quit)
            {
                items.Remove(q);
                items.Add(q);
            }
            return items;
        }

        private static readonly HashSet<string> TrailingButtons = new HashSet<string> { "Button_Back", "Button_Cancel", "Button_Quit" };

        private static bool IsRadio(UICheckbox box)
        {
            // NGUI makes every checkbox without a radio root use its parent as one once started; a real radio
            // group has siblings that are checkboxes too.
            Transform root = box.radioButtonRoot;
            if (root == null) return false;
            int count = 0;
            foreach (UICheckbox other in root.GetComponentsInChildren<UICheckbox>()) if (other.radioButtonRoot == root) count++;
            return count > 1;
        }

        private static bool Skipped(GameObject go, Options opt)
        {
            if (opt.SkipNames.Contains(go.name)) return true;
            if (opt.Skip != null && opt.Skip(go)) return true;
            // Scroll bars are not needed: every text is listed whether scrolled into view or not.
            if (go.GetComponentInParentSafe<UIScrollBar>() != null) return true;
            // Cards are always handled by the screen-specific code.
            return go.GetComponentInParentSafe<SelectionCard>() != null;
        }

        private static bool UnderAny(Transform t, HashSet<Transform> roots)
        {
            for (Transform cur = t; cur != null; cur = cur.parent) if (roots.Contains(cur)) return true;
            return false;
        }

        public static bool Visible(UIWidget w)
        {
            if (w == null || !w.enabled || !w.gameObject.activeInHierarchy) return false;
            return w.alpha > 0.01f;
        }

        private static bool IsNoise(string text)
        {
            // Lone punctuation and placeholder texts from the scene files.
            if (text.Length == 1 && !char.IsLetterOrDigit(text[0])) return true;
            return text == "New Label" || text == "Label";
        }

        /// <summary>Texts inside a control (its caption).</summary>
        private static string OwnLabel(GameObject go, HashSet<UILabel> used, bool markUsed)
        {
            var sb = new StringBuilder();
            foreach (UILabel l in go.GetComponentsInChildren<UILabel>())
            {
                if (markUsed) used.Add(l);
                if (!Visible(l)) continue;
                string t = TextUtil.Clean(l.text);
                if (t.Length == 0) continue;
                if (sb.Length > 0) sb.Append(", ");
                sb.Append(t);
            }
            return sb.ToString();
        }

        /// <summary>The caption read now (it may change, e.g. save slot names), falling back to a name.</summary>
        private static string LiveLabel(GameObject go, string atScan)
        {
            var sb = new StringBuilder();
            foreach (UILabel l in go.GetComponentsInChildren<UILabel>())
            {
                if (!Visible(l)) continue;
                string t = TextUtil.Clean(l.text);
                if (t.Length == 0) continue;
                if (sb.Length > 0) sb.Append(", ");
                sb.Append(t);
            }
            string s = sb.Length > 0 ? sb.ToString() : atScan;
            return Fallback(s, go);
        }

        public static string Fallback(string label, GameObject go)
        {
            string named = Loc.TryGet("obj_" + go.name);
            if (named != null) return named;
            if (!string.IsNullOrEmpty(label)) return label;
            return TextUtil.FromObjectName(go.name);
        }

        public static string Disabled(GameObject go)
        {
            Collider c = go.collider;
            UIButton b = go.GetComponent<UIButton>();
            if ((b != null && !b.isEnabled) || (c != null && !c.enabled)) return ", " + Loc.Get("state_unavailable");
            return string.Empty;
        }

        public static bool IsEnabled(GameObject go)
        {
            if (go == null || !go.activeInHierarchy) return false;
            Collider c = go.collider;
            return c == null || c.enabled;
        }

        private static void AdjustSlider(UISlider s, int dir)
        {
            float step = s.numberOfSteps > 1 ? 1f / (s.numberOfSteps - 1) : 0.05f;
            float v = Mathf.Clamp01(s.sliderValue + dir * step);
            s.sliderValue = v;
            Speech.Speaker.Say(Mathf.RoundToInt(s.sliderValue * 100f) + "%");
        }

        public static Vector2 ScreenPos(Transform t)
        {
            Camera cam = UICameraFor(t.gameObject);
            if (cam != null)
            {
                Vector3 p = cam.WorldToScreenPoint(t.position);
                return new Vector2(p.x, p.y);
            }
            return new Vector2(t.position.x * 1000f, t.position.y * 1000f);
        }

        private static Vector2 ScreenPos(Collider c)
        {
            Camera cam = UICameraFor(c.gameObject);
            Vector3 center = c.bounds.center;
            if (cam != null)
            {
                Vector3 p = cam.WorldToScreenPoint(center);
                return new Vector2(p.x, p.y);
            }
            return new Vector2(center.x * 1000f, center.y * 1000f);
        }

        private static Camera UICameraFor(GameObject go)
        {
            Camera cam = NGUITools.FindCameraForLayer(go.layer);
            if (cam == null) cam = CUIManager.mpCamera;
            return cam;
        }

        /// <summary>Top to bottom, then left to right; items within 12 pixels of height count as one row.</summary>
        public static void SortReadingOrder(List<NavItem> items)
        {
            items.Sort((a, b) =>
            {
                if (Mathf.Abs(a.Position.y - b.Position.y) > 12f) return b.Position.y.CompareTo(a.Position.y);
                return a.Position.x.CompareTo(b.Position.x);
            });
        }

        private static T GetComponentInParentSafe<T>(this GameObject go) where T : Component
        {
            for (Transform t = go.transform; t != null; t = t.parent)
            {
                T c = t.GetComponent<T>();
                if (c != null) return c;
            }
            return null;
        }
    }
}
