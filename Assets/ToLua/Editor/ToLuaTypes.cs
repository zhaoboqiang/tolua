using System;
using System.Collections.Generic;
using System.Reflection;

namespace LuaInterface.Editor
{
    public static class ToLuaTypes
    {
        static readonly NLog.Logger log = NLog.LoggerFactory.GetLogger(typeof(ToLuaTypes).Name);

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
                return GetGenericNamespace(t);

            var space = t.FullName;
            if (space == null)
                return t.Namespace;

            if (space.Contains("+"))
            {
                space = space.Replace('+', '.');
                int index = space.LastIndexOf('.');
                return space.Substring(0, index);
            }

            return t.Namespace;
        }

        public static string GetTypeName(Type t)
        {
            if (t.IsGenericType)
                return GetGenericTypeName(t);

            var space = t.FullName;
            if (space == null)
                return t.Name;

            if (space.Contains("+"))
            {
                space = space.Replace('+', '.');
                int index = space.LastIndexOf('.');
                return space.Substring(index + 1);
            }

            if (t.Namespace == null)
                return space;
                
            return space.Substring(t.Namespace.Length + 1);
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
            if (type == typeof(void))
                return "void";

            var name = GetTypeName(type);
            var ns = GetNamespace(type);
            var fullName = Combine(ns, name);
 
            if (fullName == "System.Single")
                return "float";
            if (fullName == "System.String")
                return "string";
            if (fullName == "System.Int32")
                return "int";
            if (fullName == "System.Double")
                return "double";
            if (fullName == "System.Boolean")
                return "bool";
            if (fullName == "System.UInt32")
                return "uint";
            if (fullName == "System.SByte")
                return "sbyte";
            if (fullName == "System.Byte")
                return "byte";
            if (fullName == "System.Int16")
                return "short";
            if (fullName == "System.UInt16")
                return "ushort";
            if (fullName == "System.Char")
                return "char";
            if (fullName == "System.Int64")
                return "long";
            if (fullName == "System.UInt64")
                return "ulong";
            if (fullName == "System.Decimal")
                return "decimal";
            /*
            if (fullName == "System.Object")
                return "object";
            */
            if (fullName == "System.Single&")
                return "float&";
            if (fullName == "System.String&")
                return "string&";
            if (fullName == "System.Int32&")
                return "int&";
            if (fullName == "System.Double&")
                return "double&";
            if (fullName == "System.Boolean&")
                return "bool&";
            if (fullName == "System.UInt32&")
                return "uint&";
            if (fullName == "System.SByte&")
                return "sbyte&";
            if (fullName == "System.Byte&")
                return "byte&";
            if (fullName == "System.Int16&")
                return "short&";
            if (fullName == "System.UInt16&")
                return "ushort&";
            if (fullName == "System.Char&")
                return "char&";
            if (fullName == "System.Int64&")
                return "long&";
            if (fullName == "System.UInt64&")
                return "ulong&";
            if (fullName == "System.Decimal&")
                return "decimal&";
            /*
            if (fullName == "System.Object&")
                return "object&";
            */
 
            return fullName;
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
