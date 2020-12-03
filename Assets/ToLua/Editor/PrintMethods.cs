
using System;
using System.Collections.Generic;
using UnityEditor;
using UnityEngine;

namespace LuaInterface.Editor
{
    public static class PrintMethods
    {
        public static void SaveCsv(List<Type> types, string fileName)
        {
            var lines = new List<string> { 
                "FullName,Namespace,Name,PlatformFlags,NamespaceIncluded,PlatformFlagsFromCsv,PlatformFlagsFromRule,IsUnsupport,IsPublic,IsVisible,IsNestedPublic,IsGenericType,IsAbstract"
            };
            foreach (var type in types)
            {
                var platformFlags = ReflectTypes.GetPlatformFlags(type);
                var namespaceIncluded = ReflectNamespaces.GetPlatformFlags(type.Namespace);
                var platformFlagsFromCsv = ReflectTypes.GetPlatformFlagsFromCsv(type, ToLuaPlatformFlags.All);
                var platformFlagsFromRule = ReflectTypes.GetPlatformFlagsFromRule(type, ToLuaPlatformFlags.All);
                var isPublic = ToLuaTypes.IsPublic(type);
                var isUnsupport = ToLuaTypes.IsUnsupported(type);

                lines.Add($"{type.FullName},{type.Namespace},{type.Name},{platformFlags},{namespaceIncluded},{platformFlagsFromCsv},{platformFlagsFromRule},{isUnsupport},{isPublic},{type.IsVisible},{type.IsNestedPublic},{type.IsGenericType},{type.IsAbstract}");
            }
            ReflectUtility.SaveCsv(lines, $"{Application.dataPath}/Editor/{fileName}.csv");
        }

        [MenuItem("Print/Methods")]
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

            SaveCsv(types, "all_methods");
        }
    }
}
