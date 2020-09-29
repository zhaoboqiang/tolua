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

        public static ToLuaMenu.BindType[] BindTypes
        {
            get
            {
                var bindTypes = new List<ToLuaMenu.BindType>();
                var assemblies = AppDomain.CurrentDomain.GetAssemblies();

                foreach (var assembly in assemblies)
                {
                    foreach (var type in assembly.GetTypes())
                    {
                        if (!ReflectTypes.IsIncluded(type))
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