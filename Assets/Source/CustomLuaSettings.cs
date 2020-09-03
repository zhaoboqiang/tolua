using LuaInterface;

namespace Editor
{
    public class CustomLuaSettings
    {
        static CustomLuaSettings()
        {
            LuaSettingsUtility.Initialize(new LuaSettings{
                luaRegister = LuaBinder.Register,
                delegates = LuaDelegates.delegates
            });
        }
    }
}