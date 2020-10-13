﻿using UnityEngine;
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

    public string SaveDir => Application.dataPath + "/Source/Generate/";
    public string WrapperSaveDir => SaveDir + "/Wrappers/";

    public string ToluaBaseType => Application.dataPath + "/ToLua/BaseType/";
    public string baseLuaDir => Application.dataPath + "/ToLua/Lua/";
    public string injectionFilesPath => Application.dataPath + "/ToLua/Injection/";

    private string GetCsvPath(string filename) => Application.dataPath + "/EditorSetting/" + filename + ".csv";

    public string IncludedAssemblyCsv => GetCsvPath("Assemblies");
    public string IncludedNamespaceCsv => GetCsvPath("Namespaces");
    public string IncludedTypeCsv => GetCsvPath("Types");
    public string IncludedFieldCsv => GetCsvPath("Fields");
}