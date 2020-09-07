using System;
using System.IO;
using System.Collections.Generic;
using System.Linq;
using UnityEditor;
using UnityEngine;

namespace LuaInterface.Editor
{
    public static class ReflectAssemblies
    {
        [MenuItem("Reflect/Print assemblies")]
        public static void PrintAssemblies()
        {
            var names = new List<string>();
            var assemblies = AppDomain.CurrentDomain.GetAssemblies();
            foreach (var assembly in assemblies)
            {
                names.Add($"\t\t\"{assembly.GetName().Name}\",");
            }

            names.Sort();

            var text = string.Join("\n", names);
            File.WriteAllText("d:/assemblies.txt", text);
        }

        private static string GetCsvFilepath(string filename)
        {
            return Application.dataPath + "/Editor/" + filename + ".csv";
        }

        private static void SaveCsv(List<string> lines, string filename)
        {
            var filepath = GetCsvFilepath(filename);
            File.WriteAllText(filepath, string.Join("\n", lines));
        }

        private static void UpdateAssembliesCsv(List<LuaIncludedAssembly> newAssemblies)
        {
            newAssemblies.Sort();

            // Load previous configurations
            var oldAssemblies = ToLuaSettingsUtility.IncludedAssemblies.ToDictionary(key => key.Name);

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
            foreach (var assembly in newAssemblies)
                lines.Add($"{assembly.Name},{assembly.Android},{assembly.iOS}");
            SaveCsv(lines, ToLuaSettingsUtility.Settings.IncludedAssemblyCsv);
        }

        [MenuItem("Reflect/Print all")]
        public static void PrintAll()
        {
            var assemblies = AppDomain.CurrentDomain.GetAssemblies();

            var newAssemblies = new List<LuaIncludedAssembly>();

            var interfaces = new List<string> { "AssemblyName,Interface" };
            var enums = new List<string> { "AssemblyName,EnumName" };
            var classes = new List<string> { "AssemblyName,ClassName" };
            var methods = new List<string> { "AssemblyName,ClassName,MethodName" };

            foreach (var assembly in assemblies)
            {
                var assemblyName = assembly.GetName().Name;

                newAssemblies.Add(new LuaIncludedAssembly { Name = assemblyName, Android = true, iOS = true });

                /*
                if (!ToLuaSettingsUtility.IsAssemblyIncluded(assemblyName))
                    continue;
                */

                foreach (var type in assembly.GetTypes())
                {
                    /*
                    if (ToLuaSettingsUtility.IsTypeExcluded(type))
                        continue;
                    */

                    var typeName = type.Name;
                    if (type.IsEnum)
                    {
                        enums.Add($"{assemblyName},{typeName}");
                    }
                    else if (type.IsInterface)
                    {
                        interfaces.Add($"{assemblyName},{typeName}");
                    }
                    else
                    {
                        classes.Add($"{assemblyName},{typeName}");
                        methods.Add($"{assemblyName},{typeName}");
                    }
                }
            }

            interfaces.Sort();
            enums.Sort();
            classes.Sort();
            methods.Sort();

            UpdateAssembliesCsv(newAssemblies);

            File.WriteAllText("d:/interface.csv", string.Join("\n", interfaces));
            File.WriteAllText("d:/enum.csv", string.Join("\n", enums));
            File.WriteAllText("d:/classes.csv", string.Join("\n", classes));
            File.WriteAllText("d:/method.csv", string.Join("\n", methods));
        }
    }
}