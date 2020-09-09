using UnityEngine;
using System;
using System.Collections.Generic;
using LuaInterface;
using LuaInterface.Editor;
using UnityEditor;

[InitializeOnLoad]
public class CustomToLuaSettings : ToLuaSettings
{
    static CustomToLuaSettings()
    {
        ToLuaSettingsUtility.Initialize(new CustomToLuaSettings());

        LuaSettingsUtility.Initialize(new LuaSettings
        {
            luaRegister = LuaBinder.Register,
            delegates = LuaDelegates.delegates
        });
    }

    public string saveDir => Application.dataPath + "/Source/Generate/";
    public string toluaBaseType => Application.dataPath + "/ToLua/BaseType/";
    public string baseLuaDir => Application.dataPath + "/ToLua/Lua/";
    public string injectionFilesPath => Application.dataPath + "/ToLua/Injection/";

    public string IncludedAssemblyCsv => "included_assembly";
    public string IncludedNamespaceCsv => "included_namespace";
    public string IncludedTypeCsv => "included_type";
}