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

        public static string GetNamespace(Type t)
        {
            if (t.IsGenericType)
            {
                return GetGenericNamespace(t);
            }
            else
            {
                var space = t.FullName;

                if (space.Contains("+"))
                {
                    space = space.Replace('+', '.');
                    int index = space.LastIndexOf('.');
                    return space.Substring(0, index);
                }
                else
                {
                    return t.Namespace;
                }
            }
        }

        public static string GetTypeName(Type t)
        {
            if (t.IsGenericType)
            {
                return GetGenericTypeName(t);
            }
            else
            {
                var space = t.FullName;

                if (space.Contains("+"))
                {
                    space = space.Replace('+', '.');
                    int index = space.LastIndexOf('.');
                    return space.Substring(index + 1);
                }
                else
                {
                    return t.Namespace == null ? space : space.Substring(t.Namespace.Length + 1);
                }
            }
        }

        static string GetGenericNamespace(Type t)
        {
            var gArgs = t.GetGenericArguments();
            string typeName = t.FullName;
            int count = gArgs.Length;
            int pos = typeName.IndexOf("[");
            if (pos > 0)
                typeName = typeName.Substring(0, pos);

            string str = null;
            string name = null;
            int offset = 0;
            pos = typeName.IndexOf("+");

            while (pos > 0)
            {
                str = typeName.Substring(0, pos);
                typeName = typeName.Substring(pos + 1);
                pos = str.IndexOf('`');

                if (pos > 0)
                {
                    count = (int)(str[pos + 1] - '0');
                    str = str.Substring(0, pos);
                    str += "<" + string.Join(",", LuaMisc.GetGenericName(gArgs, offset, count)) + ">";
                    offset += count;
                }

                name = Combine(name, str);
                pos = typeName.IndexOf("+");
            }

            var space = name;
            str = typeName;

            if (offset < gArgs.Length)
            {
                pos = str.IndexOf('`');
                count = (int)(str[pos + 1] - '0');
                str = str.Substring(0, pos);
                str += "<" + string.Join(",", LuaMisc.GetGenericName(gArgs, offset, count)) + ">";
            }

            if (string.IsNullOrEmpty(space))
                space = t.Namespace;

            return space;
        }

        static string GetGenericTypeName(Type t)
        {
            var gArgs = t.GetGenericArguments();
            string typeName = t.FullName;
            int count = gArgs.Length;
            int pos = typeName.IndexOf("[");
            if (pos > 0)
                typeName = typeName.Substring(0, pos);

            string str = null;
            string name = null;
            int offset = 0;
            pos = typeName.IndexOf("+");

            while (pos > 0)
            {
                str = typeName.Substring(0, pos);
                typeName = typeName.Substring(pos + 1);
                pos = str.IndexOf('`');

                if (pos > 0)
                {
                    count = (int)(str[pos + 1] - '0');
                    str = str.Substring(0, pos);
                    str += "<" + string.Join(",", LuaMisc.GetGenericName(gArgs, offset, count)) + ">";
                    offset += count;
                }

                name = Combine(name, str);
                pos = typeName.IndexOf("+");
            }

            var space = name;
            str = typeName;

            if (offset < gArgs.Length)
            {
                pos = str.IndexOf('`');
                count = (int)(str[pos + 1] - '0');
                str = str.Substring(0, pos);
                str += "<" + string.Join(",", LuaMisc.GetGenericName(gArgs, offset, count)) + ">";
            }

            var libName = str;

            if (string.IsNullOrEmpty(space))
            {
                space = t.Namespace;

                if (space != null)
                {
                    libName = str.Substring(space.Length + 1);
                }
            }

            return libName;
        }

        public static string Combine(string ns, string name)
        {
            return string.IsNullOrEmpty(ns) ? name : ns + "." + name;
        }

        public static string GetFullName(Type type)
        {
            var ns = GetNamespace(type);
            var name = GetTypeName(type);
            return Combine(ns, name);
        }

        public static string GetNormalizedName(Type type)
        {
            return NormalizeName(GetTypeName(type));
        }

        public static string NormalizeName(string str)
        {
            if (string.IsNullOrEmpty(str))
                return null;

            str = str.Replace('<', '_');
            str = ToLuaStrings.RemoveChar(str, '>');
            str = str.Replace('[', 's');
            str = ToLuaStrings.RemoveChar(str, ']');
            str = str.Replace('.', '_');
            return str.Replace(',', '_');
        }
    }
}
