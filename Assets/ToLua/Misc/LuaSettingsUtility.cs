
namespace LuaInterface
{
    public static class LuaSettingsUtility
    {
        public static LuaSettings Settings { get; private set; }
        
        public static void Initialize(LuaSettings settings)
        {
            Settings = settings;
        }
    }
}