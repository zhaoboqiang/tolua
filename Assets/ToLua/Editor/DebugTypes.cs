
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
            var platformFlags = ReflectTypes.GetPlatformFlags(type);
            var namespaceIncluded = ReflectNamespaces.GetPlatformFlags(type.Namespace);
            var platformFlagsFromCsv = ReflectTypes.GetPlatformFlagsFromCsv(type);
            var platformFlagsFromRule = ReflectTypes.GetPlatformFlagsFromRule(type);
            var isPublic = ToLuaTypes.IsPublic(type);
            var isUnsupport = ToLuaTypes.IsUnsupported(type);

            Debug.Log($"{type.FullName},{type.Namespace},{type.Name},{platformFlags},{namespaceIncluded},{platformFlagsFromCsv},{platformFlagsFromRule},{isUnsupport},{isPublic},{type.IsVisible},{type.IsNestedPublic},{type.IsGenericType},{type.IsAbstract}");
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
