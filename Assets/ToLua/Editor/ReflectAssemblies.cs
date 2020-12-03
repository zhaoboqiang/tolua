using System;
using System.Collections.Generic;
using UnityEditor;
using System.Linq;

namespace LuaInterface.Editor
{
    public static class ReflectAssemblies
    {
        private static Dictionary<string, LuaIncludedAssembly> includedAssemblies;
        public static Dictionary<string, LuaIncludedAssembly> IncludedAssemblies
        {
            get
            {
                if (includedAssemblies == null)
                {
                    var assemblies = LuaSettingsUtility.LoadCsv<LuaIncludedAssembly>(ToLuaSettingsUtility.Settings.AssemblyCsv);
                    if (assemblies == null)
                        includedAssemblies = new Dictionary<string, LuaIncludedAssembly>();
                    else
                        includedAssemblies = assemblies.ToDictionary(key => key.Name);
                }
                return includedAssemblies;
            }
        }

        public static void Reset()
        {
            includedAssemblies = null;
        }

        public static ToLuaPlatformFlags GetPlatformFlags(string name)
        {
            var flags = ToLuaPlatformFlags.None; // allow list

            if (IncludedAssemblies.TryGetValue(name, out var value))
                flags = ToLuaPlatformUtility.From(value.Android, value.iOS, value.Editor);

            return flags;
        }

        private static void UpdateCsv(List<LuaIncludedAssembly> newAssemblies)
        {
            // Load previous configurations
            var oldAssemblies = IncludedAssemblies;

            // merge previous configurations
            for (int index = 0, count = newAssemblies.Count; index < count; ++index)
            {
                var newAssembly = newAssemblies[index];

                if (oldAssemblies.TryGetValue(newAssembly.Name, out var oldAssembly))
                {
                    newAssemblies[index] = oldAssembly;
                }
            }

            // merge not exist previous configurations
            var mergedAssemblies = newAssemblies.ToDictionary(key => key.Name);
            foreach (var kv in oldAssemblies)
            {
                if (mergedAssemblies.ContainsKey(kv.Key))
                    continue;

                mergedAssemblies.Add(kv.Key, kv.Value);
            }

            // Sort
            var resultAssemblies = mergedAssemblies.Values.ToList();
            resultAssemblies.Sort((lhs, rhs) => lhs.Name.CompareTo(rhs.Name));

            // save configurations
            var lines = new List<string> { "Name,Android,iOS,Editor" };
            lines.AddRange(from assembly in resultAssemblies
                           select $"{assembly.Name},{assembly.Android},{assembly.iOS},{assembly.Editor}");
            ReflectUtility.SaveCsv(lines, ToLuaSettingsUtility.Settings.AssemblyCsv);
        }

        [MenuItem("Reflect/Update assemblies")]
        public static void UpdateAssemblies()
        {
            var assemblies = AppDomain.CurrentDomain.GetAssemblies();

            var newAssemblies = new List<LuaIncludedAssembly>();

            foreach (var assembly in assemblies)
            {
                var assemblyName = assembly.GetName().Name;

                newAssemblies.Add(new LuaIncludedAssembly { Name = assemblyName, Android = true, iOS = true, Editor = true });
            }

            UpdateCsv(newAssemblies);
        }
    }
}