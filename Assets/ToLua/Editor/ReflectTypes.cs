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

        private static Dictionary<Type, Type> BuildOuterTypeMap(Assembly[] assemblies)
        {
            var maps = new Dictionary<Type, Type>();

            foreach (var assembly in assemblies)
            {
                foreach (var type in assembly.GetTypes())
                {
                    foreach (var nestedType in type.GetNestedTypes())
                    {
                        maps.Add(nestedType, type);
                    }
                }
            }

            return maps;
        }


        private static string GetOuterTypeName(Dictionary<Type, Type> maps, Type type)
        {
            if (maps.TryGetValue(type, out var outerType))
                return outerType.Name;
            return null;
        }

        [MenuItem("Reflect/Update types")]
        public static void UpdateTypes()
        {
            var assemblies = AppDomain.CurrentDomain.GetAssemblies();

            var outerTypes = BuildOuterTypeMap(assemblies);

            var newTypes = new List<LuaIncludedType>();

            foreach (var assembly in assemblies)
            {
                var assemblyName = assembly.GetName().Name;
                if (!ToLuaSettingsUtility.IsAssemblyIncluded(assemblyName))
                    continue;

                foreach (var type in assembly.GetTypes())
                {
                    if (!ToLuaSettingsUtility.IsIncluded(type))
                        continue;

                    newTypes.Add(new LuaIncludedType
                    {
                        Namespace = type.Namespace,
                        Name = type.Name,
                        OuterTypeName = GetOuterTypeName(outerTypes, type),
                        FullName = type.FullName,
                        Android = true,
                        iOS = true
                    });
                }
            }

            UpdateCsv(newTypes);
        }
    }
}
