using System;
using System.Reflection;
using HarmonyLib;
using UnityEngine;

namespace CWAccess.Patches
{
    /// <summary>
    /// The game's buttons all work the same way: NGUI sends OnClick to the button, the button forwards
    /// "OnButtonPress" to its screen, and the screen raycasts from the mouse position to find out which
    /// button was hit. To press a button from the keyboard we send OnClick ourselves and, for the duration
    /// of that call, make the raycast report the button we chose.
    /// </summary>
    [HarmonyPatch(typeof(Physics), nameof(Physics.Raycast), new[] { typeof(Ray), typeof(RaycastHit), typeof(float) },
        new[] { ArgumentType.Normal, ArgumentType.Out, ArgumentType.Normal })]
    internal static class ClickPatches
    {
        private static readonly FieldInfo ColliderField = typeof(RaycastHit).GetField("m_Collider", BindingFlags.Instance | BindingFlags.NonPublic);
        private static readonly FieldInfo PointField = typeof(RaycastHit).GetField("m_Point", BindingFlags.Instance | BindingFlags.NonPublic);

        /// <summary>When set, Physics.Raycast(ray, out hit, distance) returns this collider.</summary>
        public static Collider Forced;

        private static bool Prefix(ref RaycastHit hitInfo, ref bool __result)
        {
            if (Forced == null) return true;
            object boxed = new RaycastHit();
            ColliderField.SetValue(boxed, Forced);
            PointField.SetValue(boxed, Forced.transform.position);
            hitInfo = (RaycastHit)boxed;
            __result = true;
            return false;
        }

        /// <summary>Clicks a button exactly as a mouse click would.</summary>
        public static bool Click(GameObject button)
        {
            if (button == null) return false;
            Collider col = button.collider;
            Forced = col;
            try
            {
                // Same sequence as a real mouse click; button messages and sounds may listen to any of them.
                button.SendMessage("OnPress", true, SendMessageOptions.DontRequireReceiver);
                button.SendMessage("OnPress", false, SendMessageOptions.DontRequireReceiver);
                button.SendMessage("OnClick", SendMessageOptions.DontRequireReceiver);
            }
            catch (Exception e)
            {
                Log.Error("Click on " + button.name + " failed: " + e);
                return false;
            }
            finally
            {
                Forced = null;
            }
            return true;
        }

        /// <summary>Runs an action while raycasts report the given collider (for game code that raycasts on its own).</summary>
        public static void WithForcedHit(Collider col, Action action)
        {
            Forced = col;
            try
            {
                action();
            }
            finally
            {
                Forced = null;
            }
        }
    }
}
