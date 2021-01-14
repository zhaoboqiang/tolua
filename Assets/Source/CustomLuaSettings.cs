using UnityEngine;
using LuaInterface;

public class CustomLuaSettings
{
    static CustomLuaSettings()
    {
        LuaSettingsUtility.Initialize(new LuaSettings{
            delegates = LuaDelegates.delegates,
            UsingCsvPath = Application.dataPath + "/Settings/Usings.csv" 
        });
    }
}