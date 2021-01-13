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

    private static string dataPath = Application.dataPath;

    public string SaveDir => dataPath + "/Source/Generate/";
    public string WrapperSaveDir => SaveDir + "/Wrappers/";

    public string ToluaBaseType => dataPath + "/ToLua/BaseType/";
    public string baseLuaDir => dataPath + "/ToLua/Lua/";
    public string injectionFilesPath => dataPath + "/ToLua/Injection/";

    private string GetCsvPath(string filename) => dataPath + "/EditorSetting/" + filename + ".csv";

    public string AssemblyCsv => GetCsvPath("Assemblies");
    public string NamespaceCsv => GetCsvPath("Namespaces");
    public string TypeCsv => GetCsvPath("Types");
    public string FieldCsv => GetCsvPath("Fields");
    public string PropertyCsv => GetCsvPath("Properties");
    public string UsingCsv => GetCsvPath("Usings");
}