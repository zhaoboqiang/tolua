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

        //导出时强制做为静态类的类型(注意customTypeList 还要添加这个类型才能导出)
        //unity 有些类作为sealed class, 其实完全等价于静态类
        public static Type[] staticClassTypes => new[]
        {
            typeof(Application),
            typeof(Time),
            typeof(Screen),
            typeof(SleepTimeout),
            typeof(Input),
            typeof(Resources),
            typeof(Physics),
            typeof(RenderSettings),
            typeof(QualitySettings),
            typeof(GL),
            typeof(Graphics),
        };

        //附加导出委托类型(在导出委托时, customTypeList 中牵扯的委托类型都会导出， 无需写在这里)
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

        private static bool IsTypeIncluded(Type type)
        {
            var typeName = type.FullName;

            if (IncludedTypes.TryGetValue(typeName, out var value))
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

        public static ToLuaMenu.BindType[] customTypeList
        {
            get
            {
                includedAssemblies = null;

                var bindTypes = new List<ToLuaMenu.BindType>();
                var assemblies = AppDomain.CurrentDomain.GetAssemblies();

                var typeIndex = 0;

                foreach (var assembly in assemblies)
                {
                    var assemblyName = assembly.GetName().Name;
                    Debug.Log($"[Assembly] {assemblyName}");

                    if (!IsAssemblyIncluded(assemblyName))
                        continue;

                    foreach (var type in assembly.GetTypes())
                    {
                        var typeName = type.Name;

                        if (!IsTypeIncluded(type))
                            continue;

                        Debug.Log($"\t[{typeIndex++} {typeName}");

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