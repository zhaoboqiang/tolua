using System;
using System.Collections.Generic;
using System.Reflection;

namespace LuaInterface.Editor
{
    public static class ToLuaTypes
    {
        private static Dictionary<Type, Type> _outerTypes;
        private static Dictionary<Type, Type> OuterTypes
        {
            get
            {
                if (_outerTypes == null)
                {
                    _outerTypes = new Dictionary<Type, Type>();

                    var assemblies = AppDomain.CurrentDomain.GetAssemblies();
                    var types = new List<Type>();

                    foreach (var assembly in assemblies)
                    {
                        foreach (var type in assembly.GetTypes())
                        {
                            foreach (var nestedType in type.GetNestedTypes())
                            {
                                _outerTypes.Add(nestedType, type);
                            }
                        }
                    }
                }

                return _outerTypes;
            }
        }

        public static Type GetOuterType(Type type)
        {
            return OuterTypes[type];
        }

        public static bool IsUnsupported(MemberInfo mi)
        {
            foreach (var attribute in mi.GetCustomAttributes(true))
            {
                var t = attribute.GetType();
                if (t == typeof(System.ObsoleteAttribute) ||
                    t == typeof(NoToLuaAttribute) ||
                    t.Name == "MonoNotSupportedAttribute" ||
                    t.Name == "MonoTODOAttribute" ||
                    t.Name == "RequiredByNativeCodeAttribute" ||
                    t.Name == "UnsafeValueTypeAttribute" ||
                    t.Name == "CompilerGeneratedAttribute")
                {
                    return true;
                }
            }

            return false;
        }

        public static bool IsUnsupported(Type type)
        {
            if (IsUnsupported(type as MemberInfo))
                return true;

            // check outer class
            if (type.IsNested)
            {
                var outerType = GetOuterType(type);
                if (IsUnsupported(outerType))
                {
                    return true;
                }
            }
            return false;
        }
    }

}
