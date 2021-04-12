using System;
using System.Collections.Generic;
using LuaInterface;

public struct LuaSettings
{
    public Dictionary<Type, DelegateCreate> delegates;

    public string UsingCsvPath;
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
public struct LuaMethodSetting
{
    public string MethodSignature;
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

[Serializable]
public struct LuaFieldPreprocessConditionSetting
{
    public string FieldFullName;
    public string PreprocessConditions;
}

[Serializable]
public struct LuaMethodPreprocessConditionSetting
{
    public string MethodSignature;
    public string PreprocessConditions;
}

