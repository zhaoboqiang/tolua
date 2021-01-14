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
        var customToLuaSettings = new CustomToLuaSettings();

        ToLuaSettingsUtility.Initialize(customToLuaSettings);

        LuaSettingsUtility.Initialize(new LuaSettings
        {
            luaRegister = LuaBinder.Register,
            delegates = LuaDelegates.delegates,
            UsingCsvPath = customToLuaSettings.UsingCsv
        });
    }

    private static string dataPath = Application.dataPath;

    public string SaveDir => dataPath + "/Source/Generate/";
    public string WrapperSaveDir => SaveDir + "/Wrappers/";

    public string ToluaBaseType => dataPath + "/ToLua/BaseType/";
    public string baseLuaDir => dataPath + "/ToLua/Lua/";
    public string injectionFilesPath => dataPath + "/ToLua/Injection/";

    private static string GetEditorCsvPath(string filename) => dataPath + "/EditorSettings/" + filename + ".csv";
    private static string GetPlayerCsvPath(string filename) => dataPath + "/Settings/" + filename + ".csv";

    public string AssemblyCsv => GetEditorCsvPath("Assemblies");
    public string NamespaceCsv => GetEditorCsvPath("Namespaces");
    public string TypeCsv => GetEditorCsvPath("Types");
    public string FieldCsv => GetEditorCsvPath("Fields");
    public string PropertyCsv => GetEditorCsvPath("Properties");

    public string UsingCsv => GetPlayerCsvPath("Usings");
}