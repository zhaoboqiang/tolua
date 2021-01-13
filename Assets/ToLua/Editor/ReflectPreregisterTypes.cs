
using System;
using System.Collections.Generic;
using UnityEditor;
using UnityEngine;

namespace LuaInterface.Editor
{
    public static class PrintPreregisterTypes
    {
        public static void SaveCsv(Type[] types, string fileName)
        {
            var lines = new List<string> { "FullName" };
            foreach (var type in types)
            {
                var isUnsupport = ReflectTypes.IsIncluded(type);

                lines.Add($"{ToLuaTypes.GetFullName(type)}");
            }
            ReflectUtility.SaveCsv(lines, $"{Application.dataPath}/EditorSetting/{fileName}.csv");
        }

        [MenuItem("Print/PreregisterTypes")]
        public static void Print()
        {
            var types = ToLuaSettingsUtility.Types;
            SaveCsv(types, "PreregisterTypes");
        }
    }
}
