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

    private static string dataPath = Application.dataPath;

    public string SaveDir => dataPath + "/Source/Generate/";
    public string WrapperSaveDir => SaveDir + "/Wrappers/";

    public string ToluaBaseType => dataPath + "/ToLua/BaseType/";
    public string baseLuaDir => dataPath + "/ToLua/Lua/";
    public string injectionFilesPath => dataPath + "/ToLua/Injection/";

    private string GetEditorCsvPath(string filename) => dataPath + "/EditorSettings/" + filename + ".csv";
    private string GetPlayerCsvPath(string filename) => dataPath + "/Settings/" + filename + ".csv";

    public string AssemblyCsv => GetEditorCsvPath("Assemblies");
    public string NamespaceCsv => GetEditorCsvPath("Namespaces");
    public string TypeCsv => GetEditorCsvPath("Types");
    public string FieldCsv => GetEditorCsvPath("Fields");
    public string PropertyCsv => GetEditorCsvPath("Properties");
    public string UsingCsv => GetPlayerCsvPath("Usings");
}