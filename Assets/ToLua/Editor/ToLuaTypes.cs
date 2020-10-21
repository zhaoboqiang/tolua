using System;
using System.Collections.Generic;
using System.Reflection;

namespace LuaInterface.Editor
{
    public static class ToLuaTypes
    {
        public static string GetName(Type type)
        {
            return type.FullName.Replace("+", ".");
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
            while (type.IsNested)
            {
                var outerType = type.ReflectedType;

                if (!ReflectTypes.IsIncluded(outerType))
                    return true;
                
                type = outerType;
            }

            return false;
        }

        public static bool IsPublic(Type type)
        {
            if (type.IsNested)
            {
                if (type.IsNestedPublic)
                {
                    return true;
                }

                if (type.IsNestedAssembly)
                {
                    return true;
                }
            }
            else
            {
                if (type.IsPublic)
                {
                    return true;
                }
            }

            return false;
        }
    }
}
