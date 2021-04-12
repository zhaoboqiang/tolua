using System.Reflection;
using System.Collections.Generic;
using System.Text;
using System.Linq;
using System;

namespace LuaInterface.Editor
{
    public static class MethodPreprocessConditions
    {
        private static Dictionary<string, LuaMethodPreprocessConditionSetting> settings;
        public static Dictionary<string, LuaMethodPreprocessConditionSetting> Settings
        {
            get
            {
                if (settings == null)
                {
                    var oldSettings = LuaSettingsUtility.LoadCsv<LuaMethodPreprocessConditionSetting>(ToLuaSettingsUtility.Settings.MethodPreprocessConditionCsv, ':');
                    if (oldSettings == null)
                        settings = new Dictionary<string, LuaMethodPreprocessConditionSetting>();
                    else
                        settings = oldSettings.ToDictionary(key => key.MethodSignature);
                }
                return settings;
            }
        }

        public static void Reset()
        {
            settings = null;
        }

        public static string Lookup(MethodInfo methodInfo)
        {
            var signature = ReflectMethods.GetMethodSignature(methodInfo);
            if (Settings.TryGetValue(signature, out var value))
                return value.PreprocessConditions;
            return string.Empty;
        }
    }
}
