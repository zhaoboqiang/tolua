using System.Collections.Generic;
using System.Linq;

namespace LuaInterface
{
    public static class LuaUsingSettings
    {
        private static Dictionary<string, LuaUsingSetting> settings;

        public static Dictionary<string, LuaUsingSetting> Settings
        {
            get
            {
                if (settings == null)
                {
                    var types = LuaSettingsUtility.LoadCsv<LuaUsingSetting>(LuaSettingsUtility.Settings.UsingCsvPath);
                    if (types == null)
                        settings = new Dictionary<string, LuaUsingSetting>();
                    else
                        settings = types.ToDictionary(key => key.FullName);
                }
                return settings;
            }
        }

        public static void Reset()
        {
            settings = null;
        }
    }
}
