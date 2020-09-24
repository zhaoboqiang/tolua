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
            var lines = new List<string> { "FullName,Namespace,Name,Included,NamespaceIncluded,InTypeCsv,TypeIncluded,Unsupported,Public,Visible,NestedPublic,Generic,Abstract" };
            foreach (var type in types)
            {
                var included = ToLuaSettingsUtility.IsIncluded(type);
                var namespaceIncluded = ToLuaSettingsUtility.IsNamespaceIncluded(type.Namespace);
                var inTypeCsv = ToLuaSettingsUtility.InIncludedTypeCsv(type);
                var typeIncluded = ToLuaSettingsUtility.IsTypeIncluded(type);
                var isPublic = ToLuaTypes.IsPublic(type);
                var isUnsupport = ToLuaTypes.IsUnsupported(type);

                lines.Add($"{type.FullName},{type.Namespace},{type.Name},{included},{namespaceIncluded},{inTypeCsv},{typeIncluded},{isUnsupport},{isPublic},{type.IsVisible},{type.IsNestedPublic},{type.IsGenericType},{type.IsAbstract}");
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
