
using System;
using UnityEditor;
using UnityEngine;

namespace LuaInterface.Editor
{
    public static class DebugTypes
    {
        private static void DebugType(Type type)
        {
            var included = ToLuaSettingsUtility.IsIncluded(type);
            var namespaceIncluded = ToLuaSettingsUtility.IsNamespaceIncluded(type.Namespace);
            var inTypeCsv = ToLuaSettingsUtility.InIncludedTypeCsv(type);
            var typeIncluded = ToLuaSettingsUtility.IsTypeIncluded(type);
            var isPublic = ToLuaTypes.IsPublic(type);
            var isUnsupport = ToLuaTypes.IsUnsupported(type);

            Debug.Log($"{type.FullName},{type.Namespace},{type.Name},{included},{namespaceIncluded},{inTypeCsv},{typeIncluded},{isUnsupport},{isPublic},{type.IsVisible},{type.IsNestedPublic},{type.IsGenericType},{type.IsAbstract}");
        }

        [MenuItem("Reflect/Debug types")]
        public static void Main()
        {
            DebugType(typeof(UnityEngine.GameObject));
        }
    }
}
