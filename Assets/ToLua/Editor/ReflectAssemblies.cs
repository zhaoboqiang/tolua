using System;
using System.Collections.Generic;
using UnityEditor;
using System.Linq;

namespace LuaInterface.Editor
{
    public static class ReflectAssemblies
    {
        private static void UpdateCsv(List<LuaIncludedAssembly> newAssemblies)
        {
            newAssemblies.Sort((lhs, rhs) => lhs.Name.CompareTo(rhs.Name));

            // Load previous configurations
            var oldAssemblies = ToLuaSettingsUtility.IncludedAssemblies;

            // merge previous configurations
            for (int index = 0, count = newAssemblies.Count; index < count; ++index)
            {
                var newAssembly = newAssemblies[index];

                if (oldAssemblies.TryGetValue(newAssembly.Name, out var oldAssembly))
                {
                    newAssemblies[index] = oldAssembly;
                }
            }

            // save configurations
            var lines = new List<string> { "Name,Android,iOS" };
            lines.AddRange(from assembly in newAssemblies
                           where assembly.Android && assembly.iOS
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