using System;
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
        public bool Editor;
    }

    [Serializable]
    public struct LuaIncludedNamespace
    {
        public string Namespace;
        public bool Android;
        public bool iOS;
        public bool Editor;
    }

    [Serializable]
    public struct LuaIncludedType
    {
        public string FullName;
        public string Note;
        public bool Android;
        public bool iOS;
        public bool Editor;
    }

    [Serializable]
    public struct LuaIncludedField
    {
        public string FieldFullName;
        public bool Android;
        public bool iOS;
        public bool Editor;
    }

    [Serializable]
    public struct LuaIncludedProperty
    {
        public string FieldFullName;
        public bool CanRead;
        public bool CanWrite;
    }
}