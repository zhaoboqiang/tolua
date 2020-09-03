using System;
using System.IO;
using System.Collections.Generic;
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
                names.Add($"\t\"{assembly.GetName().Name}\"");
            }

            names.Sort();

            var text = string.Join("\n", names);
            File.WriteAllText("d:/assemblies.txt", text);
        }

        [MenuItem("Reflect/Print types")]
        public static void PrintTypes()
        {
            var assemblies = AppDomain.CurrentDomain.GetAssemblies();

            var lines = new List<string>();

            foreach (var assembly in assemblies)
            {
                lines.Add(assembly.GetName().Name);

                foreach (var type in assembly.GetTypes())
                {
                    var typeName = type.Name;
                    if (typeName.Contains("`"))
                        Debug.Log($"{typeName} Contains`");
                    else if (typeName.Contains("<") && typeName.Contains(">"))
                        Debug.Log($"{type.Name} Contains<>");

                    lines.Add($"\t\"{type.Name}\", {type.IsGenericType}, {type.IsAbstract}, {type.IsVisible}, {type.IsPublic}");
                }
            }

            var text = string.Join("\n", lines);
            File.WriteAllText("d:/types.txt", text);
        }
    }
}