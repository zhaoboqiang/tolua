using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

namespace LuaInterface.Editor
{
    public static class ToLuaSettingsUtility
    {
        public static void Initialize(ToLuaSettings settings)
        {
            Settings = settings;
        }

        public static ToLuaSettings Settings { get; private set; }

        //附加导出委托类型(在导出委托时, BindTypes 中牵扯的委托类型都会导出， 无需写在这里)
        public static DelegateType[] customDelegateList => new[]
        {
            _DT(typeof(Action)),
            _DT(typeof(UnityEngine.Events.UnityAction)),
            _DT(typeof(Predicate<int>)),
            _DT(typeof(Action<int>)),
            _DT(typeof(Comparison<int>)),
            _DT(typeof(Func<int, int>)),
        };

        private static ToLuaMenu.BindType _GT(Type t)
        {
            return new ToLuaMenu.BindType(t);
        }

        private static DelegateType _DT(Type t)
        {
            return new DelegateType(t);
        }

        private static Dictionary<string, LuaIncludedAssembly> includedAssemblies;
        public static Dictionary<string, LuaIncludedAssembly> IncludedAssemblies
        {
            get
            {
                if (includedAssemblies == null)
                {
                    var assemblies = LuaSettingsUtility.LoadCsv<LuaIncludedAssembly>(Settings.IncludedAssemblyCsv);
                    if (assemblies == null)
                        includedAssemblies = new Dictionary<string, LuaIncludedAssembly>();
                    else
                        includedAssemblies = assemblies.ToDictionary(key => key.Name);
                }
                return includedAssemblies;
            }
        }

        private static Dictionary<string, LuaIncludedNamespace> includedNamespaces;
        public static Dictionary<string, LuaIncludedNamespace> IncludedNamespaces
        {
            get
            {
                if (includedNamespaces == null)
                {
                    var namespaces = LuaSettingsUtility.LoadCsv<LuaIncludedNamespace>(Settings.IncludedNamespaceCsv);
                    if (namespaces == null)
                        includedNamespaces = new Dictionary<string, LuaIncludedNamespace>();
                    else
                        includedNamespaces = namespaces.ToDictionary(key => key.Namespace);
                }
                return includedNamespaces;
            }
        }


        private static Dictionary<string, LuaIncludedType> includedTypes;
        public static Dictionary<string, LuaIncludedType> IncludedTypes
        {
            get
            {
                if (includedTypes == null)
                {
                    var types = LuaSettingsUtility.LoadCsv<LuaIncludedType>(Settings.IncludedTypeCsv);
                    if (types == null)
                        includedTypes = new Dictionary<string, LuaIncludedType>();
                    else
                        includedTypes = types.ToDictionary(key => key.FullName);
                }
                return includedTypes;
            }
        }

        private static Dictionary<string, LuaIncludedMethod> includedMethods;
        public static Dictionary<string, LuaIncludedMethod> IncludedMethods
        {
            get
            {
                if (includedMethods == null)
                {
                    var methods = LuaSettingsUtility.LoadCsv<LuaIncludedMethod>(Settings.IncludedMethodCsv);
                    if (methods == null)
                        includedMethods = new Dictionary<string, LuaIncludedMethod>();
                    else
                        includedMethods = methods.ToDictionary(key => key.MethodName);
                }
                return includedMethods;
            }
        }

        public static bool IsAssemblyIncluded(string assemblyName)
        {
            if (IncludedAssemblies.TryGetValue(assemblyName, out var value))
            {
#if UNITY_IOS
                if (value.iOS)
                    return true;
#elif UNITY_ANDROID
                if (value.Android)
                    return true;
#else
                if (value.iOS || value.Android)
                    return true;
#endif
                return false;
            }
            return true;
        }

        public static bool IsNamespaceIncluded(string ns)
        {
            if (string.IsNullOrEmpty(ns))
                return true;

            if (IncludedNamespaces.TryGetValue(ns, out var value))
            {
#if UNITY_IOS
                if (value.iOS)
                    return true;
#elif UNITY_ANDROID
                if (value.Android)
                    return true;
#else
                if (value.iOS || value.Android)
                    return true;
#endif
                return false;
            }
            return true;
        }

        private static bool IsTypeIncludedByType(Type type)
        {
            if (type.IsGenericType)
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

        private static bool IsTypeIncluded(Type type)
        {
            if (IncludedTypes.TryGetValue(type.FullName, out var value))
            {
#if UNITY_IOS
                if (value.iOS)
                    return true;
#elif UNITY_ANDROID
                if (value.Android)
                    return true;
#else
                if (value.iOS || value.Android)
                    return true;
#endif
                return false;
            }

            // default rule
            return IsTypeIncludedByType(type);
        }

        public static bool IsIncluded(Type type)
        {
            return (IsNamespaceIncluded(type.Namespace) || IncludedTypes.ContainsKey(type.FullName)) && IsTypeIncluded(type);
        }

        public static bool IsMethodIncluded(string methodName)
        {
            if (IncludedMethods.TryGetValue(methodName, out var value))
            {
#if UNITY_IOS
                if (value.iOS)
                    return true;
#elif UNITY_ANDROID
                if (value.Android)
                    return true;
#else
                if (value.iOS || value.Android)
                    return true;
#endif
                return false;
            }
            return true;
        }

        public static ToLuaMenu.BindType[] BindTypes
        {
            get
            {
                includedAssemblies = null;

                var bindTypes = new List<ToLuaMenu.BindType>();
                var assemblies = AppDomain.CurrentDomain.GetAssemblies();

                foreach (var assembly in assemblies)
                {
                    var assemblyName = assembly.GetName().Name;
                    // Debug.Log($"[Assembly] {assemblyName}");

                    if (!IsAssemblyIncluded(assemblyName))
                        continue;

                    foreach (var type in assembly.GetTypes())
                    {
                        if (!IsIncluded(type))
                            continue;

                        if (typeof(MulticastDelegate).IsAssignableFrom(type))
                            continue;

                        bindTypes.Add(_GT(type));
                    }
                }

                return bindTypes.ToArray();
            }
        }
    }
}