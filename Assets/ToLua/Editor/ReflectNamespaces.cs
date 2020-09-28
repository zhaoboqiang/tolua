using System;
using System.Collections.Generic;
using System.Linq;
using UnityEditor;

namespace LuaInterface.Editor
{
    public static class ReflectNamespaces
    {
        private static Dictionary<string, LuaIncludedNamespace> includedNamespaces;
        public static Dictionary<string, LuaIncludedNamespace> IncludedNamespaces
        {
            get
            {
                if (includedNamespaces == null)
                {
                    var namespaces = LuaSettingsUtility.LoadCsv<LuaIncludedNamespace>(ToLuaSettingsUtility.Settings.IncludedNamespaceCsv);
                    if (namespaces == null)
                        includedNamespaces = new Dictionary<string, LuaIncludedNamespace>();
                    else
                        includedNamespaces = namespaces.ToDictionary(key => key.Namespace);
                }
                return includedNamespaces;
            }
        }

        public static bool IsNamespaceIncluded(string ns)
        {
            if (string.IsNullOrEmpty(ns))
                return true;

            if (IncludedNamespaces.TryGetValue(ns, out var value))
            {
#if UNITY_IOS
                if (value.iOS)
                    return true;
#elif UNITY_ANDROID
                if (value.Android)
                    return true;
#else
                if (value.iOS || value.Android)
                    return true;
#endif
                return false;
            }
            return true;
        }

        private static void UpdateCsv(Dictionary<string, LuaIncludedNamespace> newNamespaces)
        {
            // Load previous configurations
            var oldNamespaces = IncludedNamespaces;

            // merge previous configurations
            var keys = newNamespaces.Keys.ToArray<string>();
            for (int index = 0, count = keys.Length; index < count; ++index)
            {
                var key = keys[index];

                if (oldNamespaces.TryGetValue(key, out var oldNamespace))
                {
                    newNamespaces[key] = oldNamespace;
                }
            }

            // merge not exist previous configurations
            foreach (var kv in oldNamespaces)
            {
                if (newNamespaces.ContainsKey(kv.Key))
                    continue;

                newNamespaces.Add(kv.Key, kv.Value);
            }

            // save configurations
            var lines = new List<string> { "Namespace,Android,iOS" };
            var values = newNamespaces.Values.ToList();
            values.Sort((lhs, rhs) => lhs.Namespace.CompareTo(rhs.Namespace));
            lines.AddRange(from ns in values
                           //where !ns.Android || !ns.iOS
                           select $"{ns.Namespace},{ns.Android},{ns.iOS}");
            ReflectUtility.SaveCsv(lines, ToLuaSettingsUtility.Settings.IncludedNamespaceCsv);
        }


        [MenuItem("Reflect/Update namespaces")]
        public static void UpdateNamespaces()
        {
            var assemblies = AppDomain.CurrentDomain.GetAssemblies();

            var newNamespaces = new Dictionary<string, LuaIncludedNamespace>();

            foreach (var assembly in assemblies)
            {
                var assemblyName = assembly.GetName().Name;
                if (!ReflectAssemblies.IsAssemblyIncluded(assemblyName))
                    continue;

                foreach (var type in assembly.GetTypes())
                {
                    var ns = type.Namespace;
                    if (string.IsNullOrEmpty(ns))
                        continue;

                    if (newNamespaces.ContainsKey(ns))
                        continue;

                    newNamespaces.Add(ns, new LuaIncludedNamespace { Namespace = ns, Android = true, iOS = true });
                }
            }

            UpdateCsv(newNamespaces);
        }
    }
}