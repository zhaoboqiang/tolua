
using System;
using System.Reflection;
using System.Linq;
using UnityEditor;
using UnityEngine;

namespace LuaInterface.Editor
{
    public static class DebugTypes
    {
        private static void DebugType(Type type)
        {
            var included = ReflectTypes.IsIncluded(type);
            var namespaceIncluded = ReflectNamespaces.IsNamespaceIncluded(type.Namespace);
            var inTypeCsv = ReflectTypes.InIncludedTypeCsv(type);
            var typeIncluded = ReflectTypes.IsTypeIncluded(type);
            var isPublic = ToLuaTypes.IsPublic(type);
            var isUnsupport = ToLuaTypes.IsUnsupported(type);

            Debug.Log($"{type.FullName},{type.Namespace},{type.Name},{included},{namespaceIncluded},{inTypeCsv},{typeIncluded},{isUnsupport},{isPublic},{type.IsVisible},{type.IsNestedPublic},{type.IsGenericType},{type.IsAbstract}");
        }

        private static void DebugType(string assemblyName, string typeName)
        {
            var type = ReflectTypes.GetType(assemblyName, typeName);
            DebugType(type);
        }

        [MenuItem("Reflect/Debug types")]
        public static void Main()
        {
            DebugType("EasySave", "ES3Types.ES3Type_Color32");
        }
    }
}
