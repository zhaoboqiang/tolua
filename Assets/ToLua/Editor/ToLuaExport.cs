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

using UnityEngine;
using System;
using System.Collections;
using System.Text;
using System.Reflection;
using System.Collections.Generic;
using LuaInterface;
using System.Linq;
using System.IO;
using System.Runtime.CompilerServices;


namespace LuaInterface.Editor
{
[System.Flags]
public enum MetaOp
{
    None = 0,
    Add = 1,
    Sub = 2,
    Mul = 4,
    Div = 8,
    Eq = 16,
    Neg = 32,
    ToStr = 64,
    ALL = Add | Sub | Mul | Div | Eq | Neg | ToStr,
}

public enum ObjAmbig
{
    None = 0,
    U3dObj = 1,
    NetObj = 2,
    All = 3
}

public class ToLuaExport
{
    public string className = string.Empty;
    public Type type = null;
    public Type baseType = null;

    public bool isStaticClass = true;

    HashSet<string> usingList = new HashSet<string>();
    MetaOp op = MetaOp.None;
    StringBuilder sb = null;
    List<_MethodBase> methods = new List<_MethodBase>();
    Dictionary<string, int> nameCounter = new Dictionary<string, int>();
    FieldInfo[] fields = null;
    PropertyInfo[] props = null;
    List<PropertyInfo> propList = new List<PropertyInfo>(); //非静态属性
    List<PropertyInfo> allProps = new List<PropertyInfo>();
    EventInfo[] events = null;
    List<EventInfo> eventList = new List<EventInfo>();
    List<_MethodBase> ctorList = new List<_MethodBase>();
    List<ConstructorInfo> ctorExtList = new List<ConstructorInfo>();
    List<_MethodBase> getItems = new List<_MethodBase>(); //特殊属性
    List<_MethodBase> setItems = new List<_MethodBase>();

    BindingFlags binding = BindingFlags.Public | BindingFlags.Static | BindingFlags.IgnoreCase;

    ObjAmbig ambig = ObjAmbig.NetObj;

    //wrapClaaName + "Wrap" = 导出文件名，导出类名
    public string wrapClassName = "";

    public string libClassName = "";
    public string extendName = "";

    public HashSet<Type> eventSet = new HashSet<Type>();

    ToLuaPlatformFlags platformFlags;
    string platformFlagsText;

    private List<string> logs = new List<string>();

    class _MethodBase
    {
        public bool IsStatic => method.IsStatic;

        public bool IsConstructor => method.IsConstructor;

        public string Name => method.Name;

        public string FullName => method.ReflectedType.FullName + "." + Name;

        public MethodBase Method => method;

        public bool IsGenericMethod => method.IsGenericMethod;

        MethodBase method;
        ParameterInfo[] args;

        public _MethodBase(MethodBase m, int argCount = -1)
        {
            method = m;
            var infos = m.GetParameters();
            argCount = argCount != -1 ? argCount : infos.Length;
            args = new ParameterInfo[argCount];
            Array.Copy(infos, args, argCount);
        }

        public ParameterInfo[] GetParameters()
        {
            return args;
        }

        public int GetParamsCount()
        {
            int c = method.IsStatic ? 0 : 1;
            return args.Length + c;
        }

        public int GetEqualParamsCount(ToLuaExport export, _MethodBase b)
        {
            int count = 0;
            var list1 = new List<Type>();
            var list2 = new List<Type>();

            var type = export.type;

            if (!IsStatic)
                list1.Add(type);

            if (!b.IsStatic)
                list2.Add(type);

            for (int i = 0; i < args.Length; i++)
                list1.Add(args[i].ParameterType);

            var p = b.args;

            for (int i = 0; i < p.Length; i++)
                list2.Add(p[i].ParameterType);

            for (int i = 0; i < list1.Count; i++)
            {
                if (list1[i] != list2[i])
                    break;

                ++count;
            }

            return count;
        }

        public string GenParamTypes(ToLuaExport export, int offset = 0)
        {
            var sb = new StringBuilder();
            var list = new List<Type>();

            if (!method.IsStatic)
                list.Add(export.type);

            for (int i = 0; i < args.Length; i++)
            {
                if (export.IsParams(args[i]))
                    continue;

                if (args[i].ParameterType.IsByRef &&
                    (args[i].Attributes & ParameterAttributes.Out) != ParameterAttributes.None)
                {
                    Type genericClass = typeof(LuaOut<>);
                    Type t = genericClass.MakeGenericType(args[i].ParameterType.GetElementType());
                    list.Add(t);
                }
                else
                {
                    list.Add(GetGenericBaseType(method, args[i].ParameterType));
                }
            }

            for (int i = offset; i < list.Count - 1; i++)
                sb.Append(export.GetTypeOf(list[i], ", "));

            if (list.Count > 0)
                sb.Append(export.GetTypeOf(list[list.Count - 1], ""));

            return sb.ToString();
        }

        public bool HasSetIndex(ToLuaExport export)
        {
            if (method.Name == "set_Item")
            {
                return true;
            }

            var attrs = export.type.GetCustomAttributes(true);

            for (int i = 0; i < attrs.Length; i++)
            {
                if (attrs[i] is DefaultMemberAttribute)
                {
                    return method.Name == "set_ItemOf";
                }
            }

            return false;
        }

        public bool HasGetIndex(ToLuaExport export)
        {
            if (method.Name == "get_Item")
            {
                return true;
            }

            var attrs = export.type.GetCustomAttributes(true);

            for (int i = 0; i < attrs.Length; i++)
            {
                if (attrs[i] is DefaultMemberAttribute)
                {
                    return method.Name == "get_ItemOf";
                }
            }

            return false;
        }

        public Type GetReturnType()
        {
            var m = method as MethodInfo;
            return m != null ? m.ReturnType : null;
        }

        private string GetIndent(int indentLevel)
        {
            var indent = string.Empty;
            for (int i = 0; i < indentLevel; ++i)
                indent += "\t";
            return indent;
        }

        public string GetTotalName()
        {
            var ss = new string[args.Length];

            for (int i = 0; i < args.Length; i++)
            {
                ss[i] = ToLuaExport.GetTypeStr(args[i].GetType());
            }

            if (!ToLuaExport.IsGenericMethod(method))
            {
                return Name + "(" + string.Join(",", ss) + ")";
            }
            else
            {
                var gts = method.GetGenericArguments();
                var ts = new string[gts.Length];

                for (int i = 0; i < gts.Length; i++)
                {
                    ts[i] = ToLuaExport.GetTypeStr(gts[i]);
                }

                return Name + "<" + string.Join(",", ts) + ">" + "(" + string.Join(",", ss) + ")";
            }
        }

        private static string RemoveLastArg(string argsText)
        {
            var str = argsText;
            var ss = str.Split(',');
            return string.Join(",", ss, 0, ss.Length - 1);
        }

        private static void ProcessMethodCall(StringBuilder sb, string indent, string obj, MethodBase method, string args, int argCount, bool hasParams, string ret = null)
        {
            if (ret == null)
            {
                if (hasParams)
                {
                    sb.AppendLineEx($"{indent}if (arg{(argCount - 1)} != null)");
                    sb.AppendLineEx($"{indent}{{");
                    sb.AppendLineEx($"{indent}\t{obj}.{method.Name}({args});");
                    sb.AppendLineEx($"{indent}}}");
                    sb.AppendLineEx($"{indent}else");
                    sb.AppendLineEx($"{indent}{{");
                    sb.AppendLineEx($"{indent}\t{obj}.{method.Name}({RemoveLastArg(args)});"); // TODO: Instead remove of append.
                    sb.AppendLineEx($"{indent}}}");
                }
                else
                {
                    sb.AppendLineEx($"{indent}{obj}.{method.Name}({args});");
                }
            }
            else
            {
                if (hasParams)
                {
                    sb.AppendLineEx($"{indent}{ret} o;");
                    sb.AppendLineEx($"{indent}if (arg{(argCount - 1)} != null)");
                    sb.AppendLineEx($"{indent}\to = {obj}.{method.Name}({args});");
                    sb.AppendLineEx($"{indent}else");
                    sb.AppendLineEx($"{indent}\to = {obj}.{method.Name}({RemoveLastArg(args)});"); // TODO: Instead remove of append.
                }
                else
                {
                    sb.AppendLineEx($"{indent}{ret} o = {obj}.{method.Name}({args});");
                }
            }
        }

        private string GetArgsText(ParameterInfo[] parameters)
        {
            var sbArgs = new StringBuilder();
            var count = parameters.Length;

            for (int j = 0; j < count; j++)
            {
                var param = parameters[j];

                if (!param.ParameterType.IsByRef || ((param.Attributes & ParameterAttributes.In) != ParameterAttributes.None))
                {
                    sbArgs.Append("arg");
                }
                else
                {
                    if ((param.Attributes & ParameterAttributes.Out) != ParameterAttributes.None)
                    {
                        sbArgs.Append("out arg");
                    }
                    else
                    {
                        sbArgs.Append("ref arg");
                    }
                }

                sbArgs.Append(j);

                if (j != count - 1)
                {
                    sbArgs.Append(", ");
                }
            }

            return sbArgs.ToString();
        }

        private void GetRefArgs(ToLuaExport export, ParameterInfo[] parameters, out List<string> refList, out List<Type> refTypes)
        {
            refList = new List<string>();
            refTypes = new List<Type>();

            var count = parameters.Length;
            for (int j = 0; j < count; j++)
            {
                var param = parameters[j];

                if (!param.ParameterType.IsByRef || ((param.Attributes & ParameterAttributes.In) != ParameterAttributes.None))
                    continue;

                refList.Add("arg" + j);
                refTypes.Add(export.GetRefBaseType(param.ParameterType));
            }
        }

        public bool BeExtend = false;

        public int ProcessParams(ToLuaExport export, int tab, bool beConstruct, int checkTypePos)
        {
            var parameters = args;

            if (BeExtend)
            {
                var pt = new ParameterInfo[parameters.Length - 1];
                Array.Copy(parameters, 1, pt, 0, pt.Length);
                parameters = pt;
            }

            var count = parameters.Length;
            var indent = GetIndent(tab);
            var methodType = export.GetMethodType(method, out var pi);
            var offset = ((method.IsStatic && !BeExtend) || beConstruct) ? 1 : 2;

            if (method.Name == "op_Equality")
                checkTypePos = -1;

            var type = export.type;
            var className = export.className;

            var sb = export.sb;

            if ((!method.IsStatic && !beConstruct) || BeExtend)
            {
                if (checkTypePos > 0)
                {
                    export.CheckObject(indent, type, className, 1);
                }
                else
                {
                    if (method.Name == "Equals")
                    {
                        if (!export.type.IsValueType && checkTypePos > 0)
                        {
                            export.CheckObject(indent, type, className, 1);
                        }
                        else
                        {
                            sb.AppendFormat("{0}var obj = ({1})ToLua.ToObject(L, 1);\r", indent, className);
                        }
                    }
                    else if (checkTypePos > 0)
                    {
                        export.CheckObject(indent, type, className, 1);
                    }
                    else
                    {
                        export.ToObject(indent, type, className, 1);
                    }
                }
            }

            checkTypePos = checkTypePos - offset + 1;

            bool hasParams = false;
            for (int j = 0; j < count; j++)
            {
                var param = parameters[j];
                var arg = "arg" + j;
                bool beOutArg = param.ParameterType.IsByRef &&
                                ((param.Attributes & ParameterAttributes.Out) != ParameterAttributes.None);
                hasParams = export.IsParams(param);
                Type t = GetGenericBaseType(method, param.ParameterType);
                export.ProcessArg(t, indent, arg, offset + j, j >= checkTypePos, hasParams, beOutArg);
            }

            var argsText = GetArgsText(parameters);

            GetRefArgs(export, parameters, out var refList, out var refTypes);

            if (beConstruct)
            {
                export.sb.AppendLineEx($"{indent}var obj = new {className}({argsText});");
                var str = export.GetPushFunction(type);
                export.sb.AppendLineEx($"{indent}ToLua.{str}(L, obj);");

                for (int i = 0; i < refList.Count; i++)
                    export.GenPushStr(refTypes[i], refList[i], indent);

                return refList.Count + 1;
            }

            var obj = (method.IsStatic && !BeExtend) ? className : "obj";
            var retType = GetReturnType();

            if (retType == typeof(void))
            {
                if (HasSetIndex(export))
                {
                    if (methodType == 2)
                    {
                        var str = argsText;
                        var ss = str.Split(',');
                        str = string.Join(",", ss, 0, ss.Length - 1);

                        sb.AppendLineEx($"{indent}{obj}[{str}] = {ss[ss.Length - 1]};");
                    }
                    else if (methodType == 1)
                    {
                        sb.AppendLineEx($"{indent}{obj}.Item = arg0;");
                    }
                    else
                    {
                        ProcessMethodCall(sb, indent, obj, method, argsText, count, hasParams);
                    }
                }
                else if (methodType == 1)
                {
                    sb.AppendLineEx($"{indent}{obj}.{pi.Name} = arg0;");
                }
                else
                {
                    ProcessMethodCall(sb, indent, obj, method, argsText, count, hasParams);
                }
            }
            else
            {
                Type genericType = GetGenericBaseType(method, retType);
                string ret = ToLuaExport.GetTypeStr(genericType);

                if (method.Name.StartsWith("op_"))
                {
                    export.CallOpFunction(method.Name, tab, ret);
                }
                else if (HasGetIndex(export))
                {
                    if (methodType == 2)
                    {
                        sb.AppendLineEx($"{indent}{ret} o = {obj}[{argsText}];");
                    }
                    else if (methodType == 1)
                    {
                        sb.AppendLineEx($"{indent}{ret} o = {obj}.Item;");
                    }
                    else
                    {
                        ProcessMethodCall(sb, indent, obj, method, argsText, count, hasParams, ret);
                    }
                }
                else if (method.Name == "Equals")
                {
                    if (type.IsValueType || method.GetParameters().Length > 1)
                    {
                        sb.AppendLineEx($"{indent}{ret} o = {obj}.Equals({argsText});");
                    }
                    else
                    {
                        sb.AppendLineEx($"{indent}{ret} o = obj != null ? obj.Equals({argsText}) : arg0 == null;");
                    }
                }
                else if (methodType == 1)
                {
                    sb.AppendLineEx($"{indent}{ret} o = {obj}.{pi.Name};");
                }
                else
                {
                    ProcessMethodCall(sb, indent, obj, method, argsText, count, hasParams, ret);
                }

                bool isbuffer = IsByteBuffer();
                export.GenPushStr(retType, "o", indent, isbuffer);
            }

            for (int i = 0; i < refList.Count; i++)
            {
                if (refTypes[i] == typeof(RaycastHit) && method.Name == "Raycast" && (type == typeof(Physics) || type == typeof(Collider)))
                {
                    sb.AppendLineEx($"{indent}if (o) ToLua.Push(L, {refList[i]}); else LuaDLL.lua_pushnil(L);");
                }
                else
                {
                    export.GenPushStr(refTypes[i], refList[i], indent);
                }
            }

            if (!method.IsStatic && type.IsValueType && method.Name != "ToString")
            {
                sb.Append(indent + "ToLua.SetBack(L, 1, obj);\r");
            }

            return refList.Count;
        }

        bool IsByteBuffer()
        {
            var attrs = method.GetCustomAttributes(true);

            for (int j = 0; j < attrs.Length; j++)
            {
                Type t = attrs[j].GetType();

                if (t == typeof(LuaByteBufferAttribute))
                {
                    return true;
                }
            }

            return false;
        }
    }

    public ToLuaExport()
    {
        className = null;
        type = null;
        baseType = null;
        isStaticClass = false;
        usingList.Clear();
        op = MetaOp.None;
        sb = new StringBuilder();
        fields = null;
        props = null;
        methods.Clear();
        allProps.Clear();
        propList.Clear();
        eventList.Clear();
        ctorList.Clear();
        ctorExtList.Clear();
        ambig = ObjAmbig.NetObj;
        wrapClassName = "";
        libClassName = "";
        extendName = "";
        eventSet.Clear();
        nameCounter.Clear();
        events = null;
        getItems.Clear();
        setItems.Clear();
    }

    private MetaOp GetOp(string name)
    {
        if (name == "op_Addition")
            return MetaOp.Add;
        if (name == "op_Subtraction")
            return MetaOp.Sub;
        if (name == "op_Equality")
            return MetaOp.Eq;
        if (name == "op_Multiply")
            return MetaOp.Mul;
        if (name == "op_Division")
            return MetaOp.Div;
        if (name == "op_UnaryNegation")
            return MetaOp.Neg;
        if (name == "ToString" && !isStaticClass)
            return MetaOp.ToStr;

        return MetaOp.None;
    }

    //操作符函数无法通过继承metatable实现
    void GenBaseOpFunction(List<_MethodBase> methodBases)
    {
        var baseType = type.BaseType;

        while (baseType != null)
        {
            if (allTypes.Contains(baseType))
            {
                var methods = baseType.GetMethods(BindingFlags.Instance | BindingFlags.Public | BindingFlags.Static | BindingFlags.IgnoreCase);

                for (int i = 0; i < methods.Length; i++)
                {
                    var baseOp = GetOp(methods[i].Name);

                    if (baseOp != MetaOp.None && (op & baseOp) == 0)
                    {
                        if (baseOp != MetaOp.ToStr)
                        {
                            methodBases.Add(new _MethodBase(methods[i]));
                        }

                        op |= baseOp;
                    }
                }
            }

            baseType = baseType.BaseType;
        }
    }

    public void Generate(string dir)
    {
#if !EXPORT_INTERFACE

        var iterType = typeof(System.Collections.IEnumerator);
        if (type.IsInterface && type != iterType)
            return;

#endif

        sb = new StringBuilder();
        usingList.Add("System");

        platformFlags = ReflectTypes.GetPlatformFlags(type);
        platformFlagsText = ToLuaPlatformUtility.GetText(platformFlags);

        if (type.IsEnum)
        {
            if (BeginCodeGen())
            {
                GenerateEnum();
                EndCodeGen(dir);
            }
        }
        else
        {
            InitMethods();
            InitPropertyList();
            InitCtorList();

            if (BeginCodeGen())
            {
                RegisterClass();
                GenerateConstructor();
                GenerateIndexer();
                GenerateMethods();
                GenIndexFunc();
                GenNewIndexFunc();
                //GenOutFunction();
                GenEventFunctions();

                EndCodeGen(dir);
            }
        }
    }

    //记录所有的导出类型
    public static Type[] allTypes;

    bool BeDropMethodType(MethodInfo md)
    {
        var t = md.DeclaringType;
        if (t == type)
            return true;

        return !allTypes.Contains(t);
    }

    //是否为委托类型，没处理废弃
    public static bool IsDelegateType(Type t)
    {
        if (!typeof(System.MulticastDelegate).IsAssignableFrom(t) || t == typeof(System.MulticastDelegate))
            return false;

        return true;
    }

    void BeginPlatformMacro(string flags)
    {
        ToLuaPlatformUtility.BeginPlatformMacro(sb, flags);
    }

    void EndPlatformMacro(string flags)
    {
        ToLuaPlatformUtility.EndPlatformMacro(sb, flags);
    }

    bool BeginCodeGen()
    {
        if (platformFlags == ToLuaPlatformFlags.None)
            return false;

        BeginPlatformMacro(platformFlagsText);

        sb.AppendFormat("public class {0}Wrap\r", wrapClassName);
        sb.AppendLineEx("{");

        return true;
    }

    void EndCodeGen(string dir)
    {
        sb.AppendLineEx("}\r");

        EndPlatformMacro(platformFlagsText);

        SaveFile(dir + wrapClassName + "Wrap.cs");
    }

    void InitMethods()
    {
        bool flag = false;

        if (baseType != null || isStaticClass)
        {
            binding |= BindingFlags.DeclaredOnly;
            flag = true;
        }

        var methodBases = new List<_MethodBase>();
        foreach (var methodInfo in type.GetMethods(BindingFlags.Instance | binding))
        {
            var name = methodInfo.Name;
            if ((name.StartsWith("op_") || name.StartsWith("add_") || name.StartsWith("remove_")) && !IsNeedOp(name))
                continue;

            if (!ReflectMethods.Included(methodInfo))
                continue;
 
            methodBases.Add(new _MethodBase(methodInfo));
        }

        var ps = type.GetProperties();

        for (int i = 0; i < ps.Length; i++)
        {
            var propertyInfo = ps[i];
            if (ToLuaTypes.IsUnsupported(propertyInfo))
            {
                methodBases.RemoveAll((p) => { return p.Method == propertyInfo.GetGetMethod() || p.Method == propertyInfo.GetSetMethod(); });
            }
            else
            {
                var md = propertyInfo.GetGetMethod();
                if (md != null)
                {
                    int index = methodBases.FindIndex((m) => { return m.Method == md; });
                    if (index >= 0)
                    {
                        if (md.GetParameters().Length == 0)
                        {
                            methodBases.RemoveAt(index);
                        }
                        else if (methodBases[index].HasGetIndex(this))
                        {
                            getItems.Add(methodBases[index]);
                        }
                    }
                }

                md = propertyInfo.GetSetMethod();
                if (md != null)
                {
                    int index = methodBases.FindIndex((m) => { return m.Method == md; });
                    if (index >= 0)
                    {
                        if (md.GetParameters().Length == 1)
                        {
                            methodBases.RemoveAt(index);
                        }
                        else if (methodBases[index].HasSetIndex(this))
                        {
                            setItems.Add(methodBases[index]);
                        }
                    }
                }
            }
        }

        if (flag && !isStaticClass)
        {
            var baseList = new List<MethodInfo>(type.GetMethods(BindingFlags.Public | BindingFlags.Instance | BindingFlags.Static | BindingFlags.IgnoreCase));

            for (int i = baseList.Count - 1; i >= 0; i--)
            {
                if (BeDropMethodType(baseList[i]))
                {
                    baseList.RemoveAt(i);
                }
            }

            var addList = new HashSet<MethodInfo>();

            for (int i = 0; i < methodBases.Count; i++)
            {
                var methodBase = methodBases[i];

                var mds = baseList.FindAll((p) => { return p.Name == methodBase.Name; });

                for (int j = 0; j < mds.Count; j++)
                {
                    addList.Add(mds[j]);
                    baseList.Remove(mds[j]);
                }
            }

            foreach (var iter in addList)
            {
                methodBases.Add(new _MethodBase(iter));
            }
        }

        for (int i = 0; i < methodBases.Count; i++)
        {
            var methodBase = methodBases[i];

            GetDelegateTypeFromMethodParams(methodBase);
        }

        ProcessExtends(methodBases);
        GenBaseOpFunction(methodBases);

        for (int i = 0; i < methodBases.Count; i++)
        {
            var methodBase = methodBases[i];

            int count = GetDefalutParamCount(methodBase.Method);
            int length = methodBase.GetParameters().Length;

            for (int j = 0; j < count + 1; j++)
            {
                var r = new _MethodBase(methodBase.Method, length - j);
                r.BeExtend = methodBase.BeExtend;
                methods.Add(r);
            }
        }
    }

    void InitPropertyList()
    {
        props = type.GetProperties(
            BindingFlags.GetProperty | BindingFlags.SetProperty | BindingFlags.Instance | binding);
        propList.AddRange(type.GetProperties(BindingFlags.GetProperty | BindingFlags.SetProperty |
                                             BindingFlags.Instance | BindingFlags.Public | BindingFlags.IgnoreCase));
        fields = type.GetFields(BindingFlags.GetField | BindingFlags.SetField | BindingFlags.Instance | binding);
        events = type.GetEvents(BindingFlags.DeclaredOnly | BindingFlags.Instance | BindingFlags.Public |
                                BindingFlags.Static);
        eventList.AddRange(type.GetEvents(BindingFlags.DeclaredOnly | BindingFlags.Instance | BindingFlags.Public));

        var fieldList = new List<FieldInfo>();
        fieldList.AddRange(fields);

        for (int i = fieldList.Count - 1; i >= 0; i--)
        {
            var field = fieldList[i];
            if (ToLuaTypes.IsUnsupported(field))
            {
                fieldList.RemoveAt(i);
            }
            else if (IsDelegateType(field.FieldType))
            {
                eventSet.Add(field.FieldType);
            }
        }

        fields = fieldList.ToArray();

        var piList = new List<PropertyInfo>();
        piList.AddRange(props);

        for (int i = piList.Count - 1; i >= 0; i--)
        {
            var pi = piList[i];
            if (ToLuaTypes.IsUnsupported(pi))
            {
                piList.RemoveAt(i);
            }
            else if (pi.Name == "Item" && IsItemThis(pi))
            {
                piList.RemoveAt(i);
            }
            else if (pi.GetGetMethod() != null && HasGetIndex(pi.GetGetMethod()))
            {
                piList.RemoveAt(i);
            }
            else if (pi.GetSetMethod() != null && HasSetIndex(pi.GetSetMethod()))
            {
                piList.RemoveAt(i);
            }
            else if (IsDelegateType(pi.PropertyType))
            {
                eventSet.Add(pi.PropertyType);
            }
        }

        props = piList.ToArray();

        for (int i = propList.Count - 1; i >= 0; i--)
        {
            var prop = propList[i];
            if (ToLuaTypes.IsUnsupported(prop))
            {
                propList.RemoveAt(i);
            }
        }

        allProps.AddRange(props);
        allProps.AddRange(propList);

        var evList = new List<EventInfo>();
        evList.AddRange(events);

        for (int i = evList.Count - 1; i >= 0; i--)
        {
            var ev = evList[i];
            if (ToLuaTypes.IsUnsupported(ev))
            {
                evList.RemoveAt(i);
            }
            else if (IsDelegateType(ev.EventHandlerType))
            {
                eventSet.Add(ev.EventHandlerType);
            }
        }

        events = evList.ToArray();

        for (int i = eventList.Count - 1; i >= 0; i--)
        {
            var ev = eventList[i];
            if (ToLuaTypes.IsUnsupported(ev))
            {
                eventList.RemoveAt(i);
            }
        }
    }

    void SaveFile(string file)
    {
        using (var textWriter = new StreamWriter(File.Create(file), Encoding.UTF8))
        {
            var usb = new StringBuilder();
            usb.AppendLineEx("// this source code was auto-generated by tolua#, do not modify it");

            foreach (var str in usingList)
            {
                usb.AppendFormat("using {0};\r", str);
            }

            usb.AppendLineEx("using LuaInterface;");

            if (ambig == ObjAmbig.All)
            {
                usb.AppendLineEx("using Object = UnityEngine.Object;");
            }

            usb.AppendLineEx();

            textWriter.Write(usb.ToString());
            textWriter.Write(sb.ToString());
            textWriter.Flush();
            textWriter.Close();
        }
    }

    string GetMethodName(MethodBase md)
    {
        if (md.Name.StartsWith("op_"))
        {
            return md.Name;
        }

        var attrs = md.GetCustomAttributes(true);

        for (int i = 0; i < attrs.Length; i++)
        {
            if (attrs[i] is LuaRenameAttribute)
            {
                var attr = attrs[i] as LuaRenameAttribute;
                return attr.Name;
            }
        }

        return md.Name;
    }

    bool HasGetIndex(MemberInfo md)
    {
        if (md.Name == "get_Item")
            return true;

        var attrs = type.GetCustomAttributes(true);

        for (int i = 0; i < attrs.Length; i++)
        {
            if (attrs[i] is DefaultMemberAttribute)
            {
                return md.Name == "get_ItemOf";
            }
        }

        return false;
    }

    bool HasSetIndex(MemberInfo md)
    {
        if (md.Name == "set_Item")
            return true;

        var attrs = type.GetCustomAttributes(true);

        for (int i = 0; i < attrs.Length; i++)
        {
            if (attrs[i] is DefaultMemberAttribute)
            {
                return md.Name == "set_ItemOf";
            }
        }

        return false;
    }

    bool IsThisArray(MethodBase md, int count)
    {
        var pis = md.GetParameters();

        if (pis.Length != count)
            return false;

        if (pis[0].ParameterType == typeof(int))
            return true;

        return false;
    }

    string GetRegisterFunctionName(string name)
    {
        return name == "Register" ? "_Register" : name;
    }

    void RegisterMethod(MethodBase methodBase, string methodName, string methodPlatformFlagsText)
    {
        BeginPlatformMacro(methodPlatformFlagsText);

        if (methodName == "get_Item" && IsThisArray(methodBase, 1))
            sb.AppendLine("\t\tL.RegFunction(\".geti\", get_Item);\r");
        else if (methodName == "set_Item" && IsThisArray(methodBase, 2))
            sb.AppendLine("\t\tL.RegFunction(\".seti\", set_Item);\r");

        if (!methodName.StartsWith("op_"))
            sb.AppendFormat("\t\tL.RegFunction(\"{0}\", {1});\r", methodName, GetRegisterFunctionName(methodName));

        EndPlatformMacro(methodPlatformFlagsText);
    }

    void RegisterMethods()
    {
        //注册库函数
        for (int i = 0; i < methods.Count; i++)
        {
            var m = methods[i];
            int count = 1;

            if (IsGenericMethod(m.Method))
                continue;

            var fieldPlatformFlags = ReflectFields.GetPlatformFlags(m.FullName);
            if (fieldPlatformFlags == ToLuaPlatformFlags.None)
                continue;

            var fieldPlatformFlagsText = ToLuaPlatformUtility.GetText(fieldPlatformFlags);

            string name = GetMethodName(m.Method);

            if (!nameCounter.TryGetValue(name, out count))
            {
                RegisterMethod(m.Method, name, fieldPlatformFlagsText);

                nameCounter[name] = 1;
            }
            else
            {
                nameCounter[name] = count + 1;
            }
        }

    }

    void RegisterConstructor()
    {
        if (ctorList.Count > 0 || type.IsValueType || ctorExtList.Count > 0)
        {
            sb.AppendFormat("\t\tL.RegFunction(\"New\", _Create{0});\r", wrapClassName);
        }
    }

    void RegisterIndexer()
    {
        if (getItems.Count > 0 || setItems.Count > 0)
        {
            sb.AppendLineEx("\t\tL.RegVar(\"this\", _this, null);");
        }
    }

    void RegisterOpItems()
    {
        if ((op & MetaOp.Add) != 0)
            sb.AppendLineEx("\t\tL.RegFunction(\"__add\", op_Addition);");

        if ((op & MetaOp.Sub) != 0)
            sb.AppendLineEx("\t\tL.RegFunction(\"__sub\", op_Subtraction);");

        if ((op & MetaOp.Mul) != 0)
            sb.AppendLineEx("\t\tL.RegFunction(\"__mul\", op_Multiply);");

        if ((op & MetaOp.Div) != 0)
            sb.AppendLineEx("\t\tL.RegFunction(\"__div\", op_Division);");

        if ((op & MetaOp.Eq) != 0)
            sb.AppendLineEx("\t\tL.RegFunction(\"__eq\", op_Equality);");

        if ((op & MetaOp.Neg) != 0)
            sb.AppendLineEx("\t\tL.RegFunction(\"__unm\", op_UnaryNegation);");

        if ((op & MetaOp.ToStr) != 0)
            sb.AppendLineEx("\t\tL.RegFunction(\"__tostring\", ToLua.op_ToString);");
    }

    bool IsItemThis(PropertyInfo info)
    {
        var md = info.GetGetMethod();
        if (md != null)
            return md.GetParameters().Length != 0;

        md = info.GetSetMethod();
        if (md != null)
            return md.GetParameters().Length != 1;

        return true;
    }

    private bool CanRead(PropertyInfo propertyInfo)
    {
        var propertyFieldSetting = ReflectProperties.Lookup(propertyInfo);

        return propertyFieldSetting.CanRead && propertyInfo.CanRead && propertyInfo.GetGetMethod(true).IsPublic;
    }

    private bool CanWrite(PropertyInfo propertyInfo)
    {
        var propertyFieldSetting = ReflectProperties.Lookup(propertyInfo);

        return propertyFieldSetting.CanWrite && propertyInfo.CanWrite && propertyInfo.GetSetMethod(true).IsPublic;
    }

    void RegisterProperties()
    {
        foreach (var field in fields)
        {
            var fieldPlatformFlags = ReflectFields.GetPlatformFlags(field);
            if (fieldPlatformFlags == ToLuaPlatformFlags.None)
                continue;

            var fieldPlatformFlagsText = ToLuaPlatformUtility.GetText(fieldPlatformFlags);

            BeginPlatformMacro(fieldPlatformFlagsText);

            var name = field.Name;

            if (field.IsLiteral || field.IsPrivate || field.IsInitOnly)
            {
                if (field.IsLiteral && field.FieldType.IsPrimitive && !field.FieldType.IsEnum)
                {
                    sb.AppendFormat("\t\tL.RegConstant(\"{0}\", {1});\r", name, ToLuaMembers.GetName(field));
                }
                else
                {
                    sb.AppendFormat("\t\tL.RegVar(\"{0}\", get_{0}, null);\r", name);
                }
            }
            else
            {
                sb.AppendFormat("\t\tL.RegVar(\"{0}\", get_{0}, set_{0});\r", name);
            }

            EndPlatformMacro(fieldPlatformFlagsText);
        }

        foreach (var prop in props)
        {
            var fieldPlatformFlags = ReflectFields.GetPlatformFlags(prop);
            if (fieldPlatformFlags == ToLuaPlatformFlags.None)
                continue;

            var fieldPlatformFlagsText = ToLuaPlatformUtility.GetText(fieldPlatformFlags);

            var canRead = CanRead(prop);
            var canWrite = CanWrite(prop);

            BeginPlatformMacro(fieldPlatformFlagsText);

            var name = prop.Name;

            if (canRead && canWrite)
            {
                var md = methods.Find((p) => { return p.Name == "get_" + name; });
                var get = md == null ? "get" : "_get";
                md = methods.Find((p) => { return p.Name == "set_" + name; });
                var set = md == null ? "set" : "_set";
                sb.AppendFormat("\t\tL.RegVar(\"{0}\", {1}_{0}, {2}_{0});\r", name, get, set);
            }
            else if (canRead)
            {
                var md = methods.Find((p) => { return p.Name == "get_" + name; });
                sb.AppendFormat("\t\tL.RegVar(\"{0}\", {1}_{0}, null);\r", name, md == null ? "get" : "_get");
            }
            else if (canWrite)
            {
                var md = methods.Find((p) => { return p.Name == "set_" + name; });
                sb.AppendFormat("\t\tL.RegVar(\"{0}\", null, {1}_{0});\r", name, md == null ? "set" : "_set");
            }

            EndPlatformMacro(fieldPlatformFlagsText);
        }

        foreach (var eventInfo in events)
        {
            var fieldPlatformFlags = ReflectFields.GetPlatformFlags(eventInfo);
            if (fieldPlatformFlags == ToLuaPlatformFlags.None)
                continue;

            var fieldPlatformFlagsText = ToLuaPlatformUtility.GetText(fieldPlatformFlags);

            BeginPlatformMacro(fieldPlatformFlagsText);
            sb.AppendFormat("\t\tL.RegVar(\"{0}\", get_{0}, set_{0});\r", eventInfo.Name);
            EndPlatformMacro(fieldPlatformFlagsText);
        }
    }

    void RegisterEvents()
    {
        var list = new List<Type>();

        foreach (var t in eventSet)
        {
            var space = GetNameSpace(t, out var funcName);
            if (space != className)
            {
                list.Add(t);
                continue;
            }

            var fieldPlatformFlags = ReflectTypes.GetPlatformFlags(t);
            if (fieldPlatformFlags == ToLuaPlatformFlags.None)
                continue;

            var fieldPlatformFlagsText = ToLuaPlatformUtility.GetText(fieldPlatformFlags);

            BeginPlatformMacro(fieldPlatformFlagsText);

            funcName = ConvertToLibSign(funcName);
            var funcFullName = ConvertToLibSign(space) + "_" + funcName;

            sb.AppendFormat("\t\tL.RegFunction(\"{0}\", {1}); // [RegisterEvents] \r", funcName, funcFullName);

            EndPlatformMacro(fieldPlatformFlagsText);
        }

        for (int i = 0; i < list.Count; i++)
        {
            eventSet.Remove(list[i]);
        }
    }

    bool BeginRegisterClass()
    {
        sb.AppendLineEx("\tpublic static void Register(LuaState L)");
        sb.AppendLineEx("\t{");

        if (isStaticClass)
        {
            sb.AppendFormat("\t\tL.BeginStaticLibs(\"{0}\");\r", libClassName);
        }
        else if (!type.IsGenericType)
        {
            if (baseType == null)
            {
                sb.AppendFormat("\t\tL.BeginClass(typeof({0}), null);\r", className);
            }
            else
            {
                sb.AppendFormat("\t\tL.BeginClass(typeof({0}), typeof({1}));\r", className, GetBaseTypeStr(baseType));
            }
        }
        else
        {
            if (baseType == null)
            {
                sb.AppendFormat("\t\tL.BeginClass(typeof({0}), null, \"{1}\");\r", className, libClassName);
            }
            else
            {
                sb.AppendFormat("\t\tL.BeginClass(typeof({0}), typeof({1}), \"{2}\");\r", className, GetBaseTypeStr(baseType), libClassName);
            }
        }

        return true;
    }

    void EndRegisterClass()
    {
        if (!isStaticClass)
        {
            sb.AppendFormat("\t\tL.EndClass();\r");
        }
        else
        {
            sb.AppendFormat("\t\tL.EndStaticLibs();\r");
        }

        sb.AppendLineEx("\t}");
    }

    void RegisterClass()
    {
        if (BeginRegisterClass())
        {
            RegisterMethods();
            RegisterConstructor();
            RegisterIndexer();
            RegisterOpItems();
            RegisterProperties();
            RegisterEvents();

            EndRegisterClass();
        }
    }

    bool IsParams(ParameterInfo param)
    {
        return param.GetCustomAttributes(typeof(ParamArrayAttribute), false).Length > 0;
    }

    void GenFunction(_MethodBase m)
    {
        var fieldPlatformFlags = ReflectFields.GetPlatformFlags(m.FullName);
        if (fieldPlatformFlags == ToLuaPlatformFlags.None)
            return;

        var fieldPlatformFlagsText = ToLuaPlatformUtility.GetText(fieldPlatformFlags);

        BeginPlatformMacro(fieldPlatformFlagsText);

        var name = GetMethodName(m.Method);

        sb.AppendLineEx("\r\t[MonoPInvokeCallback(typeof(LuaCSFunction))]");
        sb.AppendFormat("\tstatic int {0}(IntPtr L)\r", GetRegisterFunctionName(name));
        sb.AppendLineEx("\t{");

        var parameters = m.GetParameters();
        int offset = m.IsStatic ? 0 : 1;
        bool haveParams = HasOptionalParam(parameters);
        int rc = m.GetReturnType() == typeof(void) ? 0 : 1;

        BeginTry();

        if (!haveParams)
        {
            int count = parameters.Length + offset;
            if (m.Name == "op_UnaryNegation")
                count = 2;
            sb.AppendFormat("\t\t\tToLua.CheckArgsCount(L, {0});\r", count);
        }
        else
        {
            sb.AppendLineEx("\t\t\tint count = LuaDLL.lua_gettop(L);");
        }

        rc += m.ProcessParams(this, 3, false, int.MaxValue);
        sb.AppendFormat("\t\t\treturn {0};\r", rc);
        EndTry();

        sb.AppendLineEx("\t}");

        EndPlatformMacro(fieldPlatformFlagsText);
    }

    //没有未知类型的模版类型List<int> 返回false, List<T>返回true
    static bool IsGenericConstraintType(Type t)
    {
        if (!t.IsGenericType)
            return t.IsGenericParameter || t == typeof(System.ValueType);

        var types = t.GetGenericArguments();

        for (int i = 0; i < types.Length; i++)
        {
            var t1 = types[i];

            if (t1.IsGenericParameter || t1 == typeof(System.ValueType))
                return true;

            if (IsGenericConstraintType(t1))
                return true;
        }

        return false;
    }

    static bool IsGenericConstraints(Type[] constraints)
    {
        for (int i = 0; i < constraints.Length; i++)
        {
            if (!IsGenericConstraintType(constraints[i]))
            {
                return false;
            }
        }

        return true;
    }

    static bool IsGenericMethod(MethodBase md)
    {
        if (md.IsGenericMethod)
        {
            var arguments = md.GetGenericArguments();
            var list = new List<ParameterInfo>(md.GetParameters());

            for (int i = 0; i < arguments.Length; i++)
            {
                var constraints = arguments[i].GetGenericParameterConstraints();
                if (constraints == null || constraints.Length == 0 || IsGenericConstraints(constraints))
                    return true;

                var p = list.Find((iter) => iter.ParameterType == arguments[i]);
                if (p == null)
                    return true;

                list.RemoveAll((iter) => iter.ParameterType == arguments[i]);
            }

            for (int i = 0; i < list.Count; i++)
            {
                var t = list[i].ParameterType;
                if (IsGenericConstraintType(t))
                {
                    return true;
                }
            }
        }

        return false;
    }

    void GenerateMethods()
    {
        var set = new HashSet<string>();

        for (int i = 0; i < methods.Count; i++)
        {
            var m = methods[i];

            if (IsGenericMethod(m.Method))
            {
                var typeName = LuaMisc.GetTypeName(type);
                var totalName = m.GetTotalName();

                logs.Add($"Generic Method {typeName}.{totalName} cannot be export to lua");
                continue;
            }

            var name = GetMethodName(m.Method);
            if (!nameCounter.TryGetValue(name, out var count))
            {
                var typeName = LuaMisc.GetTypeName(type);
                var totalName = m.GetTotalName();

                logs.Add($"Not register method {typeName}.{totalName}");
                continue;
            }

            if (count > 1)
            {
                if (!set.Contains(name))
                {
                    var mi = GenOverrideFunc(name);
                    if (mi == null)
                    {
                        set.Add(name);
                        continue;
                    }
                    else
                    {
                        m = mi; //非重载函数，或者折叠之后只有一个函数
                    }
                }
                else
                {
                    continue;
                }
            }

            set.Add(name);
            GenFunction(m);
        }
    }

    bool IsSealedType(Type t)
    {
        if (t.IsSealed)
            return true;

        if (t.IsGenericType && (t.GetGenericTypeDefinition() == typeof(List<>) ||
                                t.GetGenericTypeDefinition() == typeof(Dictionary<,>)))
        {
            return true;
        }

        return false;
    }

    static bool IsIEnumerator(Type t)
    {
        if (t == typeof(IEnumerator) || t == typeof(CharEnumerator))
            return true;

        if (typeof(IEnumerator).IsAssignableFrom(t))
        {
            if (t.IsGenericType)
            {
                var gt = t.GetGenericTypeDefinition();
                if (gt == typeof(List<>.Enumerator) || gt == typeof(Dictionary<,>.Enumerator))
                {
                    return true;
                }
            }
        }

        return false;
    }

    string GetPushFunction(Type t, bool isByteBuffer = false)
    {
        if (t.IsEnum || t.IsPrimitive || t == typeof(string) ||
            t == typeof(LuaTable) || t == typeof(LuaCSFunction) || t == typeof(LuaThread) || t == typeof(LuaFunction) ||
            t == typeof(Type) || t == typeof(IntPtr) ||
            t == typeof(LuaByteBuffer) /* || t == typeof(LuaInteger64) */ ||
            t == typeof(Vector3) || t == typeof(Vector2) || t == typeof(Vector4) ||
            t == typeof(Quaternion) || t == typeof(Color) || t == typeof(RaycastHit) ||
            t == typeof(Ray) || t == typeof(Touch) || t == typeof(Bounds) || t == typeof(object) ||
            typeof(Delegate).IsAssignableFrom(t))
        {
            return "Push";
        }

        if (t.IsArray || t == typeof(System.Array))
            return "Push";

        if (IsIEnumerator(t))
            return "Push";

        if (t == typeof(LayerMask))
            return "PushLayerMask";

        if (typeof(UnityEngine.Object).IsAssignableFrom(t) || typeof(UnityEngine.TrackedReference).IsAssignableFrom(t))
            return IsSealedType(t) ? "PushSealed" : "Push";

        if (t.IsValueType)
        {
            if (t.IsGenericType && t.GetGenericTypeDefinition() == typeof(Nullable<>))
                return "PusNullable";

            return "PushValue";
        }

        if (IsSealedType(t))
            return "PushSealed";

        return "PushObject";
    }

    void DefaultConstruct()
    {
        sb.AppendLineEx("\r\t[MonoPInvokeCallback(typeof(LuaCSFunction))]");
        sb.AppendFormat("\tstatic int _Create{0}(IntPtr L)\r", wrapClassName);
        sb.AppendLineEx("\t{");
        sb.AppendFormat("\t\tvar obj = new {0}();\r", className);
        GenPushStr(type, "obj", "\t\t");
        sb.AppendLineEx("\t\treturn 1;");
        sb.AppendLineEx("\t}");
    }

    string GetCountStr(int count)
    {
        if (count != 0)
            return $"count - {count}";

        return "count";
    }

    void GenOutFunction()
    {
        if (isStaticClass)
            return;

        sb.AppendLineEx("\r\t[MonoPInvokeCallback(typeof(LuaCSFunction))]");
        sb.AppendLineEx("\tstatic int get_out(IntPtr L)");
        sb.AppendLineEx("\t{");
        sb.AppendFormat("\t\tToLua.PushOut<{0}>(L, new LuaOut<{0}>());\r", className);
        sb.AppendLineEx("\t\treturn 1;");
        sb.AppendLineEx("\t}");
    }

    int GetDefalutParamCount(MethodBase md)
    {
        int count = 0;
        var infos = md.GetParameters();

        for (int i = 0; i < infos.Length; i++)
        {
            if (!(infos[i].DefaultValue is DBNull))
            {
                ++count;
            }
        }

        return count;
    }

    void InitCtorList()
    {
        if (isStaticClass || type.IsAbstract || typeof(MonoBehaviour).IsAssignableFrom(type))
            return;

        var constructors = type.GetConstructors(BindingFlags.Instance | binding);
        if (constructors.Length == 0)
            return;

        bool isGenericType = type.IsGenericType;
        Type genericType = isGenericType ? type.GetGenericTypeDefinition() : null;
        Type dictType = typeof(Dictionary<,>);

        for (int i = 0; i < constructors.Length; i++)
        {
            if (ToLuaTypes.IsUnsupported(constructors[i]))
                continue;

            int count = GetDefalutParamCount(constructors[i]);
            int length = constructors[i].GetParameters().Length;

            if (genericType == dictType && length >= 1)
            {
                Type pt = constructors[i].GetParameters()[0].ParameterType;

                if (pt.IsGenericType &&
                    pt.GetGenericTypeDefinition() == typeof(System.Collections.Generic.IEnumerable<>))
                {
                    continue;
                }
            }

            for (int j = 0; j < count + 1; j++)
            {
                var r = new _MethodBase(constructors[i], length - j);
                int index = ctorList.FindIndex((p) => { return CompareMethod(p, r) >= 0; });

                if (index >= 0)
                {
                    if (CompareMethod(ctorList[index], r) == 2)
                    {
                        ctorList.RemoveAt(index);
                        ctorList.Add(r);
                    }
                }
                else
                {
                    ctorList.Add(r);
                }
            }
        }
    }

    void GenerateConstructor()
    {
        if (ctorList.Count == 0)
        {
            if (type.IsValueType)
                DefaultConstruct();

            return;
        }

        ctorList.Sort(Compare);
        var checkTypeMap = CheckCheckTypePos(ctorList);
        sb.AppendLineEx("\r\t[MonoPInvokeCallback(typeof(LuaCSFunction))]");
        sb.AppendFormat("\tstatic int _Create{0}(IntPtr L)\r", wrapClassName);
        sb.AppendLineEx("\t{");

        BeginTry();
        sb.AppendLineEx("\t\t\tint count = LuaDLL.lua_gettop(L);");
        sb.AppendLineEx();

        var md = ctorList[0];
        bool hasEmptyCon = ctorList[0].GetParameters().Length == 0 ? true : false;

        //处理重载构造函数
        if (HasOptionalParam(md.GetParameters()))
        {
            var paramInfos = md.GetParameters();
            var param = paramInfos[paramInfos.Length - 1];
            string str = GetTypeStr(param.ParameterType.GetElementType());

            if (paramInfos.Length > 1)
            {
                string strParams = md.GenParamTypes(this, 1);
                sb.AppendFormat(
                    "\t\t\tif (TypeChecker.CheckTypes<{0}>(L, 1) && TypeChecker.CheckParamsType<{1}>(L, {2}, {3}))\r",
                    strParams, str, paramInfos.Length, GetCountStr(paramInfos.Length - 1));
            }
            else
            {
                sb.AppendFormat("\t\t\tif (TypeChecker.CheckParamsType<{0}>(L, {1}, {2}))\r", str, paramInfos.Length,
                    GetCountStr(paramInfos.Length - 1));
            }
        }
        else
        {
            var paramInfos = md.GetParameters();

            if (ctorList.Count == 1 || paramInfos.Length == 0 || paramInfos.Length + 1 <= checkTypeMap[0])
            {
                sb.AppendFormat("\t\t\tif (count == {0})\r", paramInfos.Length);
            }
            else
            {
                string strParams = md.GenParamTypes(this, checkTypeMap[0]);
                sb.AppendFormat("\t\t\tif (count == {0} && TypeChecker.CheckTypes<{1}>(L, {2}))\r", paramInfos.Length,
                    strParams, checkTypeMap[0]);
            }
        }

        sb.AppendLineEx("\t\t\t{");
        int rc = md.ProcessParams(this, 4, true, checkTypeMap[0] - 1);
        sb.AppendFormat("\t\t\t\treturn {0};\r", rc);
        sb.AppendLineEx("\t\t\t}");

        for (int i = 1; i < ctorList.Count; i++)
        {
            hasEmptyCon = ctorList[i].GetParameters().Length == 0 ? true : hasEmptyCon;
            md = ctorList[i];
            var paramInfos = md.GetParameters();

            if (!HasOptionalParam(md.GetParameters()))
            {
                string strParams = md.GenParamTypes(this, checkTypeMap[i]);

                if (paramInfos.Length + 1 > checkTypeMap[i])
                {
                    sb.AppendFormat("\t\t\telse if (count == {0} && TypeChecker.CheckTypes<{1}>(L, {2}))\r", paramInfos.Length, strParams, checkTypeMap[i]);
                }
                else
                {
                    sb.AppendFormat("\t\t\telse if (count == {0})\r", paramInfos.Length);
                }
            }
            else
            {
                var param = paramInfos[paramInfos.Length - 1];
                string str = GetTypeStr(param.ParameterType.GetElementType());

                if (paramInfos.Length > 1)
                {
                    string strParams = md.GenParamTypes(this, 1);
                    sb.AppendFormat(
                        "\t\t\telse if (TypeChecker.CheckTypes<{0}>(L, 1) && TypeChecker.CheckParamsType<{1}>(L, {2}, {3}))\r",
                        strParams, str, paramInfos.Length, GetCountStr(paramInfos.Length - 1));
                }
                else
                {
                    sb.AppendFormat("\t\t\telse if (TypeChecker.CheckParamsType<{0}>(L, {1}, {2}))\r", str,
                        paramInfos.Length, GetCountStr(paramInfos.Length - 1));
                }
            }

            sb.AppendLineEx("\t\t\t{");
            rc = md.ProcessParams(this, 4, true, checkTypeMap[i] - 1);
            sb.AppendFormat("\t\t\t\treturn {0};\r", rc);
            sb.AppendLineEx("\t\t\t}");
        }

        if (type.IsValueType && !hasEmptyCon)
        {
            sb.AppendLineEx("\t\t\telse if (count == 0)");
            sb.AppendLineEx("\t\t\t{");
            sb.AppendFormat("\t\t\t\tvar obj = new {0}();\r", className);
            GenPushStr(type, "obj", "\t\t\t\t");
            sb.AppendLineEx("\t\t\t\treturn 1;");
            sb.AppendLineEx("\t\t\t}");
        }

        sb.AppendLineEx("\t\t\telse");
        sb.AppendLineEx("\t\t\t{");
        sb.AppendFormat("\t\t\t\treturn LuaDLL.luaL_throw(L, \"invalid arguments to ctor method: {0}.New\");\r", className);
        sb.AppendLineEx("\t\t\t}");

        EndTry();
        sb.AppendLineEx("\t}");
    }

    //this[] 非静态函数
    void GenerateIndexer()
    {
        int flag = 0;

        if (getItems.Count > 0)
        {
            sb.AppendLineEx("\r\t[MonoPInvokeCallback(typeof(LuaCSFunction))]");
            sb.AppendLineEx("\tstatic int _get_this(IntPtr L)");
            sb.AppendLineEx("\t{");
            BeginTry();

            if (getItems.Count == 1)
            {
                var m = getItems[0];
                int count = m.GetParameters().Length + 1;
                sb.AppendFormat("\t\t\tToLua.CheckArgsCount(L, {0});\r", count);
                m.ProcessParams(this, 3, false, int.MaxValue);
                sb.AppendLineEx("\t\t\treturn 1;\r");
            }
            else
            {
                getItems.Sort(Compare);
                var checkTypeMap = CheckCheckTypePos(getItems);

                sb.AppendLineEx("\t\t\tint count = LuaDLL.lua_gettop(L);");
                sb.AppendLineEx();

                for (int i = 0; i < getItems.Count; i++)
                    GenOverrideFuncBody(getItems[i], i == 0, checkTypeMap[i]);

                sb.AppendLineEx("\t\t\telse");
                sb.AppendLineEx("\t\t\t{");
                sb.AppendFormat(
                    "\t\t\t\treturn LuaDLL.luaL_throw(L, \"invalid arguments to operator method: {0}.this\");\r",
                    className);
                sb.AppendLineEx("\t\t\t}");
            }

            EndTry();
            sb.AppendLineEx("\t}");
            flag |= 1;
        }

        if (setItems.Count > 0)
        {
            sb.AppendLineEx("\r\t[MonoPInvokeCallback(typeof(LuaCSFunction))]");
            sb.AppendLineEx("\tstatic int _set_this(IntPtr L)");
            sb.AppendLineEx("\t{");
            BeginTry();

            if (setItems.Count == 1)
            {
                var m = setItems[0];
                int count = m.GetParameters().Length + 1;
                sb.AppendFormat("\t\t\tToLua.CheckArgsCount(L, {0});\r", count);
                m.ProcessParams(this, 3, false, int.MaxValue);
                sb.AppendLineEx("\t\t\treturn 0;\r");
            }
            else
            {
                setItems.Sort(Compare);
                int[] checkTypeMap = CheckCheckTypePos(setItems);

                sb.AppendLineEx("\t\t\tint count = LuaDLL.lua_gettop(L);");
                sb.AppendLineEx();

                for (int i = 0; i < setItems.Count; i++)
                {
                    GenOverrideFuncBody(setItems[i], i == 0, checkTypeMap[i]);
                }

                sb.AppendLineEx("\t\t\telse");
                sb.AppendLineEx("\t\t\t{");
                sb.AppendFormat(
                    "\t\t\t\treturn LuaDLL.luaL_throw(L, \"invalid arguments to operator method: {0}.this\");\r",
                    className);
                sb.AppendLineEx("\t\t\t}");
            }


            EndTry();
            sb.AppendLineEx("\t}");
            flag |= 2;
        }

        if (flag != 0)
        {
            sb.AppendLineEx("\r\t[MonoPInvokeCallback(typeof(LuaCSFunction))]");
            sb.AppendLineEx("\tstatic int _this(IntPtr L)");
            sb.AppendLineEx("\t{");
            BeginTry();
            sb.AppendLineEx("\t\t\tLuaDLL.lua_pushvalue(L, 1);");
            sb.AppendFormat("\t\t\tLuaDLL.tolua_bindthis(L, {0}, {1});\r", (flag & 1) == 1 ? "_get_this" : "null",
                (flag & 2) == 2 ? "_set_this" : "null");
            sb.AppendLineEx("\t\t\treturn 1;");
            EndTry();
            sb.AppendLineEx("\t}");
        }
    }

    int GetOptionalParamPos(ParameterInfo[] infos)
    {
        for (int i = 0; i < infos.Length; i++)
        {
            if (IsParams(infos[i]))
            {
                return i;
            }
        }

        return -1;
    }

    bool Is64bit(Type t)
    {
        return t == typeof(long) || t == typeof(ulong);
    }

    int Compare(_MethodBase lhs, _MethodBase rhs)
    {
        int off1 = lhs.IsStatic ? 0 : 1;
        int off2 = rhs.IsStatic ? 0 : 1;

        var lp = lhs.GetParameters();
        var rp = rhs.GetParameters();

        int pos1 = GetOptionalParamPos(lp);
        int pos2 = GetOptionalParamPos(rp);

        if (pos1 >= 0 && pos2 < 0)
        {
            return 1;
        }
        else if (pos1 < 0 && pos2 >= 0)
        {
            return -1;
        }
        else if (pos1 >= 0 && pos2 >= 0)
        {
            pos1 += off1;
            pos2 += off2;

            if (pos1 != pos2)
            {
                return pos1 > pos2 ? -1 : 1;
            }
            else
            {
                pos1 -= off1;
                pos2 -= off2;

                if (lp[pos1].ParameterType.GetElementType() == typeof(object) &&
                    rp[pos2].ParameterType.GetElementType() != typeof(object))
                {
                    return 1;
                }
                else if (lp[pos1].ParameterType.GetElementType() != typeof(object) &&
                         rp[pos2].ParameterType.GetElementType() == typeof(object))
                {
                    return -1;
                }
            }
        }

        int c1 = off1 + lp.Length;
        int c2 = off2 + rp.Length;

        if (c1 > c2)
        {
            return 1;
        }
        else if (c1 == c2)
        {
            var list1 = new List<ParameterInfo>(lp);
            var list2 = new List<ParameterInfo>(rp);

            if (list1.Count > list2.Count)
            {
                if (list1[0].ParameterType == typeof(object))
                {
                    return 1;
                }
                else if (list1[0].ParameterType.IsPrimitive)
                {
                    return -1;
                }

                list1.RemoveAt(0);
            }
            else if (list2.Count > list1.Count)
            {
                if (list2[0].ParameterType == typeof(object))
                {
                    return -1;
                }
                else if (list2[0].ParameterType.IsPrimitive)
                {
                    return 1;
                }

                list2.RemoveAt(0);
            }

            for (int i = 0; i < list1.Count; i++)
            {
                if (list1[i].ParameterType == typeof(object) && list2[i].ParameterType != typeof(object))
                {
                    return 1;
                }
                else if (list1[i].ParameterType != typeof(object) && list2[i].ParameterType == typeof(object))
                {
                    return -1;
                }
                else if (list1[i].ParameterType.IsPrimitive && !list2[i].ParameterType.IsPrimitive)
                {
                    return -1;
                }
                else if (!list1[i].ParameterType.IsPrimitive && list2[i].ParameterType.IsPrimitive)
                {
                    return 1;
                }
                else if (list1[i].ParameterType.IsPrimitive && list2[i].ParameterType.IsPrimitive)
                {
                    if (Is64bit(list1[i].ParameterType) && !Is64bit(list2[i].ParameterType))
                    {
                        return 1;
                    }
                    else if (!Is64bit(list1[i].ParameterType) && Is64bit(list2[i].ParameterType))
                    {
                        return -1;
                    }
                    else if (Is64bit(list1[i].ParameterType) && Is64bit(list2[i].ParameterType) &&
                             list1[i].ParameterType != list2[i].ParameterType)
                    {
                        if (list1[i].ParameterType == typeof(ulong))
                        {
                            return 1;
                        }

                        return -1;
                    }
                }
            }

            return 0;
        }
        else
        {
            return -1;
        }
    }

    bool HasOptionalParam(ParameterInfo[] infos)
    {
        for (int i = 0; i < infos.Length; i++)
        {
            if (IsParams(infos[i]))
            {
                return true;
            }
        }

        return false;
    }

    void CheckObject(string head, Type type, string className, int pos)
    {
        if (type == typeof(object))
        {
            sb.AppendFormat("{0}object obj = ToLua.CheckObject(L, {1});\r", head, pos);
        }
        else if (type == typeof(Type))
        {
            sb.AppendFormat("{0}{1} obj = ToLua.CheckMonoType(L, {2});\r", head, className, pos);
        }
        else if (IsIEnumerator(type))
        {
            sb.AppendFormat("{0}{1} obj = ToLua.CheckIter(L, {2});\r", head, className, pos);
        }
        else
        {
            if (IsSealedType(type))
            {
                sb.AppendFormat("{0}var obj = ({1})ToLua.CheckObject(L, {2}, typeof({1}));\r", head, className, pos);
            }
            else
            {
                sb.AppendFormat("{0}var obj = ToLua.CheckObject<{1}>(L, {2});\r", head, className, pos);
            }
        }
    }

    void ToObject(string head, Type type, string className, int pos)
    {
        if (type == typeof(object))
        {
            sb.AppendFormat("{0}object obj = ToLua.ToObject(L, {1});\r", head, pos);
        }
        else
        {
            sb.AppendFormat("{0}var obj = ({1})ToLua.ToObject(L, {2});\r", head, className, pos);
        }
    }

    void BeginTry()
    {
        sb.AppendLineEx("\t\ttry");
        sb.AppendLineEx("\t\t{");
    }

    void EndTry()
    {
        sb.AppendLineEx("\t\t}");
        sb.AppendLineEx("\t\tcatch (Exception e)");
        sb.AppendLineEx("\t\t{");
        sb.AppendLineEx("\t\t\treturn LuaDLL.toluaL_exception(L, e);");
        sb.AppendLineEx("\t\t}");
    }

    void EndTry(string o, string msg)
    {
        sb.AppendLineEx("\t\t}");
        sb.AppendLineEx("\t\tcatch (Exception e)");
        sb.AppendLineEx("\t\t{");
        sb.AppendLineEx($"\t\t\treturn LuaDLL.toluaL_exception(L, e, {o}, {msg}");
        sb.AppendLineEx("\t\t}");
    }

    Type GetRefBaseType(Type argType)
    {
        if (argType.IsByRef)
        {
            return argType.GetElementType();
        }

        return argType;
    }

    void ProcessArg(Type varType, string indent, string arg, int stackPos, bool beCheckTypes = false,
        bool beParams = false, bool beOutArg = false)
    {
        varType = GetRefBaseType(varType);
        string typeName = GetTypeStr(varType);
        string checkStr = beCheckTypes ? "To" : "Check";

        if (beOutArg)
        {
            if (varType.IsValueType)
            {
                sb.AppendLineEx($"{indent}{typeName} {arg};");
            }
            else
            {
                sb.AppendLineEx($"{indent}{typeName} {arg} = null;");
            }
        }
        else if (varType == typeof(bool))
        {
            string chkstr = beCheckTypes ? "lua_toboolean" : "luaL_checkboolean";
            sb.AppendLineEx($"{indent}var {arg} = LuaDLL.{chkstr}(L, {stackPos});");
        }
        else if (varType == typeof(string))
        {
            sb.AppendLineEx($"{indent}string {arg} = ToLua.{checkStr}String(L, {stackPos});");
        }
        else if (varType == typeof(IntPtr))
        {
            sb.AppendLineEx($"{indent}{typeName} {arg} = ToLua.CheckIntPtr(L, {stackPos});");
        }
        else if (varType == typeof(long))
        {
            string chkstr = beCheckTypes ? "tolua_toint64" : "tolua_checkint64";
            sb.AppendLineEx($"{indent}{typeName} {arg} = LuaDLL.{chkstr}(L, {stackPos});");
        }
        else if (varType == typeof(ulong))
        {
            string chkstr = beCheckTypes ? "tolua_touint64" : "tolua_checkuint64";
            sb.AppendLineEx($"{indent}{typeName} {arg} = LuaDLL.{chkstr}(L, {stackPos});");
        }
        else if (varType.IsPrimitive || IsNumberEnum(varType))
        {
            string chkstr = beCheckTypes ? "lua_tonumber" : "luaL_checknumber";
            sb.AppendLineEx($"{indent}{typeName} {arg} = ({typeName})LuaDLL.{chkstr}(L, {stackPos});");
        }
        else if (varType == typeof(LuaFunction))
        {
            sb.AppendLineEx($"{indent}LuaFunction {arg} = ToLua.{checkStr}LuaFunction(L, {stackPos});");
        }
        else if (varType.IsSubclassOf(typeof(System.MulticastDelegate)))
        {
            if (beCheckTypes)
            {
                sb.AppendLineEx($"{indent}{typeName} {arg} = ({typeName})ToLua.ToObject(L, {stackPos});");
            }
            else
            {
                sb.AppendLineEx($"{indent}{typeName} {arg} = ({typeName})ToLua.CheckDelegate<{typeName}>(L, {stackPos});");
            }
        }
        else if (varType == typeof(LuaTable))
        {
            sb.AppendLineEx($"{indent}LuaTable {arg} = ToLua.{checkStr}LuaTable(L, {stackPos});");
        }
        else if (varType == typeof(Vector2))
        {
            sb.AppendLineEx($"{indent}UnityEngine.Vector2 {arg} = ToLua.ToVector2(L, {stackPos});");
        }
        else if (varType == typeof(Vector3))
        {
            sb.AppendLineEx($"{indent}UnityEngine.Vector3 {arg} = ToLua.ToVector3(L, {stackPos});");
        }
        else if (varType == typeof(Vector4))
        {
            sb.AppendLineEx($"{indent}UnityEngine.Vector4 {arg} = ToLua.ToVector4(L, {stackPos});");
        }
        else if (varType == typeof(Quaternion))
        {
            sb.AppendLineEx($"{indent}UnityEngine.Quaternion {arg} = ToLua.ToQuaternion(L, {stackPos});");
        }
        else if (varType == typeof(Color))
        {
            sb.AppendLineEx($"{indent}UnityEngine.Color {arg} = ToLua.ToColor(L, {stackPos});");
        }
        else if (varType == typeof(Ray))
        {
            sb.AppendLineEx($"{indent}UnityEngine.Ray {arg} = ToLua.ToRay(L, {stackPos});");
        }
        else if (varType == typeof(Bounds))
        {
            sb.AppendLineEx($"{indent}UnityEngine.Bounds {arg} = ToLua.ToBounds(L, {stackPos});");
        }
        else if (varType == typeof(LayerMask))
        {
            sb.AppendLineEx($"{indent}UnityEngine.LayerMask {arg} = ToLua.ToLayerMask(L, {stackPos});");
        }
        else if (varType == typeof(object))
        {
            sb.AppendLineEx($"{indent}object {arg} = ToLua.ToVarObject(L, {stackPos});");
        }
        else if (varType == typeof(LuaByteBuffer))
        {
            sb.AppendLineEx($"{indent}LuaByteBuffer {arg} = new LuaByteBuffer(ToLua.CheckByteBuffer(L, {stackPos}));");
        }
        else if (varType == typeof(Type))
        {
            if (beCheckTypes)
            {
                sb.AppendLineEx($"{indent}System.Type {arg} = (System.Type)ToLua.ToObject(L, {stackPos});");
            }
            else
            {
                sb.AppendLineEx($"{indent}System.Type {arg} = ToLua.CheckMonoType(L, {stackPos});");
            }
        }
        else if (IsIEnumerator(varType))
        {
            if (beCheckTypes)
            {
                sb.AppendLineEx($"{indent}System.Collections.IEnumerator {arg} = (System.Collections.IEnumerator)ToLua.ToObject(L, {stackPos});");
            }
            else
            {
                sb.AppendLineEx($"{0}System.Collections.IEnumerator {arg} = ToLua.CheckIter(L, {stackPos});");
            }
        }
        else if (varType.IsArray && varType.GetArrayRank() == 1)
        {
            Type et = varType.GetElementType();
            string atstr = GetTypeStr(et);
            string fname;
            bool flag = false; //是否模版函数
            bool isObject = false;

            if (et.IsPrimitive)
            {
                if (beParams)
                {
                    if (et == typeof(bool))
                    {
                        fname = beCheckTypes ? "ToParamsBool" : "CheckParamsBool";
                    }
                    else if (et == typeof(char))
                    {
                        //char用的多些，特殊处理一下减少gcalloc
                        fname = beCheckTypes ? "ToParamsChar" : "CheckParamsChar";
                    }
                    else
                    {
                        flag = true;
                        fname = beCheckTypes ? "ToParamsNumber" : "CheckParamsNumber";
                    }
                }
                else if (et == typeof(char))
                {
                    fname = "CheckCharBuffer";
                }
                else if (et == typeof(byte))
                {
                    fname = "CheckByteBuffer";
                }
                else if (et == typeof(bool))
                {
                    fname = "CheckBoolArray";
                }
                else
                {
                    fname = beCheckTypes ? "ToNumberArray" : "CheckNumberArray";
                    flag = true;
                }
            }
            else if (et == typeof(string))
            {
                if (beParams)
                {
                    fname = beCheckTypes ? "ToParamsString" : "CheckParamsString";
                }
                else
                {
                    fname = beCheckTypes ? "ToStringArray" : "CheckStringArray";
                }
            }
            else //if (et == typeof(object))
            {
                flag = true;

                if (et == typeof(object))
                {
                    isObject = true;
                    flag = false;
                }

                if (beParams)
                {
                    fname = (isObject || beCheckTypes) ? "ToParamsObject" : "CheckParamsObject";
                }
                else
                {
                    if (et.IsValueType)
                    {
                        fname = beCheckTypes ? "ToStructArray" : "CheckStructArray";
                    }
                    else
                    {
                        fname = beCheckTypes ? "ToObjectArray" : "CheckObjectArray";
                    }
                }

                if (et == typeof(UnityEngine.Object))
                {
                    ambig |= ObjAmbig.U3dObj;
                }
            }

            if (flag)
            {
                if (beParams)
                {
                    if (isObject)
                    {
                        sb.AppendLineEx($"{indent}var {arg} = ToLua.{fname}(L, {stackPos}, {GetCountStr(stackPos - 1)});");
                    }
                    else
                    {
                        sb.AppendLineEx($"{indent}var {arg} = ToLua.{fname}<{atstr}>(L, {stackPos}, {GetCountStr(stackPos - 1)});");
                    }
                }
                else
                {
                    sb.AppendLineEx($"{indent}var {arg} = ToLua.{fname}<{atstr}>(L, {stackPos});");
                }
            }
            else
            {
                if (beParams)
                {
                    sb.AppendLineEx($"{indent}{atstr}[] {arg} = ToLua.{fname}(L, {stackPos}, {GetCountStr(stackPos - 1)});");
                }
                else
                {
                    sb.AppendLineEx($"{indent}{atstr}[] {arg} = ToLua.{fname}(L, {stackPos});");
                }
            }
        }
        else if (varType.IsGenericType && varType.GetGenericTypeDefinition() == typeof(Nullable<>))
        {
            var t = TypeChecker.GetNullableType(varType);

            sb.AppendLineEx($"{indent}var {arg} = ToLua.{(beCheckTypes ? "ToNullable" : "CheckNullable")}<{GetTypeStr(t)}>(L, {stackPos});");
        }
        else if (varType.IsValueType && !varType.IsEnum)
        {
            sb.AppendLineEx($"{indent}var {arg} = StackTraits<{typeName}>.{(beCheckTypes ? "To" : "Check")}(L, {stackPos});");
        }
        else //从object派生但不是object
        {
            if (beCheckTypes)
            {
                sb.AppendLineEx($"{indent}var {arg} = ({typeName})ToLua.ToObject(L, {stackPos});");
            }
            else
            {
                if (IsSealedType(varType))
                {
                    sb.AppendLineEx($"{indent}var {arg} = ({typeName})ToLua.CheckObject(L, {stackPos}, typeof({typeName}));");
                }
                else
                {
                    sb.AppendLineEx($"{indent}var {arg} = ToLua.CheckObject<{typeName}>(L, {stackPos});");
                }
            }
        }
    }

    int GetMethodType(MethodBase md, out PropertyInfo pi)
    {
        pi = null;

        if (!md.IsSpecialName)
        {
            return 0; // normal method
        }

        int methodType = 0;
        int pos = allProps.FindIndex((p) => { return p.GetGetMethod() == md || p.GetSetMethod() == md; });

        if (pos >= 0)
        {
            methodType = 1; // properties
            pi = allProps[pos];

            if (md == pi.GetGetMethod())
            {
                if (md.GetParameters().Length > 0)
                {
                    methodType = 2; // get method
                }
            }
            else if (md == pi.GetSetMethod())
            {
                if (md.GetParameters().Length > 1)
                {
                    methodType = 2; // set method
                }
            }
        }

        return methodType;
    }

    private static Type GetConstraintParameterType(Type[] types, Type genericType)
    {
        for (int index = 0, count = types.Length; index < count; ++index)
        {
            var type = types[index];
            if (type.IsInterface)
                return type;

            if (type.IsClass)
                return type;

            if (type.IsEnum)
                return type;
        }

        return genericType;
    }

    public static Type GetGenericParameterType(MethodInfo methodInfo, Type parameterType)
    {
        if (parameterType.IsGenericParameter)
        {
            var genericParameters = methodInfo.GetGenericArguments();
            var genericParameter = genericParameters[parameterType.GenericParameterPosition];

            var constraints = genericParameter.GetGenericParameterConstraints();

            return GetConstraintParameterType(constraints, parameterType);
        }
        return parameterType;
    }

    static Type GetGenericBaseType(MethodBase methodBase, Type parameterType)
    {
        if (!methodBase.IsGenericMethod)
            return parameterType;

        return GetGenericParameterType(methodBase as MethodInfo, parameterType);
    }

    bool IsNumberEnum(Type t)
    {
        if (t == typeof(BindingFlags))
        {
            return true;
        }

        return false;
    }

    void GenPushStr(Type t, string arg, string head, bool isByteBuffer = false)
    {
        if (t == typeof(int))
        {
            sb.AppendFormat("{0}LuaDLL.lua_pushinteger(L, {1});\r", head, arg);
        }
        else if (t == typeof(bool))
        {
            sb.AppendFormat("{0}LuaDLL.lua_pushboolean(L, {1});\r", head, arg);
        }
        else if (t == typeof(string))
        {
            sb.AppendFormat("{0}LuaDLL.lua_pushstring(L, {1});\r", head, arg);
        }
        else if (t == typeof(IntPtr))
        {
            sb.AppendFormat("{0}LuaDLL.lua_pushlightuserdata(L, {1});\r", head, arg);
        }
        else if (t == typeof(long))
        {
            sb.AppendFormat("{0}LuaDLL.tolua_pushint64(L, {1});\r", head, arg);
        }
        else if (t == typeof(ulong))
        {
            sb.AppendFormat("{0}LuaDLL.tolua_pushuint64(L, {1});\r", head, arg);
        }
        else if ((t.IsPrimitive))
        {
            sb.AppendFormat("{0}LuaDLL.lua_pushnumber(L, {1});\r", head, arg);
        }
        else
        {
            if (isByteBuffer && t == typeof(byte[]))
            {
                sb.AppendFormat("{0}LuaDLL.tolua_pushlstring(L, {1}, {1}.Length);\r", head, arg);
            }
            else
            {
                string str = GetPushFunction(t);
                sb.AppendFormat("{0}ToLua.{1}(L, {2});\r", head, str, arg);
            }
        }
    }

    bool CompareParmsCount(_MethodBase l, _MethodBase r)
    {
        if (l == r)
        {
            return false;
        }

        int c1 = l.IsStatic ? 0 : 1;
        int c2 = r.IsStatic ? 0 : 1;

        c1 += l.GetParameters().Length;
        c2 += r.GetParameters().Length;

        return c1 == c2;
    }

    //decimal 类型扔掉了
    Dictionary<Type, int> typeSize = new Dictionary<Type, int>()
    {
        { typeof(char), 2 },
        { typeof(byte), 3 },
        { typeof(sbyte), 4 },
        { typeof(ushort), 5 },
        { typeof(short), 6 },
        { typeof(uint), 7 },
        { typeof(int), 8 },
        //{ typeof(ulong), 9 },
        //{ typeof(long), 10 },
        { typeof(decimal), 11 },
        { typeof(float), 12 },
        { typeof(double), 13 },
    };

    //-1 不存在替换, 1 保留左面， 2 保留右面
    int CompareMethod(_MethodBase l, _MethodBase r)
    {
        int s = 0;

        if (!CompareParmsCount(l, r))
        {
            return -1;
        }
        else
        {
            var lp = l.GetParameters();
            var rp = r.GetParameters();

            var ll = new List<Type>();
            var lr = new List<Type>();

            if (!l.IsStatic)
            {
                ll.Add(type);
            }

            if (!r.IsStatic)
            {
                lr.Add(type);
            }

            for (int i = 0; i < lp.Length; i++)
            {
                ll.Add(lp[i].ParameterType);
            }

            for (int i = 0; i < rp.Length; i++)
            {
                lr.Add(rp[i].ParameterType);
            }

            for (int i = 0; i < ll.Count; i++)
            {
                if (!typeSize.ContainsKey(ll[i]) || !typeSize.ContainsKey(lr[i]))
                {
                    if (ll[i] == lr[i])
                    {
                        continue;
                    }
                    else
                    {
                        return -1;
                    }
                }
                else if (ll[i].IsPrimitive && lr[i].IsPrimitive && s == 0)
                {
                    s = typeSize[ll[i]] >= typeSize[lr[i]] ? 1 : 2;
                }
                else if (ll[i] != lr[i] && !ll[i].IsPrimitive && !lr[i].IsPrimitive)
                {
                    return -1;
                }
            }

            if (s == 0 && l.IsStatic)
            {
                s = 2;
            }
        }

        return s;
    }

    void Push(List<_MethodBase> list, _MethodBase r)
    {
        string name = GetMethodName(r.Method);
        int index = list.FindIndex((p) => { return GetMethodName(p.Method) == name && CompareMethod(p, r) >= 0; });

        if (index >= 0)
        {
            if (CompareMethod(list[index], r) == 2)
            {
                logs.Add($"{className}.{list[index].GetTotalName()} has been dropped as function {r.GetTotalName()} more match lua");
                list.RemoveAt(index);
                list.Add(r);
                return;
            }
            else
            {
                logs.Add($"{className}.{r.GetTotalName()} has been dropped as function {list[index].GetTotalName()} more match lua");
                return;
            }
        }

        list.Add(r);
    }

    void GenOverrideFuncBody(_MethodBase md, bool beIf, int checkTypeOffset)
    {
        int offset = md.IsStatic ? 0 : 1;
        int ret = md.GetReturnType() == typeof(void) ? 0 : 1;
        string strIf = beIf ? "if " : "else if ";

        if (HasOptionalParam(md.GetParameters()))
        {
            var paramInfos = md.GetParameters();
            var param = paramInfos[paramInfos.Length - 1];
            string str = GetTypeStr(param.ParameterType.GetElementType());

            if (paramInfos.Length + offset > 1)
            {
                string strParams = md.GenParamTypes(this, 0);
                sb.AppendFormat(
                    "\t\t\t{0}(TypeChecker.CheckTypes<{1}>(L, 1) && TypeChecker.CheckParamsType<{2}>(L, {3}, {4}))\r",
                    strIf, strParams, str, paramInfos.Length + offset, GetCountStr(paramInfos.Length + offset - 1));
            }
            else
            {
                sb.AppendFormat("\t\t\t{0}(TypeChecker.CheckParamsType<{1}>(L, {2}, {3}))\r", strIf, str,
                    paramInfos.Length + offset, GetCountStr(paramInfos.Length + offset - 1));
            }
        }
        else
        {
            var paramInfos = md.GetParameters();

            if (paramInfos.Length + offset > checkTypeOffset)
            {
                string strParams = md.GenParamTypes(this, checkTypeOffset);
                sb.AppendFormat("\t\t\t{0}(count == {1} && TypeChecker.CheckTypes<{2}>(L, {3}))\r", strIf,
                    paramInfos.Length + offset, strParams, checkTypeOffset + 1);
            }
            else
            {
                sb.AppendFormat("\t\t\t{0}(count == {1})\r", strIf, paramInfos.Length + offset);
            }
        }

        sb.AppendLineEx("\t\t\t{");
        int count = md.ProcessParams(this, 4, false, checkTypeOffset);
        sb.AppendFormat("\t\t\t\treturn {0};\r", ret + count);
        sb.AppendLineEx("\t\t\t}");
    }

    int[] CheckCheckTypePos<T>(List<T> list) where T : _MethodBase
    {
        var map = new int[list.Count];

        for (int i = 0; i < list.Count;)
        {
            if (HasOptionalParam(list[i].GetParameters()))
            {
                if (list[0].IsConstructor)
                {
                    for (int k = 0; k < map.Length; k++)
                    {
                        map[k] = 1;
                    }
                }
                else
                {
                    Array.Clear(map, 0, map.Length);
                }

                return map;
            }

            int c1 = list[i].GetParamsCount();
            int count = c1;
            map[i] = count;
            int j = i + 1;

            for (; j < list.Count; j++)
            {
                int c2 = list[j].GetParamsCount();

                if (c1 == c2)
                {
                    count = Mathf.Min(count, list[i].GetEqualParamsCount(this, list[j]));
                }
                else
                {
                    map[j] = c2;
                    break;
                }

                for (int m = i; m <= j; m++)
                {
                    map[m] = count;
                }
            }

            i = j;
        }

        return map;
    }

    _MethodBase GenOverrideFunc(string name)
    {
        var methodBases = new List<_MethodBase>();

        for (int i = 0; i < methods.Count; i++)
        {
            string curName = GetMethodName(methods[i].Method);

            if (curName == name && !IsGenericMethod(methods[i].Method))
            {
                Push(methodBases, methods[i]);
            }
        }

        if (methodBases.Count == 1)
            return methodBases[0];
        else if (methodBases.Count == 0)
            return null;

        var fieldPlatformFlags = ReflectFields.GetPlatformFlags(methodBases[0].FullName);
        if (fieldPlatformFlags == ToLuaPlatformFlags.None)
            return null;

        methodBases.Sort(Compare);

        var checkTypeMap = CheckCheckTypePos(methodBases);

        var fieldPlatformFlagsText = ToLuaPlatformUtility.GetText(fieldPlatformFlags);

        BeginPlatformMacro(fieldPlatformFlagsText);

        sb.AppendLineEx("\r\t[MonoPInvokeCallback(typeof(LuaCSFunction))]");
        sb.AppendFormat("\tstatic int {0}(IntPtr L)\r", GetRegisterFunctionName(name));
        sb.AppendLineEx("\t{");

        BeginTry();
        sb.AppendLineEx("\t\t\tint count = LuaDLL.lua_gettop(L);");
        sb.AppendLineEx();

        for (int i = 0; i < methodBases.Count; i++)
        {
            var methodBase = methodBases[i];

            GenOverrideFuncBody(methodBase, i == 0, checkTypeMap[i]);
        }

        sb.AppendLineEx("\t\t\telse");
        sb.AppendLineEx("\t\t\t{");
        sb.AppendFormat("\t\t\t\treturn LuaDLL.luaL_throw(L, \"invalid arguments to method: {0}.{1}\");\r", className,
            name);
        sb.AppendLineEx("\t\t\t}");

        EndTry();
        sb.AppendLineEx("\t}");

        EndPlatformMacro(fieldPlatformFlagsText);

        return null;
    }

    public static string CombineTypeStr(string space, string name)
    {
        return string.IsNullOrEmpty(space) ? name : space + "." + name;
    }

    public static string GetBaseTypeStr(Type t)
    {
        return t.IsGenericType ? LuaMisc.GetTypeName(t) : ToLuaTypes.GetName(t);
    }

    //获取类型名字
    public static string GetTypeStr(Type t)
    {
        if (t.IsByRef)
        {
            t = t.GetElementType();
            return GetTypeStr(t);
        }

        if (t.IsArray)
        {
            string str = GetTypeStr(t.GetElementType());
            str += LuaMisc.GetArrayRank(t);
            return str;
        }

        if (IsIEnumerator(t))
            return LuaMisc.GetTypeName(typeof(IEnumerator));

        return LuaMisc.GetTypeName(t);
    }

    public string GetTypeFullName(Type type)
    {
        var typeName = GetTypeStr(type);
        return ConvertToLibSign(typeName);
    }

    //获取 typeof(string) 这样的名字
    string GetTypeOf(Type t, string sep)
    {
        string str;

        if (t.IsByRef)
            t = t.GetElementType();

        if (IsNumberEnum(t))
            str = string.Format("uint{0}", sep);
        else if (IsIEnumerator(t))
            str = string.Format("{0}{1}", GetTypeStr(typeof(IEnumerator)), sep);
        else
            str = string.Format("{0}{1}", GetTypeStr(t), sep);

        return str;
    }

    string GenParamTypes(ParameterInfo[] p, MethodBase mb, int offset = 0)
    {
        var sb = new StringBuilder();
        var list = new List<Type>();

        if (!mb.IsStatic)
        {
            list.Add(type);
        }

        for (int i = 0; i < p.Length; i++)
        {
            if (IsParams(p[i]))
            {
                continue;
            }

            if (p[i].ParameterType.IsByRef && (p[i].Attributes & ParameterAttributes.Out) != ParameterAttributes.None)
            {
                Type genericClass = typeof(LuaOut<>);
                Type t = genericClass.MakeGenericType(p[i].ParameterType);
                list.Add(t);
            }
            else
            {
                list.Add(GetGenericBaseType(mb, p[i].ParameterType));
            }
        }

        for (int i = offset; i < list.Count - 1; i++)
        {
            sb.Append(GetTypeOf(list[i], ", "));
        }

        if (list.Count > 0)
        {
            sb.Append(GetTypeOf(list[list.Count - 1], ""));
        }

        return sb.ToString();
    }

    void CheckObjectNull()
    {
        if (type.IsValueType)
        {
            sb.AppendLineEx("\t\t\tif (o == null)");
        }
        else
        {
            sb.AppendLineEx("\t\t\tif (obj == null)");
        }
    }

    void GenGetFieldStr(MemberInfo memberInfo, Type varType, bool isStatic, bool isByteBuffer, bool beOverride = false)
    {
        var fieldPlatformFlags = ReflectFields.GetPlatformFlags(memberInfo);
        if (fieldPlatformFlags == ToLuaPlatformFlags.None)
            return;

        var fieldPlatformFlagsText = ToLuaPlatformUtility.GetText(fieldPlatformFlags);

        BeginPlatformMacro(fieldPlatformFlagsText);

        var fieldName = memberInfo.Name;

        sb.AppendLineEx("\r\t[MonoPInvokeCallback(typeof(LuaCSFunction))]");
        sb.AppendFormat("\tstatic int {0}_{1}(IntPtr L)\r", beOverride ? "_get" : "get", fieldName);
        sb.AppendLineEx("\t{");

        if (isStatic)
        {
            var arg = $"{className}.{fieldName}";
            BeginTry();
            GenPushStr(varType, arg, "\t\t\t", isByteBuffer);
            sb.AppendLineEx("\t\t\treturn 1;");
            EndTry();
        }
        else
        {
            sb.AppendLineEx("\t\tobject o = null;\r");
            BeginTry();
            sb.AppendLineEx("\t\t\to = ToLua.ToObject(L, 1);");
            sb.AppendFormat("\t\t\tvar obj = ({0})o;\r", className);
            sb.AppendFormat("\t\t\t{0} ret = obj.{1};\r", GetTypeStr(varType), fieldName);
            GenPushStr(varType, "ret", "\t\t\t", isByteBuffer);
            sb.AppendLineEx("\t\t\treturn 1;");

            sb.AppendLineEx("\t\t}");
            sb.AppendLineEx("\t\tcatch(Exception e)");
            sb.AppendLineEx("\t\t{");

            sb.AppendFormat(
                "\t\t\treturn LuaDLL.toluaL_exception(L, e, o, \"attempt to index {0} on a nil value\");\r", fieldName);
            sb.AppendLineEx("\t\t}");
        }

        sb.AppendLineEx("\t}");

        EndPlatformMacro(fieldPlatformFlagsText);
    }

    void GenGetEventStr(MemberInfo memberInfo, Type varType)
    {
        var fieldPlatformFlags = ReflectFields.GetPlatformFlags(memberInfo);
        if (fieldPlatformFlags == ToLuaPlatformFlags.None)
            return;

        var fieldPlatformFlagsText = ToLuaPlatformUtility.GetText(fieldPlatformFlags);

        BeginPlatformMacro(fieldPlatformFlagsText);

        var fieldName = memberInfo.Name;

        sb.AppendLineEx("\r\t[MonoPInvokeCallback(typeof(LuaCSFunction))]");
        sb.AppendFormat("\tstatic int get_{0}(IntPtr L)\r", fieldName);
        sb.AppendLineEx("\t{");
        sb.AppendFormat("\t\tToLua.Push(L, new EventObject(typeof({0})));\r", GetTypeStr(varType));
        sb.AppendLineEx("\t\treturn 1;");
        sb.AppendLineEx("\t}");

        EndPlatformMacro(fieldPlatformFlagsText);
    }

    void GenIndexFunc()
    {
        for (int i = 0; i < fields.Length; i++)
        {
            var field = fields[i];

            if (field.IsLiteral && field.FieldType.IsPrimitive && !field.FieldType.IsEnum)
                continue;

            var beBuffer = IsByteBuffer(field);
            GenGetFieldStr(field, field.FieldType, field.IsStatic, beBuffer);
        }

        for (int i = 0; i < props.Length; i++)
        {
            var prop = props[i];

            if (!CanRead(prop))
                continue;

            var isStatic = propList.IndexOf(prop) < 0;

            var md = methods.Find((p) => p.Name == "get_" + prop.Name);
            var beBuffer = IsByteBuffer(prop);

            GenGetFieldStr(prop, prop.PropertyType, isStatic, beBuffer, md != null);
        }

        for (int i = 0; i < events.Length; i++)
        {
            var eventInfo = events[i];

            GenGetEventStr(eventInfo, eventInfo.EventHandlerType);
        }
    }

    void GenSetFieldStr(MemberInfo memberInfo, Type varType, bool isStatic, bool beOverride = false)
    {
        var fieldPlatformFlags = ReflectFields.GetPlatformFlags(memberInfo);
        if (fieldPlatformFlags == ToLuaPlatformFlags.None)
            return;

        var fieldPlatformFlagsText = ToLuaPlatformUtility.GetText(fieldPlatformFlags);

        BeginPlatformMacro(fieldPlatformFlagsText);

        var fieldName = memberInfo.Name;

        sb.AppendLineEx("\r\t[MonoPInvokeCallback(typeof(LuaCSFunction))]");
        sb.AppendFormat("\tstatic int {0}_{1}(IntPtr L)\r", beOverride ? "_set" : "set", fieldName);
        sb.AppendLineEx("\t{");

        if (!isStatic)
        {
            sb.AppendLineEx("\t\tobject o = null;\r");
            BeginTry();
            sb.AppendLineEx("\t\t\to = ToLua.ToObject(L, 1);");
            sb.AppendFormat("\t\t\t{0} obj = ({0})o;\r", className);
            ProcessArg(varType, "\t\t\t", "arg0", 2);
            sb.AppendFormat("\t\t\tobj.{0} = arg0;\r", fieldName);

            if (type.IsValueType)
            {
                sb.AppendLineEx("\t\t\tToLua.SetBack(L, 1, obj);");
            }

            sb.AppendLineEx("\t\t\treturn 0;");
            EndTry("o", $"\"attempt to index {fieldName} on a nil value\");\r");
        }
        else
        {
            BeginTry();
            ProcessArg(varType, "\t\t\t", "arg0", 2);
            sb.AppendFormat("\t\t\t{0}.{1} = arg0;\r", className, fieldName);
            sb.AppendLineEx("\t\t\treturn 0;");
            EndTry();
        }

        sb.AppendLineEx("\t}");

        EndPlatformMacro(fieldPlatformFlagsText);
    }

    void GenSetEventStr(MemberInfo memberInfo, Type varType, bool isStatic)
    {
        var fieldPlatformFlags = ReflectFields.GetPlatformFlags(memberInfo);
        if (fieldPlatformFlags == ToLuaPlatformFlags.None)
            return;

        var fieldPlatformFlagsText = ToLuaPlatformUtility.GetText(fieldPlatformFlags);

        BeginPlatformMacro(fieldPlatformFlagsText);

        var fieldName = memberInfo.Name;

        sb.AppendLineEx("\r\t[MonoPInvokeCallback(typeof(LuaCSFunction))]");
        sb.AppendFormat("\tstatic int set_{0}(IntPtr L)\r", fieldName);
        sb.AppendLineEx("\t{");
        BeginTry();

        if (!isStatic)
            sb.AppendFormat("\t\t\t{0} obj = ({0})ToLua.CheckObject(L, 1, typeof({0}));\r", className);

        string strVarType = GetTypeStr(varType);
        string objStr = isStatic ? className : "obj";

        sb.AppendLineEx("\t\t\tEventObject arg0 = null;\r");
        sb.AppendLineEx("\t\t\tif (LuaDLL.lua_isuserdata(L, 2) != 0)");
        sb.AppendLineEx("\t\t\t{");
        sb.AppendLineEx("\t\t\t\targ0 = (EventObject)ToLua.ToObject(L, 2);");
        sb.AppendLineEx("\t\t\t}");
        sb.AppendLineEx("\t\t\telse");
        sb.AppendLineEx("\t\t\t{");
        sb.AppendFormat(
            "\t\t\t\treturn LuaDLL.luaL_throw(L, \"The event '{0}.{1}' can only appear on the left hand side of += or -= when used outside of the type '{0}'\");\r",
            className, fieldName);
        sb.AppendLineEx("\t\t\t}\r");

        sb.AppendLineEx("\t\t\tif (arg0.op == EventOp.Add)");
        sb.AppendLineEx("\t\t\t{");
        sb.AppendFormat("\t\t\t\tvar ev = ({0})arg0.func;\r", strVarType);
        sb.AppendFormat("\t\t\t\t{0}.{1} += ev;\r", objStr, fieldName);
        sb.AppendLineEx("\t\t\t}");
        sb.AppendLineEx("\t\t\telse if (arg0.op == EventOp.Sub)");
        sb.AppendLineEx("\t\t\t{");
        sb.AppendFormat("\t\t\t\tvar ev = ({0})arg0.func;\r", strVarType);
        sb.AppendFormat("\t\t\t\t{0}.{1} -= ev;\r", objStr, fieldName);
        sb.AppendLineEx("\t\t\t}\r");

        sb.AppendLineEx("\t\t\treturn 0;");
        EndTry();

        sb.AppendLineEx("\t}");

        EndPlatformMacro(fieldPlatformFlagsText);
    }

    void GenNewIndexFunc()
    {
        for (int i = 0; i < fields.Length; i++)
        {
            var field = fields[i];

            if (field.IsLiteral || field.IsInitOnly || field.IsPrivate)
                continue;

            GenSetFieldStr(field, field.FieldType, field.IsStatic);
        }

        for (int i = 0; i < props.Length; i++)
        {
            var prop = props[i];

            if (!CanWrite(prop))
                continue;

            bool isStatic = propList.IndexOf(prop) < 0;

            var md = methods.Find((p) => { return p.Name == "set_" + prop.Name; });
            GenSetFieldStr(prop, prop.PropertyType, isStatic, md != null);
        }

        for (int i = 0; i < events.Length; i++)
        {
            var eventInfo = events[i];

            bool isStatic = eventList.IndexOf(eventInfo) < 0;

            GenSetEventStr(eventInfo, eventInfo.EventHandlerType, isStatic);
        }
    }

    void GenLuaFunctionRetValue(StringBuilder sb, Type t, string head, string name, bool beDefined = false)
    {
        if (t == typeof(bool))
        {
            name = beDefined ? name : "bool " + name;
            sb.AppendFormat("{0}{1} = func.CheckBoolean();\r", head, name);
        }
        else if (t == typeof(long))
        {
            name = beDefined ? name : "long " + name;
            sb.AppendFormat("{0}{1} = func.CheckLong();\r", head, name);
        }
        else if (t == typeof(ulong))
        {
            name = beDefined ? name : "ulong " + name;
            sb.AppendFormat("{0}{1} = func.CheckULong();\r", head, name);
        }
        else if (t.IsPrimitive || IsNumberEnum(t))
        {
            var type = GetTypeStr(t);
            name = beDefined ? name : type + " " + name;
            sb.AppendFormat("{0}{1} = ({2})func.CheckNumber();\r", head, name, type);
        }
        else if (t == typeof(string))
        {
            name = beDefined ? name : "string " + name;
            sb.AppendFormat("{0}{1} = func.CheckString();\r", head, name);
        }
        else if (typeof(System.MulticastDelegate).IsAssignableFrom(t))
        {
            name = beDefined ? name : GetTypeStr(t) + " " + name;
            sb.AppendFormat("{0}{1} = func.CheckDelegate();\r", head, name);
        }
        else if (t == typeof(Vector3))
        {
            name = beDefined ? name : "UnityEngine.Vector3 " + name;
            sb.AppendFormat("{0}{1} = func.CheckVector3();\r", head, name);
        }
        else if (t == typeof(Quaternion))
        {
            name = beDefined ? name : "UnityEngine.Quaternion " + name;
            sb.AppendFormat("{0}{1} = func.CheckQuaternion();\r", head, name);
        }
        else if (t == typeof(Vector2))
        {
            name = beDefined ? name : "UnityEngine.Vector2 " + name;
            sb.AppendFormat("{0}{1} = func.CheckVector2();\r", head, name);
        }
        else if (t == typeof(Vector4))
        {
            name = beDefined ? name : "UnityEngine.Vector4 " + name;
            sb.AppendFormat("{0}{1} = func.CheckVector4();\r", head, name);
        }
        else if (t == typeof(Color))
        {
            name = beDefined ? name : "UnityEngine.Color " + name;
            sb.AppendFormat("{0}{1} = func.CheckColor();\r", head, name);
        }
        else if (t == typeof(Ray))
        {
            name = beDefined ? name : "UnityEngine.Ray " + name;
            sb.AppendFormat("{0}{1} = func.CheckRay();\r", head, name);
        }
        else if (t == typeof(Bounds))
        {
            name = beDefined ? name : "UnityEngine.Bounds " + name;
            sb.AppendFormat("{0}{1} = func.CheckBounds();\r", head, name);
        }
        else if (t == typeof(LayerMask))
        {
            name = beDefined ? name : "UnityEngine.LayerMask " + name;
            sb.AppendFormat("{0}{1} = func.CheckLayerMask();\r", head, name);
        }
        else if (t == typeof(object))
        {
            name = beDefined ? name : "object " + name;
            sb.AppendFormat("{0}{1} = func.CheckVariant();\r", head, name);
        }
        else if (t == typeof(byte[]))
        {
            name = beDefined ? name : "byte[] " + name;
            sb.AppendFormat("{0}{1} = func.CheckByteBuffer();\r", head, name);
        }
        else if (t == typeof(char[]))
        {
            name = beDefined ? name : "char[] " + name;
            sb.AppendFormat("{0}{1} = func.CheckCharBuffer();\r", head, name);
        }
        else
        {
            var type = GetTypeStr(t);
            name = beDefined ? name : type + " " + name;
            sb.AppendFormat("{0}{1} = ({2})func.CheckObject(typeof({2}));\r", head, name, type);
        }
    }

    public bool IsByteBuffer(Type type)
    {
        var attrs = type.GetCustomAttributes(true);

        for (int j = 0; j < attrs.Length; j++)
        {
            Type t = attrs[j].GetType();

            if (t == typeof(LuaByteBufferAttribute))
            {
                return true;
            }
        }

        return false;
    }

    public bool IsByteBuffer(MemberInfo mb)
    {
        var attrs = mb.GetCustomAttributes(true);

        for (int j = 0; j < attrs.Length; j++)
        {
            Type t = attrs[j].GetType();

            if (t == typeof(LuaByteBufferAttribute))
            {
                return true;
            }
        }

        return false;
    }

    void GenDelegateBody(StringBuilder sb, Type t, string head, bool hasSelf = false)
    {
        var mi = t.GetMethod("Invoke");
        var pi = mi.GetParameters();
        int n = pi.Length;

        if (n == 0)
        {
            if (mi.ReturnType == typeof(void))
            {
                if (!hasSelf)
                {
                    sb.AppendFormat("{0}{{\r{0}\tfunc.Call();\r{0}}}\r", head);
                }
                else
                {
                    sb.AppendFormat("{0}{{\r{0}\tfunc.BeginPCall();\r", head);
                    sb.AppendFormat("{0}\tfunc.Push(self);\r", head);
                    sb.AppendFormat("{0}\tfunc.PCall();\r", head);
                    sb.AppendFormat("{0}\tfunc.EndPCall();\r", head);
                    sb.AppendFormat("{0}}}\r", head);
                }
            }
            else
            {
                sb.AppendFormat("{0}{{\r{0}\tfunc.BeginPCall();\r", head);
                if (hasSelf) sb.AppendFormat("{0}\tfunc.Push(self);\r", head);
                sb.AppendFormat("{0}\tfunc.PCall();\r", head);
                GenLuaFunctionRetValue(sb, mi.ReturnType, head + "\t", "ret");
                sb.AppendFormat("{0}\tfunc.EndPCall();\r", head);
                sb.AppendLineEx(head + "\treturn ret;");
                sb.AppendFormat("{0}}}\r", head);
            }

            return;
        }

        sb.AppendFormat("{0}{{\r{0}", head);
        sb.AppendLineEx("\tfunc.BeginPCall();");
        if (hasSelf) sb.AppendFormat("{0}\tfunc.Push(self);\r", head);

        for (int i = 0; i < n; i++)
        {
            var push = GetPushFunction(pi[i].ParameterType);

            if (!IsParams(pi[i]))
            {
                if (pi[i].ParameterType == typeof(byte[]) && IsByteBuffer(t))
                {
                    sb.AppendFormat("{2}\tfunc.PushByteBuffer(param{1});\r", push, i, head);
                }
                else if ((pi[i].Attributes & ParameterAttributes.Out) == ParameterAttributes.None)
                {
                    sb.AppendFormat("{2}\tfunc.{0}(param{1});\r", push, i, head);
                }
            }
            else
            {
                sb.AppendLineEx();
                sb.AppendFormat("{0}\tfor (int i = 0; i < param{1}.Length; i++)\r", head, i);
                sb.AppendLineEx(head + "\t{");
                sb.AppendFormat("{2}\t\tfunc.{0}(param{1}[i]);\r", push, i, head);
                sb.AppendLineEx(head + "\t}\r");
            }
        }

        sb.AppendFormat("{0}\tfunc.PCall();\r", head);

        if (mi.ReturnType == typeof(void))
        {
            for (int i = 0; i < pi.Length; i++)
            {
                if ((pi[i].Attributes & ParameterAttributes.Out) != ParameterAttributes.None)
                {
                    GenLuaFunctionRetValue(sb, pi[i].ParameterType.GetElementType(), head + "\t", "param" + i, true);
                }
            }

            sb.AppendFormat("{0}\tfunc.EndPCall();\r", head);
        }
        else
        {
            GenLuaFunctionRetValue(sb, mi.ReturnType, head + "\t", "ret");

            for (int i = 0; i < pi.Length; i++)
            {
                if ((pi[i].Attributes & ParameterAttributes.Out) != ParameterAttributes.None)
                {
                    GenLuaFunctionRetValue(sb, pi[i].ParameterType.GetElementType(), head + "\t", "param" + i, true);
                }
            }

            sb.AppendFormat("{0}\tfunc.EndPCall();\r", head);
            sb.AppendLineEx(head + "\treturn ret;");
        }

        sb.AppendFormat("{0}}}\r", head);
    }

    public bool IsNeedOp(string name)
    {
        if (name == "op_Addition")
            op |= MetaOp.Add;
        else if (name == "op_Subtraction")
            op |= MetaOp.Sub;
        else if (name == "op_Equality")
            op |= MetaOp.Eq;
        else if (name == "op_Multiply")
            op |= MetaOp.Mul;
        else if (name == "op_Division")
            op |= MetaOp.Div;
        else if (name == "op_UnaryNegation")
            op |= MetaOp.Neg;
        else if (name == "ToString" && !isStaticClass)
            op |= MetaOp.ToStr;
        else
            return false;

        return true;
    }

    void CallOpFunction(string name, int count, string ret)
    {
        var head = string.Empty;

        for (int i = 0; i < count; i++)
            head += "\t";

        if (name == "op_Addition")
            sb.AppendFormat("{0}{1} o = arg0 + arg1;\r", head, ret);
        else if (name == "op_Subtraction")
            sb.AppendFormat("{0}{1} o = arg0 - arg1;\r", head, ret);
        else if (name == "op_Equality")
            sb.AppendFormat("{0}{1} o = arg0 == arg1;\r", head, ret);
        else if (name == "op_Multiply")
            sb.AppendFormat("{0}{1} o = arg0 * arg1;\r", head, ret);
        else if (name == "op_Division")
            sb.AppendFormat("{0}{1} o = arg0 / arg1;\r", head, ret);
        else if (name == "op_UnaryNegation")
            sb.AppendFormat("{0}{1} o = -arg0;\r", head, ret);
    }

    public bool HasAttribute(MemberInfo mb, Type atrtype)
    {
        var attrs = mb.GetCustomAttributes(true);

        for (int j = 0; j < attrs.Length; j++)
        {
            var t = attrs[j].GetType();
            if (t == atrtype)
            {
                return true;
            }
        }

        return false;
    }

    void GenerateEnum()
    {
        fields = type.GetFields(BindingFlags.GetField | BindingFlags.Public | BindingFlags.Static);
        var list = from field in fields
                   where !ToLuaTypes.IsUnsupported(field)
                   select field;

        fields = list.ToArray();

        sb.AppendLineEx("\tpublic static void Register(LuaState L)");
        sb.AppendLineEx("\t{");
        sb.AppendFormat("\t\tL.BeginEnum(typeof({0}));\r", className);

        foreach (var field in fields)
        {
            var fieldName = field.Name;
            var fullName = type.FullName + "." + fieldName;
            var fieldPlatformFlags = ReflectFields.GetPlatformFlags(fullName);
            if (fieldPlatformFlags != ToLuaPlatformFlags.None)
            {
                var fieldPlatformFlagsText = ToLuaPlatformUtility.GetText(fieldPlatformFlags);

                BeginPlatformMacro(fieldPlatformFlagsText);
                sb.AppendFormat("\t\tL.RegVar(\"{0}\", get_{0}, null);\r", fieldName);
                EndPlatformMacro(fieldPlatformFlagsText);
            }
        }

        sb.AppendFormat("\t\tL.RegFunction(\"IntToEnum\", IntToEnum);\r");
        sb.AppendFormat("\t\tL.EndEnum();\r");
        sb.AppendFormat("\t\tTypeTraits<{0}>.Check = CheckType;\r", className);
        sb.AppendFormat("\t\tStackTraits<{0}>.Push = Push;\r", className);
        sb.AppendLineEx("\t}");
        sb.AppendLineEx();

        sb.AppendFormat("\tstatic void Push(IntPtr L, {0} arg)\r", className);
        sb.AppendLineEx("\t{");
        sb.AppendLineEx("\t\tToLua.Push(L, arg);");
        sb.AppendLineEx("\t}");
        sb.AppendLineEx();

        sb.AppendLineEx("\tstatic bool CheckType(IntPtr L, int pos)");
        sb.AppendLineEx("\t{");
        sb.AppendFormat("\t\treturn TypeChecker.CheckEnumType(typeof({0}), L, pos);\r", className);
        sb.AppendLineEx("\t}");

        foreach (var field in fields)
        {
            var fieldName = field.Name;
            var fullName = type.FullName + "." + fieldName;
            var fieldPlatformFlags = ReflectFields.GetPlatformFlags(fullName);
            if (fieldPlatformFlags != ToLuaPlatformFlags.None)
            {
                var fieldPlatformFlagsText = ToLuaPlatformUtility.GetText(fieldPlatformFlags);

                BeginPlatformMacro(fieldPlatformFlagsText);

                sb.AppendLineEx("\r\t[MonoPInvokeCallback(typeof(LuaCSFunction))]");
                sb.AppendFormat("\tstatic int get_{0}(IntPtr L)\r", field.Name);
                sb.AppendLineEx("\t{");
                sb.AppendFormat("\t\tToLua.Push(L, {0}.{1});\r", className, field.Name);
                sb.AppendLineEx("\t\treturn 1;");
                sb.AppendLineEx("\t}");

                EndPlatformMacro(fieldPlatformFlagsText);
            }
        }

        sb.AppendLineEx("\r\t[MonoPInvokeCallback(typeof(LuaCSFunction))]");
        sb.AppendLineEx("\tstatic int IntToEnum(IntPtr L)");
        sb.AppendLineEx("\t{");
        sb.AppendLineEx("\t\tvar arg0 = (int)LuaDLL.lua_tonumber(L, 1);");
        sb.AppendFormat("\t\tvar o = ({0})arg0;\r", className);
        sb.AppendLineEx("\t\tToLua.Push(L, o);");
        sb.AppendLineEx("\t\treturn 1;");
        sb.AppendLineEx("\t}");
    }

    static string CreateDelegate = @"    
    public static Delegate CreateDelegate(Type t, LuaFunction func = null)
    {
        if (!delegates.TryGetValue(t, out var Create))
        {
            throw new LuaException(string.Format(""Delegate {0} not register"", LuaMisc.GetTypeName(t)));            
        }

        if (func != null)
        {
            var state = func.GetLuaState();
            var target = state.GetLuaDelegate(func);
            
            if (target != null)
            {
                return Delegate.CreateDelegate(t, target, target.method);
            }  
            else
            {
                Delegate d = Create(func, null, false);
                target = d.Target as LuaDelegate;
                state.AddLuaDelegate(target, func);
                return d;
            }       
        }

        return Create(null, null, false);        
    }
    
    public static Delegate CreateDelegate(Type t, LuaFunction func, LuaTable self)
    {
        if (!delegates.TryGetValue(t, out var Create))
        {
            throw new LuaException(string.Format(""Delegate {0} not register"", LuaMisc.GetTypeName(t)));
        }

        if (func != null)
        {
            var state = func.GetLuaState();
            var target = state.GetLuaDelegate(func, self);

            if (target != null)
            {
                return Delegate.CreateDelegate(t, target, target.method);
            }
            else
            {
                Delegate d = Create(func, self, true);
                target = d.Target as LuaDelegate;
                state.AddLuaDelegate(target, func, self);
                return d;
            }
        }

        return Create(null, null, true);
    }
";

    static string RemoveDelegate = @"    
    public static Delegate RemoveDelegate(Delegate obj, LuaFunction func)
    {
        var state = func.GetLuaState();
        var ds = obj.GetInvocationList();

        for (int i = 0; i < ds.Length; i++)
        {
            var ld = ds[i].Target as LuaDelegate;

            if (ld != null && ld.func == func)
            {
                obj = Delegate.Remove(obj, ds[i]);
                state.DelayDispose(ld.func);
                break;
            }
        }

        return obj;
    }
    
    public static Delegate RemoveDelegate(Delegate obj, Delegate dg)
    {
        var remove = dg.Target as LuaDelegate;

        if (remove == null)
        {
            obj = Delegate.Remove(obj, dg);
            return obj;
        }

        var state = remove.func.GetLuaState();
        var ds = obj.GetInvocationList();        

        for (int i = 0; i < ds.Length; i++)
        {
            var ld = ds[i].Target as LuaDelegate;

            if (ld != null && ld == remove)
            {
                obj = Delegate.Remove(obj, ds[i]);
                state.DelayDispose(ld.func);
                state.DelayDispose(ld.self);
                break;
            }
        }

        return obj;
    }
";

    string GetDelegateParams(MethodInfo mi)
    {
        var parameters = mi.GetParameters();
        var list = new List<string>();

        for (int index = 0, count = parameters.Length; index < count; ++index)
        {
            var parameter = parameters[index];

            var s2 = GetTypeStr(parameter.ParameterType) + " param" + index;

            if (parameter.ParameterType.IsByRef)
            {
                if (parameter.Attributes == ParameterAttributes.Out)
                {
                    s2 = "out " + s2;
                }
                else
                {
                    s2 = "ref " + s2;
                }
            }

            list.Add(s2);
        }

        return string.Join(", ", list.ToArray());
    }

    string GetReturnValue(Type t)
    {
        if (t.IsPrimitive)
        {
            if (t == typeof(bool))
                return "false";
            if (t == typeof(char))
                return "'\\0'";
            return "0";
        }
        if (!t.IsValueType)
            return "null";
        return $"default({GetTypeStr(t)})";
    }

    string GetDefaultDelegateBody(MethodInfo md)
    {
        var str = "\r\t\t\t{\r";
        bool flag = false;
        var pis = md.GetParameters();

        for (int i = 0; i < pis.Length; i++)
        {
            var pi = pis[i];

            if ((pi.Attributes & ParameterAttributes.Out) != ParameterAttributes.None)
            {
                str += $"\t\t\t\tparam{i} = {GetReturnValue(pi.ParameterType.GetElementType())};\r";
                flag = true;
            }
        }

        if (flag)
        {
            if (md.ReturnType != typeof(void))
            {
                str += "\t\t\treturn ";
                str += GetReturnValue(md.ReturnType);
                str += ";";
            }

            str += "\t\t\t};\r\r";
            return str;
        }

        if (md.ReturnType == typeof(void))
        {
            return "{ };\r";
        }
        else
        {
            return $"{{ return {GetReturnValue(md.ReturnType)}; }};\r";
        }
    }

    private void ForEachDelegates(Type[] delegateTypes, Action<Type, string, string> action, bool multiLine = false)
    {
        if (delegateTypes.Length > 0)
        {
            foreach (var type in delegateTypes)
            {
                var platformFlags = ReflectTypes.GetPlatformFlags(type);
                if (platformFlags == ToLuaPlatformFlags.None)
                    continue;

                var platformFlagsText = ToLuaPlatformUtility.GetText(platformFlags);

                BeginPlatformMacro(platformFlagsText);

                var typeName = GetTypeStr(type);
                var typeFullName = GetTypeFullName(type);

                action(type, typeName, typeFullName);

                sb.AppendLineEx();

                EndPlatformMacro(platformFlagsText);

                if (multiLine)
                {
                    sb.AppendLine();
                }
            }
        }
    }

    public void GenDelegates(Type[] delegateTypes)
    {
        usingList.Add("System");
        usingList.Add("System.Collections.Generic");

        sb.Append("public static class LuaDelegates\r");
        sb.Append("{\r");
        sb.Append(
            "\tpublic static Dictionary<Type, DelegateCreate> delegates => new Dictionary<Type, DelegateCreate>();\r");
        sb.AppendLineEx();
        sb.Append("\tstatic LuaDelegates()");
        sb.AppendLineEx();
        sb.Append("\t{\r");

        ForEachDelegates(delegateTypes, (type, typeName, typeFullName) =>
            sb.AppendLineEx($"\t\tdelegates.Add(typeof({typeName}), {typeFullName});")
        );

        ForEachDelegates(delegateTypes, (type, typeName, typeFullName) =>
            sb.AppendLineEx($"\t\tDelegateTraits<{typeName}>.Init({typeFullName});")
        );

        ForEachDelegates(delegateTypes, (type, typeName, typeFullName) =>
            sb.AppendLineEx($"\t\tTypeTraits<{typeName}>.Init(Check_{typeFullName});")
        );

        ForEachDelegates(delegateTypes, (type, typeName, typeFullName) =>
            sb.AppendLineEx($"\t\tStackTraits<{typeName}>.Push = Push_{typeFullName};")
        );

        sb.Append("\t}\r");
        sb.Append(CreateDelegate);
        sb.AppendLineEx(RemoveDelegate);

        ForEachDelegates(delegateTypes, (type, typeName, typeFullName) => {
            var mi = type.GetMethod("Invoke");
            var args = GetDelegateParams(mi);

            //生成委托类
            sb.AppendFormat("\tclass {0}_Event : LuaDelegate\r", typeFullName);
            sb.AppendLineEx("\t{");
            sb.AppendFormat("\t\tpublic {0}_Event(LuaFunction func) : base(func) {{ }}\r", typeFullName);
            sb.AppendFormat("\t\tpublic {0}_Event(LuaFunction func, LuaTable self) : base(func, self) {{ }}\r", typeFullName);
            sb.AppendLineEx();
            sb.AppendFormat("\t\tpublic {0} Call({1})\r", GetTypeStr(mi.ReturnType), args);
            GenDelegateBody(sb, type, "\t\t");
            sb.AppendLineEx();
            sb.AppendFormat("\t\tpublic {0} CallWithSelf({1})\r", GetTypeStr(mi.ReturnType), args);
            GenDelegateBody(sb, type, "\t\t", true);
            sb.AppendLineEx("\t}\r");

            //生成转换函数1
            sb.AppendFormat("\tpublic static {0} {1}(LuaFunction func, LuaTable self, bool flag)\r", typeName, typeFullName);
            sb.AppendLineEx("\t{");
            sb.AppendLineEx("\t\tif (func == null)");
            sb.AppendLineEx("\t\t{");
            sb.AppendFormat("\t\t\t{0} fn = delegate({1}) {2}", typeName, args, GetDefaultDelegateBody(mi));
            sb.AppendLineEx("\t\t\treturn fn;");
            sb.AppendLineEx("\t\t}\r");
            sb.AppendLineEx("\t\tif(!flag)");
            sb.AppendLineEx("\t\t{");
            sb.AppendFormat("\t\t\tvar target = new {0}_Event(func);\r", typeFullName);
            sb.AppendFormat("\t\t\t{0} d = target.Call;\r", typeName);
            sb.AppendLineEx("\t\t\ttarget.method = d.Method;");
            sb.AppendLineEx("\t\t\treturn d;");
            sb.AppendLineEx("\t\t}");
            sb.AppendLineEx("\t\telse");
            sb.AppendLineEx("\t\t{");
            sb.AppendFormat("\t\t\tvar target = new {0}_Event(func, self);\r", typeFullName);
            sb.AppendFormat("\t\t\t{0} d = target.CallWithSelf;\r", typeName);
            sb.AppendLineEx("\t\t\ttarget.method = d.Method;");
            sb.AppendLineEx("\t\t\treturn d;");
            sb.AppendLineEx("\t\t}");
            sb.AppendLineEx("\t}\r");

            sb.AppendFormat("\tstatic bool Check_{0}(IntPtr L, int pos)\r", typeFullName);
            sb.AppendLineEx("\t{");
            sb.AppendFormat("\t\treturn TypeChecker.CheckDelegateType(typeof({0}), L, pos);\r", typeName);
            sb.AppendLineEx("\t}\r");

            sb.AppendFormat("\tstatic void Push_{0}(IntPtr L, {1} o)\r", typeFullName, typeName);
            sb.AppendLineEx("\t{");
            sb.AppendLineEx("\t\tToLua.Push(L, o);");
            sb.Append("\t}");
        }, true);

        sb.AppendLineEx("}\r");
        SaveFile(ToLuaSettingsUtility.Settings.SaveDir + "LuaDelegates.cs");
    }

    bool IsUseDefinedAttributee(MemberInfo mb)
    {
        var attrs = mb.GetCustomAttributes(false);

        for (int j = 0; j < attrs.Length; j++)
        {
            var t = attrs[j].GetType();

            if (t == typeof(UseDefinedAttribute))
            {
                return true;
            }
        }

        return false;
    }

    bool IsMethodEqualExtend(MethodBase a, MethodBase b)
    {
        if (a.Name != b.Name)
            return false;

        int c1 = a.IsStatic ? 0 : 1;
        int c2 = b.IsStatic ? 0 : 1;

        c1 += a.GetParameters().Length;
        c2 += b.GetParameters().Length;

        if (c1 != c2) return false;

        var lp = a.GetParameters();
        var rp = b.GetParameters();

        var ll = new List<Type>();
        var lr = new List<Type>();

        if (!a.IsStatic)
            ll.Add(type);

        if (!b.IsStatic)
            lr.Add(type);

        for (int i = 0; i < lp.Length; i++)
            ll.Add(lp[i].ParameterType);

        for (int i = 0; i < rp.Length; i++)
            lr.Add(rp[i].ParameterType);

        for (int i = 0; i < ll.Count; i++)
        {
            if (ll[i] != lr[i])
            {
                return false;
            }
        }

        return true;
    }

    bool IsGenericType(MethodInfo md, Type t)
    {
        var list = md.GetGenericArguments();

        for (int i = 0; i < list.Length; i++)
        {
            if (list[i] == t)
            {
                return true;
            }
        }

        return false;
    }

    void ProcessExtendType(Type extendType, List<_MethodBase> methodBases)
    {
        if (extendType == null)
            return;

        var list2 = new List<MethodInfo>();
        list2.AddRange(
            extendType.GetMethods(BindingFlags.Public | BindingFlags.Static | BindingFlags.DeclaredOnly));

        for (int i = list2.Count - 1; i >= 0; i--)
        {
            var md = list2[i];

            if (!md.IsDefined(typeof(ExtensionAttribute), false))
                continue;

            var plist = md.GetParameters();
            var t = plist[0].ParameterType;

            if (t == type || t.IsAssignableFrom(type) ||
                (IsGenericType(md, t) && (type == t.BaseType || type.IsSubclassOf(t.BaseType))))
            {
                if (!ToLuaTypes.IsUnsupported(list2[i]))
                {
                    var mb = new _MethodBase(md);
                    mb.BeExtend = true;
                    methodBases.Add(mb);
                }
            }
        }
    }

    private IEnumerable<Type> GetExtensionTypes(Type extendedType)
    {
        var query = from type in allTypes
                    where type.IsSealed && !type.IsGenericType && !type.IsNested
                    from method in type.GetMethods(BindingFlags.Static | BindingFlags.Public | BindingFlags.NonPublic)
                    where method.IsDefined(typeof(ExtensionAttribute), false)
                    where method.GetParameters()[0].ParameterType == extendedType
                    select type;
        return query;
    }

    void ProcessExtends(List<_MethodBase> methodBases)
    {
        var extendTypes = GetExtensionTypes(type);
        foreach (var extendType in extendTypes)
        {
            ProcessExtendType(extendType, methodBases);
            var nameSpace = GetNameSpace(extendType, out var _);

            if (!string.IsNullOrEmpty(nameSpace))
            {
                usingList.Add(nameSpace);
            }
        }
    }

    void GetDelegateTypeFromMethodParams(_MethodBase m)
    {
        if (m.IsGenericMethod)
            return;

        var pifs = m.GetParameters();

        for (int k = 0; k < pifs.Length; k++)
        {
            var t = pifs[k].ParameterType;

            if (IsDelegateType(t))
            {
                eventSet.Add(t);
            }
        }
    }

    public void GenerateDelegate(Type t, StringBuilder sb)
    {
        var platformFlags = ReflectTypes.GetPlatformFlags(t);
        if (platformFlags == ToLuaPlatformFlags.None)
            return;

        sb.AppendLineEx("\r");

        var platformFlagsText = ToLuaPlatformUtility.GetText(platformFlags);

        BeginPlatformMacro(platformFlagsText);

        var space = GetNameSpace(t, out var funcName);
        funcName = CombineTypeStr(space, funcName);
        funcName = ConvertToLibSign(funcName);

        sb.AppendLineEx("\t[MonoPInvokeCallback(typeof(LuaCSFunction))]");
        sb.AppendFormat("\tstatic int {0}(IntPtr L)\r", funcName);
        sb.AppendLineEx("\t{");
        sb.AppendLineEx("\t\ttry");
        sb.AppendLineEx("\t\t{");
        sb.AppendLineEx("\t\t\tint count = LuaDLL.lua_gettop(L);");
        sb.AppendLineEx("\t\t\tLuaFunction func = ToLua.CheckLuaFunction(L, 1);");
        sb.AppendLineEx();
        sb.AppendLineEx("\t\t\tif (count == 1)");
        sb.AppendLineEx("\t\t\t{");
        sb.AppendFormat("\t\t\t\tvar arg1 = DelegateTraits<{0}>.Create(func);\r", GetTypeStr(t));
        sb.AppendLineEx("\t\t\t\tToLua.Push(L, arg1);");
        sb.AppendLineEx("\t\t\t}");
        sb.AppendLineEx("\t\t\telse");
        sb.AppendLineEx("\t\t\t{");
        sb.AppendLineEx("\t\t\t\tvar self = ToLua.CheckLuaTable(L, 2);");
        sb.AppendFormat("\t\t\t\tvar arg1 = DelegateTraits<{0}>.Create(func, self);\r", GetTypeStr(t));
        sb.AppendFormat("\t\t\t\tToLua.Push(L, arg1);\r");
        sb.AppendLineEx("\t\t\t}");

        sb.AppendLineEx("\t\t\treturn 1;");
        sb.AppendLineEx("\t\t}");
        sb.AppendLineEx("\t\tcatch(Exception e)");
        sb.AppendLineEx("\t\t{");
        sb.AppendLineEx("\t\t\treturn LuaDLL.toluaL_exception(L, e);");
        sb.AppendLineEx("\t\t}");
        sb.AppendLineEx("\t}");

        EndPlatformMacro(platformFlagsText);
    }

    void GenEventFunctions()
    {
        foreach (Type t in eventSet)
        {
            GenerateDelegate(t, sb);
        }
    }

    static string RemoveChar(string str, char c)
    {
        int index = str.IndexOf(c);

        while (index > 0)
        {
            str = str.Remove(index, 1);
            index = str.IndexOf(c);
        }

        return str;
    }

    public static string ConvertToLibSign(string str)
    {
        if (string.IsNullOrEmpty(str))
            return null;

        str = str.Replace('<', '_');
        str = RemoveChar(str, '>');
        str = str.Replace('[', 's');
        str = RemoveChar(str, ']');
        str = str.Replace('.', '_');
        return str.Replace(',', '_');
    }

    public static string GetNameSpace(Type t, out string libName)
    {
        if (t.IsGenericType)
        {
            return GetGenericNameSpace(t, out libName);
        }
        else
        {
            var space = t.FullName;

            if (space.Contains("+"))
            {
                space = space.Replace('+', '.');
                int index = space.LastIndexOf('.');
                libName = space.Substring(index + 1);
                return space.Substring(0, index);
            }
            else
            {
                libName = t.Namespace == null ? space : space.Substring(t.Namespace.Length + 1);
                return t.Namespace;
            }
        }
    }

    static string GetGenericNameSpace(Type t, out string libName)
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

            name = CombineTypeStr(name, str);
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

        libName = str;

        if (string.IsNullOrEmpty(space))
        {
            space = t.Namespace;

            if (space != null)
            {
                libName = str.Substring(space.Length + 1);
            }
        }

        return space;
    }
}
}
