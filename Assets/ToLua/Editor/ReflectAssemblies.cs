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
                    var assemblies = LuaSettingsUtility.LoadCsv<LuaIncludedAssembly>(ToLuaSettingsUtility.Settings.IncludedAssemblyCsv);
                    if (assemblies == null)
                        includedAssemblies = new Dictionary<string, LuaIncludedAssembly>();
                    else
                        includedAssemblies = assemblies.ToDictionary(key => key.Name);
                }
                return includedAssemblies;
            }
        }

        public static bool IsAssemblyIncluded(string assemblyName)
        {
            if (IncludedAssemblies.TryGetValue(assemblyName, out var value))
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
            var lines = new List<string> { "Name,Android,iOS" };
            lines.AddRange(from assembly in resultAssemblies
                           //where !assembly.Android || !assembly.iOS
                           select $"{assembly.Name},{assembly.Android},{assembly.iOS}");
            ReflectUtility.SaveCsv(lines, ToLuaSettingsUtility.Settings.IncludedAssemblyCsv);
        }


        [MenuItem("Reflect/Update assemblies")]
        public static void UpdateAssemblies()
        {
            var assemblies = AppDomain.CurrentDomain.GetAssemblies();

            var newAssemblies = new List<LuaIncludedAssembly>();

            foreach (var assembly in assemblies)
            {
                var assemblyName = assembly.GetName().Name;

                newAssemblies.Add(new LuaIncludedAssembly { Name = assemblyName, Android = true, iOS = true });
            }

            UpdateCsv(newAssemblies);
        }
    }
}