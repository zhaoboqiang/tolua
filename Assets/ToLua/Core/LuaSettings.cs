﻿using System;
using System.Collections.Generic;

namespace LuaInterface
{
    public struct LuaSettings
    {
        public LuaRegister luaRegister;

        public Dictionary<Type, DelegateCreate> delegates;
    }

    [Serializable]
    public struct LuaIncludedAssembly
    {
        public string Name;
        public bool Android;
        public bool iOS;
    }

    [Serializable]
    public struct LuaIncludedNamespace
    {
        public string Namespace;
        public bool Android;
        public bool iOS;
    }

    [Serializable]
    public struct LuaIncludedType
    {
        public string Namespace;
        public string Name;
        public string FullName;
        public string Note;
        public bool Android;
        public bool iOS;
    }

    [Serializable]
    public struct LuaIncludedMethod
    {
        public string MethodName;
        public bool Android;
        public bool iOS;
    }

    [Serializable]
    public struct LuaIncludedEnum
    {
        public string FieldFullName;
        public bool Android;
        public bool iOS;
    }

}