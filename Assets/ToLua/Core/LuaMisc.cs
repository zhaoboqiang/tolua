/*
Copyright (c) 2015-2017 topameng(topameng@qq.com)

Permission is hereby granted, free of charge, to any person obtaining a copy
of this software and associated documentation files (the "Software"), to deal
in the Software without restriction, including without limitation the rights
to use, copy, modify, merge, publish, distribute, sublicense, and/or sell
copies of the Software, and to permit persons to whom the Software is
furnished to do so, subject to the following conditions:

The above copyright notice and this permission notice shall be included in all
copies or substantial portions of the Software.

THE SOFTWARE IS PROVIDED "AS IS", WITHOUT WARRANTY OF ANY KIND, EXPRESS OR
IMPLIED, INCLUDING BUT NOT LIMITED TO THE WARRANTIES OF MERCHANTABILITY,
FITNESS FOR A PARTICULAR PURPOSE AND NONINFRINGEMENT. IN NO EVENT SHALL THE
AUTHORS OR COPYRIGHT HOLDERS BE LIABLE FOR ANY CLAIM, DAMAGES OR OTHER
LIABILITY, WHETHER IN AN ACTION OF CONTRACT, TORT OR OTHERWISE, ARISING FROM,
OUT OF OR IN CONNECTION WITH THE SOFTWARE OR THE USE OR OTHER DEALINGS IN THE
SOFTWARE.
*/
using System;
using System.Runtime.InteropServices;
using System.Collections.Generic;
using System.Reflection;
using System.Text;
using System.Runtime.CompilerServices;

namespace LuaInterface
{
    public class GCRef
    {
        public int reference;
        public string name = null;

        public GCRef(int reference, string name)
        {
            this.reference = reference;
            this.name = name;
        }
    }

    //让byte[] 压入成为lua string 而不是数组 userdata
    //也可以使用LuaByteBufferAttribute来标记byte[]
    public struct LuaByteBuffer
    {        
        public LuaByteBuffer(IntPtr source, int len)
            : this()            
        {
            buffer = new byte[len];
            Length = len;
            Marshal.Copy(source, buffer, 0, len);
        }
        
        public LuaByteBuffer(byte[] buf)
            : this()
        {
            buffer = buf;
            Length = buf.Length;            
        }

        public LuaByteBuffer(byte[] buf, int len)
            : this()
        {            
            buffer = buf;
            Length = len;
        }

        public LuaByteBuffer(System.IO.MemoryStream stream)   
            : this()         
        {
            buffer = stream.GetBuffer();
            Length = (int)stream.Length;            
        }

        public static implicit operator LuaByteBuffer(System.IO.MemoryStream stream)
        {
            return new LuaByteBuffer(stream);
        }

        public byte[] buffer;    

        public int Length
        {
            get;
            private set;
        }    
    }   

    public class LuaOut<T> { }
    //public class LuaOutMetatable {}
    public class NullObject { }

    //泛型函数参数null代替
    public struct nil { }

    public class LuaDelegate
    {
        public LuaFunction func = null;
        public LuaTable self = null;
        public MethodInfo method = null; 

        public LuaDelegate(LuaFunction func)
        {
            this.func = func;
        }

        public LuaDelegate(LuaFunction func, LuaTable self)
        {
            this.func = func;
            this.self = self;
        }

        //如果count不是1，说明还有其他人引用，只能等待gc来处理
        public virtual void Dispose()
        {
            method = null;

            if (func != null)
            {
                func.Dispose(1);
                func = null;
            }

            if (self != null)
            {
                self.Dispose(1);
                self = null;
            }
        }

        public override bool Equals(object o)
        {                                    
            if (o == null) return func == null && self == null;
            LuaDelegate ld = o as LuaDelegate;

            if (ld == null || ld.func != func || ld.self != self)
            {
                return false;
            }

            return ld.func != null;
        }

        static bool CompareLuaDelegate(LuaDelegate a, LuaDelegate b)
        {
            if (System.Object.ReferenceEquals(a, b))
            {
                return true;
            }

            object l = a;
            object r = b;

            if (l == null && r != null)
            {
                return b.func == null && b.self == null;
            }

            if (l != null && r == null)
            {
                return a.func == null && a.self == null;
            }

            if (a.func != b.func || a.self != b.self)
            {
                return false;
            }

            return a.func != null;
        }

        public static bool operator == (LuaDelegate a, LuaDelegate b)
        {
            return CompareLuaDelegate(a, b);
        }

        public static bool operator != (LuaDelegate a, LuaDelegate b)
        {
            return !CompareLuaDelegate(a, b);
        }
        public override int GetHashCode()
        {
            return RuntimeHelpers.GetHashCode(this);            
        }
    }

    [NoToLua]
    public static class LuaMisc
    {
        public static string GetArrayRank(Type t)
        {
            int count = t.GetArrayRank();

            if (count == 1)
            {                
                return "[]";
            }

            using (CString.Block())
            {
                CString sb = CString.Alloc(64);
                sb.Append('[');

                for (int i = 1; i < count; i++)
                {
                    sb.Append(',');
                }

                sb.Append(']');
                return sb.ToString();
            }
        }

        public static string GetTypeName(Type t)
        {
            if (t.IsArray)
            {
                string str = GetTypeName(t.GetElementType());
                str += GetArrayRank(t);
                return str;                
            }
            else if (t.IsByRef)
            {
                t = t.GetElementType();
                return GetTypeName(t);
            }
            else if (t.IsGenericType)
            {
                return GetGenericName(t);
            }
            else if (t == typeof(void))
            {
                return "void";
            }
            else
            {
                string name = GetPrimitiveStr(t);
                return name.Replace('+', '.');
            }
        }

        public static string[] GetGenericName(Type[] types, int offset, int count)
        {
            var results = new string[count];

            for (int i = 0; i < count; i++)
            {
                int pos = i + offset;

                if (types[pos].IsGenericType)
                {
                    results[i] = GetGenericName(types[pos]);
                }
                else
                {
                    results[i] = GetTypeName(types[pos]);
                }

            }

            return results;
        }

        static string CombineTypeStr(string space, string name)
        {
            if (string.IsNullOrEmpty(space))
            {
                return name;
            }
            else
            {
                return space + "." + name;
            }
        }

        private static string GetGenericName(Type t)
        {
            var gArgs = t.GetGenericArguments();
            var typeName = t.FullName ?? t.Name;
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
                    str += "<" + string.Join(",", GetGenericName(gArgs, offset, count)) + ">";
                    offset += count;
                }

                name = CombineTypeStr(name, str);
                pos = typeName.IndexOf("+");
            }

            str = typeName;

            if (offset < gArgs.Length)
            {
                pos = str.IndexOf('`');
                count = (int)(str[pos + 1] - '0');
                str = str.Substring(0, pos);
                str += "<" + string.Join(",", GetGenericName(gArgs, offset, count)) + ">";
            }

            return CombineTypeStr(name, str);
        }

        public static Delegate GetEventHandler(object obj, Type t, string eventName)
        {
            FieldInfo eventField = t.GetField(eventName, BindingFlags.GetField | BindingFlags.NonPublic | BindingFlags.Instance | BindingFlags.Static);
            return (Delegate)eventField.GetValue(obj);
        }

        public static string GetPrimitiveStr(Type t)
        {
            if (t == typeof(System.Single))
                return "float";
            if (t == typeof(System.String))
                return "string";
            if (t == typeof(System.Int32))
                return "int";
            if (t == typeof(System.Double))
                return "double";
            if (t == typeof(System.Boolean))
                return "bool";
            if (t == typeof(System.UInt32))
                return "uint";
            if (t == typeof(System.SByte))
                return "sbyte";
            if (t == typeof(System.Byte))
                return "byte";
            if (t == typeof(System.Int16))
                return "short";
            if (t == typeof(System.UInt16))
                return "ushort";
            if (t == typeof(System.Char))
                return "char";
            if (t == typeof(System.Int64))
                return "long";
            if (t == typeof(System.UInt64))
                return "ulong";
            if (t == typeof(System.Decimal))
                return "decimal";
            if (t == typeof(System.Object))
                return "object";
            return t.ToString();
        }        

        public static double ToDouble(object obj)
        {
            Type t = obj.GetType();

            if (t == typeof(double) || t == typeof(float))
                return Convert.ToDouble(obj);
            if (t == typeof(int))
                return (double)Convert.ToInt32(obj);
            if (t == typeof(uint))
                return (double)Convert.ToUInt32(obj);
            if (t == typeof(long))
                return (double)Convert.ToInt64(obj);
            if (t == typeof(ulong))
                return (double)Convert.ToUInt64(obj);
            if (t == typeof(byte))
                return (double)Convert.ToByte(obj);
            if (t == typeof(sbyte))
                return (double)Convert.ToSByte(obj);
            if (t == typeof(char))
                return (double)Convert.ToChar(obj);
            if (t == typeof(short))
                return (double)Convert.ToInt16(obj);
            if (t == typeof(ushort))
                return (double)Convert.ToUInt16(obj);

            return 0;
        }

        //可产生导出文件的基类
        public static Type GetExportBaseType(Type t)
        {
            Type baseType = t.BaseType;

            if (baseType == typeof(ValueType))
                return null;

            if (t.IsAbstract && t.IsSealed)
                return baseType == typeof(object) ? null : baseType;

            return baseType;
        }
    }       

    public class TouchBits
    {
        public const int DeltaPosition = 1;
        public const int Position = 2;
        public const int RawPosition = 4;
        public const int ALL = 7;
    }

    public class RaycastBits
    {
        public const int Collider = 1;
        public const int Normal = 2;
        public const int Point = 4;
        public const int Rigidbody = 8;
        public const int Transform = 16;
        public const int ALL = 31;
    }

    public enum EventOp
    {
        None = 0,
        Add = 1,
        Sub = 2,
    }

    public class EventObject
    {
        [NoToLua]
        public EventOp op = EventOp.None;
        [NoToLua]
        public Delegate func = null;
        [NoToLua]
        public Type type;

        [NoToLua]
        public EventObject(Type t)
        {
            type = t;
        }

        public static EventObject operator +(EventObject a, Delegate b)
        {
            a.op = EventOp.Add;
            a.func = b;
            return a;
        }

        public static EventObject operator -(EventObject a, Delegate b)
        {
            a.op = EventOp.Sub;
            a.func = b;
            return a;
        }
    }
}

