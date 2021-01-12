
using System;
using System.Collections.Generic;
using UnityEditor;
using UnityEngine;

namespace LuaInterface.Editor
{
    public static class PrintPreregisterTypes
    {
        public static void SaveCsv(List<Type> types, string fileName)
        {
            var lines = new List<string> { "FullName" };
            foreach (var type in types)
            {
                var isUnsupport = ReflectTypes.IsIncluded(type);

                lines.Add($"{type.FullName}");
            }
            ReflectUtility.SaveCsv(lines, $"{Application.dataPath}/EditorSetting/{fileName}.csv");
        }

        [MenuItem("Print/PreregisterTypes")]
        public static void Print()
        {
            var assemblies = AppDomain.CurrentDomain.GetAssemblies();
            var types = new List<Type>();

            foreach (var assembly in assemblies)
            {
                foreach (var type in assembly.GetTypes())
                {
                    types.Add(type);
                }
            }

            SaveCsv(types, "PreregisterTypes");
        }
    }
}
