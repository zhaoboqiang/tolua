using System;
using System.Collections.Generic;
using UnityEditor;

namespace LuaInterface.Editor
{
    public static class ReflectTypes
    {
        private static bool IsTypeIncluded(Type type)
        {
            if (type.IsGenericType)
                return false;

            if (!type.IsVisible)
                return false;

            if (!type.IsPublic)
                return false;

            if (ToLuaMenu.BindType.IsObsolete(type))
                return false;

            return true;
        }

        private static void UpdateCsv(List<LuaIncludedType> newTypes)
        {
            newTypes.Sort((lhs, rhs) => lhs.FullName.CompareTo(rhs.FullName));

            // Load previous configurations
            var oldTypes = ToLuaSettingsUtility.IncludedTypes;

            // merge previous configurations
            for (int index = 0, count = newTypes.Count; index < count; ++index)
            {
                var newType = newTypes[index];

                if (oldTypes.TryGetValue(newType.Name, out var oldType))
                {
                    newTypes[index] = oldType;
                }
            }

            // save configurations
            var lines = new List<string> { "FullName,Namespace,Name,Android,iOS" };
            foreach (var type in newTypes)
                lines.Add($"{type.FullName},{type.Namespace},{type.Name},{type.Android},{type.iOS}");
            ReflectUtility.SaveCsv(lines, ToLuaSettingsUtility.Settings.IncludedTypeCsv);
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
                    var included = IsTypeIncluded(type);

                    newTypes.Add(new LuaIncludedType
                    {
                        Namespace = type.Namespace,
                        Name = type.Name,
                        FullName = type.FullName,
                        Android = included,
                        iOS = included
                    });
                }
            }

            UpdateCsv(newTypes);
        }
    }
}
