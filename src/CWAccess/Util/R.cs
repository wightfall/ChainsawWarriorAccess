using System;
using System.Collections.Generic;
using System.Reflection;

namespace CWAccess
{
    /// <summary>Cached reflection for the game's private fields and methods.</summary>
    internal static class R
    {
        private const BindingFlags All = BindingFlags.Instance | BindingFlags.Static | BindingFlags.Public | BindingFlags.NonPublic;
        private static readonly Dictionary<string, FieldInfo> fields = new Dictionary<string, FieldInfo>();
        private static readonly Dictionary<string, MethodInfo> methods = new Dictionary<string, MethodInfo>();

        public static FieldInfo Field(Type t, string name)
        {
            string key = t.FullName + "." + name;
            if (!fields.TryGetValue(key, out FieldInfo f))
            {
                for (Type cur = t; cur != null && f == null; cur = cur.BaseType) f = cur.GetField(name, All);
                fields[key] = f;
                if (f == null) Log.Warn("Field not found: " + key);
            }
            return f;
        }

        public static T Get<T>(object obj, string name)
        {
            if (obj == null) return default(T);
            FieldInfo f = Field(obj.GetType(), name);
            if (f == null) return default(T);
            object v = f.GetValue(obj);
            return v is T t ? t : default(T);
        }

        public static T GetStatic<T>(Type type, string name)
        {
            FieldInfo f = Field(type, name);
            if (f == null) return default(T);
            object v = f.GetValue(null);
            return v is T t ? t : default(T);
        }

        public static void Set(object obj, string name, object value)
        {
            if (obj == null) return;
            FieldInfo f = Field(obj.GetType(), name);
            if (f != null) f.SetValue(obj, value);
        }

        public static object Call(object obj, string name, params object[] args)
        {
            if (obj == null) return null;
            Type t = obj.GetType();
            string key = t.FullName + "." + name + "/" + args.Length;
            if (!methods.TryGetValue(key, out MethodInfo m))
            {
                for (Type cur = t; cur != null && m == null; cur = cur.BaseType)
                {
                    foreach (MethodInfo cand in cur.GetMethods(All))
                    {
                        if (cand.Name == name && cand.GetParameters().Length == args.Length)
                        {
                            m = cand;
                            break;
                        }
                    }
                }
                methods[key] = m;
                if (m == null) Log.Warn("Method not found: " + key);
            }
            return m == null ? null : m.Invoke(obj, args);
        }
    }
}
