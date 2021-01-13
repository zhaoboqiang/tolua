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
    public struct LuaAssemblySetting
    {
        public string Name;
        public bool Android;
        public bool iOS;
        public bool Editor;
    }

    [Serializable]
    public struct LuaNamespaceSetting
    {
        public string Namespace;
        public bool Android;
        public bool iOS;
        public bool Editor;
    }

    [Serializable]
    public struct LuaTypeSetting
    {
        public string FullName;
        public string Note;
        public bool Android;
        public bool iOS;
        public bool Editor;
    }

    [Serializable]
    public struct LuaFieldSetting
    {
        public string FieldFullName;
        public bool Android;
        public bool iOS;
        public bool Editor;
    }

    [Serializable]
    public struct LuaPropertySetting
    {
        public string FieldFullName;
        public bool CanRead;
        public bool CanWrite;
    }

    [Serializable]
    public struct LuaUsingSetting
    {
        public string FullName;
        public bool Preload;
    }

}