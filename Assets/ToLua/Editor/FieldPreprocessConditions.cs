using System.Reflection;
using System.Collections.Generic;
using System.Text;
using System.Linq;
using System;

namespace LuaInterface.Editor
{
    public static class FieldPreprocessConditions
    {
        private static Dictionary<string, LuaFieldPreprocessConditionSetting> settings;
        public static Dictionary<string, LuaFieldPreprocessConditionSetting> Settings
        {
            get
            {
                if (settings == null)
                {
                    var oldSettings = LuaSettingsUtility.LoadCsv<LuaFieldPreprocessConditionSetting>(ToLuaSettingsUtility.Settings.FieldPreprocessConditionCsv);
                    if (oldSettings == null)
                        settings = new Dictionary<string, LuaFieldPreprocessConditionSetting>();
                    else
                        settings = oldSettings.ToDictionary(key => key.FieldFullName);
                }
                return settings;
            }
        }

        public static void Reset()
        {
            settings = null;
        }

        public static string Lookup(string name)
        {
            if (Settings.TryGetValue(name, out var value))
                return value.PreprocessConditions;
            return string.Empty;
        }

        public static string Lookup(MemberInfo memberInfo)
        {
            try
            {
                var name = $"{memberInfo.ReflectedType.FullName}.{memberInfo.Name}";

                return Lookup(name);
            }
            catch (Exception e)
            {
                Debugger.LogError($"FieldPreprocessConditions.Lookup({memberInfo.Name}) {e.Message}");
                return string.Empty;
            }
        }
 
    }
}
