using System;
using System.Collections.Generic;
using UnityEditor;
using System.Linq;
using System.Reflection;

namespace LuaInterface.Editor
{
    public static class ReflectTypes
    {
        private static Dictionary<string, LuaIncludedType> includedTypes;
        public static Dictionary<string, LuaIncludedType> IncludedTypes
        {
            get
            {
                if (includedTypes == null)
                {
                    var types = LuaSettingsUtility.LoadCsv<LuaIncludedType>(ToLuaSettingsUtility.Settings.IncludedTypeCsv);
                    if (types == null)
                        includedTypes = new Dictionary<string, LuaIncludedType>();
                    else
                        includedTypes = types.ToDictionary(key => key.FullName);
                }
                return includedTypes;
            }
        }

        private static bool IsTypeIncluded(Type type)
        {
            if (type.IsGenericTypeDefinition)
                return false;

            if (!type.IsVisible)
                return false;

            if (!ToLuaTypes.IsPublic(type))
                return false;

            if (type.IsInterface)
                return false;

            if (ToLuaTypes.IsUnsupported(type))
                return false;

            return true;
        }

        public static ToLuaPlatformFlags GetPlatformFlagsFromCsv(Type type, ToLuaPlatformFlags flags)
        {
            if (IncludedTypes.TryGetValue(type.FullName.Replace("+", "."), out var value))
                flags = ToLuaPlatformUtility.From(value.Android, value.iOS, value.Android || value.iOS);

            return flags;
        }

        public static ToLuaPlatformFlags GetPlatformFlagsFromRule(Type type, ToLuaPlatformFlags flags)
        {
            return IsTypeIncluded(type) ? flags : ToLuaPlatformFlags.None;
        }

        public static bool IsIncluded(Type type)
        {
            return (GetPlatformFlags(type) & ToLuaPlatformFlags.All) != ToLuaPlatformFlags.None;
        }

        public static ToLuaPlatformFlags GetPlatformFlags(Type type)
        {
            var assemblyName = type.Assembly.GetName().Name;

            var assemblyFlags = ReflectAssemblies.GetPlatformFlags(assemblyName);

            var namespaceFlags = ReflectNamespaces.GetPlatformFlags(type.Namespace);

            for (var outerType = type; outerType.IsNested; outerType = outerType.ReflectedType)
                namespaceFlags &= ReflectNamespaces.GetPlatformFlags(outerType.Namespace);

            var flags = GetPlatformFlagsFromCsv(type, assemblyFlags & namespaceFlags);
            return GetPlatformFlagsFromRule(type, flags);
        }

        public static Type GetType(string assemblyName, string typeName)
        {
            var selectedAssembly = AppDomain.CurrentDomain.GetAssemblies().SingleOrDefault(assembly => assembly.GetName().Name == assemblyName);
            return selectedAssembly.GetType(typeName);
        }

        private static void UpdateCsv(List<LuaIncludedType> newTypes)
        {
            // Load previous configurations
            var oldTypes = IncludedTypes;

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
            var lines = new List<string> { "FullName,Note,Android,iOS" };
            lines.AddRange(from type in resultTypes
                           where !type.Android || !type.iOS || oldTypes.ContainsKey(type.FullName)
                           select $"{type.FullName},{type.Note},{type.Android},{type.iOS}");
            ReflectUtility.SaveCsv(lines, ToLuaSettingsUtility.Settings.IncludedTypeCsv);
        }

        private static void AddNewType(List<LuaIncludedType> newTypes, Type type, Type outerType)
        {
            newTypes.Add(new LuaIncludedType
            {
                FullName = type.FullName,
                Android = true,
                iOS = true
            });
        }

        [MenuItem("Reflect/Update types")]
        public static void UpdateTypes()
        {
            var assemblies = AppDomain.CurrentDomain.GetAssemblies();

            var newTypes = new List<LuaIncludedType>();

            foreach (var assembly in assemblies)
            {
                foreach (var type in assembly.GetTypes())
                {
                    AddNewType(newTypes, type, null);
                }
            }

            UpdateCsv(newTypes);
        }
    }
}
