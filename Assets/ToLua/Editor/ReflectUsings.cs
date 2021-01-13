using System;
using System.Collections.Generic;
using System.Linq;
using UnityEditor;
using UnityEngine;

namespace LuaInterface.Editor
{
    public static class PrintUsings
    {
        private static Dictionary<string, LuaUsingSetting> usingSettings;
        public static Dictionary<string, LuaUsingSetting> UsingSettings
        {
            get
            {
                if (usingSettings == null)
                {
                    var types = LuaSettingsUtility.LoadCsv<LuaUsingSetting>(ToLuaSettingsUtility.Settings.UsingCsv);
                    if (types == null)
                        usingSettings = new Dictionary<string, LuaUsingSetting>();
                    else
                        usingSettings = types.ToDictionary(key => key.FullName);
                }
                return usingSettings;
            }
        }

        private static void UpdateCsv(List<LuaUsingSetting> newTypes)
        {
            // Load previous configurations
            var oldTypes = UsingSettings;

            // Merge previous configurations
            for (int index = 0, count = newTypes.Count; index < count; ++index)
            {
                var newType = newTypes[index];

                if (oldTypes.TryGetValue(newType.FullName, out var oldType))
                {
                    newTypes[index] = oldType;
                }
            }

            // Merge not exist previous configurations
            var mergedTypes = newTypes.ToDictionary(key => key.FullName);
            foreach (var kv in oldTypes)
            {
                if (mergedTypes.ContainsKey(kv.Key))
                    continue;

                mergedTypes.Add(kv.Key, kv.Value);
            }

            // Sort
            var resultTypes = mergedTypes.Values.ToList();
            resultTypes.Sort((lhs, rhs) => lhs.FullName.CompareTo(rhs.FullName));

            // Save configurations
            var lines = new List<string> { "FullName,Preload" };
            lines.AddRange(from type in resultTypes
                           select $"{type.FullName},{type.Preload}");
            ReflectUtility.SaveCsv(lines, ToLuaSettingsUtility.Settings.UsingCsv);
        }
        
        private static void AddNewType(List<LuaUsingSetting> newTypes, Type type)
        {
            newTypes.Add(new LuaUsingSetting
            {
                FullName = ToLuaTypes.GetFullName(type),
                Preload = false
            });
        }

        [MenuItem("Reflect/Update usings")]
        public static void Print()
        {
            var newTypes = new List<LuaUsingSetting>();

            var types = ToLuaSettingsUtility.Types;
            foreach (var type in types)
                AddNewType(newTypes, type);

            UpdateCsv(newTypes);
        }
    }
}
