using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
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

        private static ToLuaMenu.BindType _GT(Type t)
        {
            return new ToLuaMenu.BindType(t);
        }

        public static Type[] Types
        {
            get
            {
                var types = new List<Type>();
                var assemblies = AppDomain.CurrentDomain.GetAssemblies();

                foreach (var assembly in assemblies)
                {
                    foreach (var type in assembly.GetTypes())
                    {
                        if (!ReflectTypes.IsIncluded(type))
                            continue;

                        if (typeof(MulticastDelegate).IsAssignableFrom(type))
                            continue;

                        types.Add(type);
                    }
                }

                return types.ToArray();
            }
        }

        public static Type[] DelegateTypes
        {
            get
            {
                var delegateTypes = new HashSet<Type>();
                var binding = BindingFlags.Public | BindingFlags.Static | BindingFlags.IgnoreCase | BindingFlags.Instance;

                foreach (var type in Types)
                {
                    var fields = type.GetFields(BindingFlags.GetField | BindingFlags.SetField | binding);
                    var props = type.GetProperties(BindingFlags.GetProperty | BindingFlags.SetProperty | binding);

                    var methods = type.IsInterface ? type.GetMethods() : type.GetMethods(BindingFlags.Instance | binding);

                    for (int j = 0; j < fields.Length; j++)
                    {
                        var field = fields[j];
                        var t = field.FieldType;

                        if (ToLuaExport.IsDelegateType(t))
                        {
                            delegateTypes.Add(t);
                        }
                    }

                    for (int j = 0; j < props.Length; j++)
                    {
                        var prop = props[j];
                        var t = prop.PropertyType;

                        if (ToLuaExport.IsDelegateType(t))
                        {
                            delegateTypes.Add(t);
                        }
                    }

                    for (int j = 0; j < methods.Length; j++)
                    {
                        var m = methods[j];
                        if (m.IsGenericMethod)
                            continue;

                        var pifs = m.GetParameters();
                        for (int k = 0; k < pifs.Length; k++)
                        {
                            var pif = pifs[k];

                            var t = pif.ParameterType;
                            if (t.IsByRef)
                                t = t.GetElementType();

                            if (ToLuaExport.IsDelegateType(t))
                            {
                                delegateTypes.Add(t);
                            }
                        }
                    }
                }

                return delegateTypes.ToArray();
            }
        }

    }
}