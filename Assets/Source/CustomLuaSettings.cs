using UnityEngine;
using LuaInterface;

public class CustomLuaSettings
{
    static CustomLuaSettings()
    {
        LuaSettingsUtility.Initialize(new LuaSettings{
            luaRegister = LuaBinder.Register,
            delegates = LuaDelegates.delegates,
            UsingCsvPath = Application.dataPath + "/Settings/Usings.csv" 
        });
    }
}