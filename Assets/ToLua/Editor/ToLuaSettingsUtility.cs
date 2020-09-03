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

        public static ToLuaMenu.BindType[] customTypeList
        {
            get
            {
                var excludedAssemblies = Settings.ExcludedAssemblies;
                var bindTypes = new List<ToLuaMenu.BindType>();
                var assemblies = AppDomain.CurrentDomain.GetAssemblies();

                var typeIndex = 0;

                foreach (var assembly in assemblies)
                {
                    var assemblyName = assembly.GetName().Name;
                    Debug.Log($"[Assembly] {assemblyName}");

                    if (excludedAssemblies.Contains(assemblyName))
                        continue;

                    foreach (var type in assembly.GetTypes())
                    {
                        if (type.IsGenericType)
                            continue;

                        var typeName = type.Name;
                        /*
                        if (typeName.Contains("`"))
                            continue;
                        */

                        if (!type.IsVisible)
                            continue;

                        if (!type.IsPublic)
                            continue;

                        if (ToLuaMenu.BindType.IsObsolete(type))
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