
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;

namespace LuaInterface.Editor
{
    public static class ReflectFields
    {
        private static Dictionary<string, LuaFieldSetting> fieldSettings;
        public static Dictionary<string, LuaFieldSetting> FieldSettings
        {
            get
            {
                if (fieldSettings == null)
                {
                    var fields = LuaSettingsUtility.LoadCsv<LuaFieldSetting>(ToLuaSettingsUtility.Settings.FieldCsv);
                    if (fields == null)
                        fieldSettings = new Dictionary<string, LuaFieldSetting>();
                    else
                        fieldSettings = fields.ToDictionary(key => key.FieldFullName);
                }
                return fieldSettings;
            }
        }

        public static void Reset()
        {
            fieldSettings = null;
        }

        public static ToLuaPlatformFlags GetPlatformFlags(string name)
        {
            var flags = ToLuaPlatformFlags.All; // deny list

            if (FieldSettings.TryGetValue(name, out var value))
                flags = ToLuaPlatformUtility.From(value.Android, value.iOS, value.Editor);

            return flags;
        }

        public static ToLuaPlatformFlags GetPlatformFlags(MemberInfo memberInfo)
        {
            try
            {
                var name = $"{memberInfo.ReflectedType.FullName}.{memberInfo.Name}";

                return GetPlatformFlags(name);
            }
            catch (Exception e)
            {
                Debugger.LogError($"ReflectFields.GetPlatformFlags({memberInfo.Name}) {e.Message}");
                return ToLuaPlatformFlags.None;
            }
        }
 
    }
}
