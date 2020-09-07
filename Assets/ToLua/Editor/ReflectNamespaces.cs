using System;
using System.Collections.Generic;
using UnityEditor;

namespace LuaInterface.Editor
{
    public static class ReflectNamespaces
    {
        private static void UpdateCsv(List<LuaIncludedNamespace> newNamespaces)
        {
            newNamespaces.Sort((lhs, rhs) => lhs.Name.CompareTo(rhs.Name));

            // Load previous configurations
            var oldNamespaces = ToLuaSettingsUtility.IncludedNamespaces;

            // merge previous configurations
            for (int index = 0, count = newNamespaces.Count; index < count; ++index)
            {
                var newNamespace = newNamespaces[index];

                if (oldNamespaces.TryGetValue(newNamespace.Name, out var oldNamespace))
                {
                    newNamespaces[index] = oldNamespace;
                }
            }

            // save configurations
            var lines = new List<string> { "Name,Android,iOS" };
            foreach (var ns in newNamespaces)
                lines.Add($"{ns.Name},{ns.Android},{ns.iOS}");
            ReflectUtility.SaveCsv(lines, ToLuaSettingsUtility.Settings.IncludedNamespaceCsv);
        }


        [MenuItem("Reflect/Update namespaces")]
        public static void UpdateNamespaces()
        {
            var assemblies = AppDomain.CurrentDomain.GetAssemblies();

            var newNamespaces = new List<LuaIncludedNamespace>();

            foreach (var assembly in assemblies)
            {
                var assemblyName = assembly.GetName().Name;

                newNamespaces.Add(new LuaIncludedNamespace { Name = assemblyName, Android = true, iOS = true });
            }

            UpdateCsv(newNamespaces);
        }
    }
}