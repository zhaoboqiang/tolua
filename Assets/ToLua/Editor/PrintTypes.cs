using System;
using System.Collections.Generic;
using UnityEditor;
using UnityEngine;

namespace LuaInterface.Editor
{
    public static class PrintTypes
    {
        public static void SaveCsv(List<Type> types, string fileName)
        {
            var lines = new List<string> { "FullName,Namespace,Name,Android,iOS,Included,NamespaceIncluded,InTypeCsv,TypeIncluded,Unsupported,Public,Visible,NestedPublic,Generic,Abstract" };
            foreach (var type in types)
            {
                var included = ReflectTypes.IsIncluded(type);
                var namespaceIncluded = ReflectNamespaces.IsNamespaceIncluded(type.Namespace);
                var inTypeCsv = ReflectTypes.InIncludedTypeCsv(type);
                var typeIncluded = ReflectTypes.IsTypeIncluded(type);
                var isPublic = ToLuaTypes.IsPublic(type);
                var isUnsupport = ToLuaTypes.IsUnsupported(type);
                var android = ToLuaTypes.AndroidSupported(type);
                var iOS = ToLuaTypes.iOSSupported(type);

                lines.Add($"{type.FullName},{type.Namespace},{type.Name},{android},{iOS},{included},{namespaceIncluded},{inTypeCsv},{typeIncluded},{isUnsupport},{isPublic},{type.IsVisible},{type.IsNestedPublic},{type.IsGenericType},{type.IsAbstract}");
            }
            ReflectUtility.SaveCsv(lines, $"{Application.dataPath}/Editor/{fileName}.csv");
        }

        [MenuItem("Print/Types")]
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

            SaveCsv(types, "all_types");
        }
    }
}
