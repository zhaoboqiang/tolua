using System;
using System.Collections.Generic;
using System.Linq;
using UnityEditor;

namespace LuaInterface.Editor
{
    public static class ReflectNamespaces
    {
        private static Dictionary<string, LuaNamespaceSetting> namespaceSettings;
        public static Dictionary<string, LuaNamespaceSetting> NamespaceSettings
        {
            get
            {
                if (namespaceSettings == null)
                {
                    var namespaces = LuaSettingsUtility.LoadCsv<LuaNamespaceSetting>(ToLuaSettingsUtility.Settings.NamespaceCsv);
                    if (namespaces == null)
                        namespaceSettings = new Dictionary<string, LuaNamespaceSetting>();
                    else
                        namespaceSettings = namespaces.ToDictionary(key => key.Namespace);
                }
                return namespaceSettings;
            }
        }

        public static void Reset()
        {
            namespaceSettings = null;
        }

        public static ToLuaPlatformFlags GetPlatformFlags(string name)
        {
            var flags = ToLuaPlatformFlags.All;

            if (string.IsNullOrEmpty(name))
                return flags;

            if (NamespaceSettings.TryGetValue(name, out var value))
                flags = ToLuaPlatformUtility.From(value.Android, value.iOS, value.Editor);

            return flags;
        }

        private static void UpdateCsv(Dictionary<string, LuaNamespaceSetting> newNamespaces)
        {
            // Load previous configurations
            var oldNamespaces = NamespaceSettings;

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
            var lines = new List<string> { "Namespace,Android,iOS,Editor" };
            var values = newNamespaces.Values.ToList();
            values.Sort((lhs, rhs) => lhs.Namespace.CompareTo(rhs.Namespace));
            lines.AddRange(from ns in values
                           // where !ns.Android || !ns.iOS || !ns.Editor
                           select $"{ns.Namespace},{ns.Android},{ns.iOS},{ns.Editor}");
            ReflectUtility.SaveCsv(lines, ToLuaSettingsUtility.Settings.NamespaceCsv);
        }

        [MenuItem("Reflect/Update namespaces")]
        public static void UpdateNamespaces()
        {
            var assemblies = AppDomain.CurrentDomain.GetAssemblies();

            var newNamespaces = new Dictionary<string, LuaNamespaceSetting>();

            foreach (var assembly in assemblies)
            {
                var assemblyName = assembly.GetName().Name;
                foreach (var type in assembly.GetTypes())
                {
                    var ns = type.Namespace;
                    if (string.IsNullOrEmpty(ns))
                        continue;

                    if (newNamespaces.ContainsKey(ns))
                        continue;

                    newNamespaces.Add(ns, new LuaNamespaceSetting { Namespace = ns, Android = true, iOS = true, Editor = true });
                }
            }

            UpdateCsv(newNamespaces);
        }
    }
}