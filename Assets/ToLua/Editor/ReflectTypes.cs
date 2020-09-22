using System;
using System.Collections.Generic;
using UnityEditor;
using System.Linq;
using System.Reflection;

namespace LuaInterface.Editor
{
    public static class ReflectTypes
    {
        private static void UpdateCsv(List<LuaIncludedType> newTypes)
        {
            // Load previous configurations
            var oldTypes = ToLuaSettingsUtility.IncludedTypes;

            // merge previous configurations
            for (int index = 0, count = newTypes.Count; index < count; ++index)
            {
                var newType = newTypes[index];

                if (oldTypes.TryGetValue(newType.FullName, out var oldType))
                {
                    newTypes[index] = oldType;
                }
            }

            // merge not exist previous configurations
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

            // save configurations
            var lines = new List<string> { "FullName,Namespace,OuterTypeName,Name,Note,Android,iOS" };
            lines.AddRange(from type in resultTypes
                           where !type.Android || !type.iOS || oldTypes.ContainsKey(type.FullName)
                           select $"{type.FullName},{type.Namespace},{type.OuterTypeName},{type.Name},{type.Note},{type.Android},{type.iOS}");
            ReflectUtility.SaveCsv(lines, ToLuaSettingsUtility.Settings.IncludedTypeCsv);
        }

        private static void AddNewType(List<LuaIncludedType> newTypes, Type type, Type outerType)
        {
            if (!ToLuaSettingsUtility.IsIncluded(type))
                return;

            newTypes.Add(new LuaIncludedType
            {
                Namespace = type.Namespace,
                Name = type.Name,
                OuterTypeName = outerType != null ? type.Name : null,
                FullName = type.FullName,
                Android = true,
                iOS = true
            });

            foreach (var nestedType in type.GetNestedTypes())
            {
                AddNewType(newTypes, nestedType, type);
            }
        }

        [MenuItem("Reflect/Update types")]
        public static void UpdateTypes()
        {
            var assemblies = AppDomain.CurrentDomain.GetAssemblies();

            var newTypes = new List<LuaIncludedType>();

            foreach (var assembly in assemblies)
            {
                var assemblyName = assembly.GetName().Name;
                if (!ToLuaSettingsUtility.IsAssemblyIncluded(assemblyName))
                    continue;

                foreach (var type in assembly.GetTypes())
                {
                    AddNewType(newTypes, type, null);
                }
            }

            UpdateCsv(newTypes);
        }
    }
}
