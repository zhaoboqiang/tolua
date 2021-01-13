
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;

namespace LuaInterface.Editor
{
    public static class ReflectProperties
    {
        private static Dictionary<string, LuaPropertySetting> propertySettings;
        private static Dictionary<string, LuaPropertySetting> fieldSettings
        {
            get
            {
                if (propertySettings == null)
                {
                    var fields = LuaSettingsUtility.LoadCsv<LuaPropertySetting>(ToLuaSettingsUtility.Settings.PropertyCsv);
                    if (fields == null)
                        propertySettings = new Dictionary<string, LuaPropertySetting>();
                    else
                        propertySettings = fields.ToDictionary(key => key.FieldFullName);
                }
                return propertySettings;
            }
        }

        public static void Reset()
        {
            propertySettings = null;
        }

        public static LuaPropertySetting Lookup(string name)
        {
            var property = new LuaPropertySetting();

            if (!fieldSettings.TryGetValue(name, out property))
            {
                property.CanRead = true;
                property.CanWrite = true;
            }

            return property;
        }

        public static LuaPropertySetting Lookup(MemberInfo memberInfo)
        {
            var name = $"{ToLuaMembers.GetTypeName(memberInfo)}.{memberInfo.Name}";
            return Lookup(name);
        }
 
    }
}
