
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;

namespace LuaInterface.Editor
{
    public static class ReflectEnums
    {
        private static Dictionary<string, LuaIncludedEnum> includedEnums;
        public static Dictionary<string, LuaIncludedEnum> IncludedEnums
        {
            get
            {
                if (includedEnums == null)
                {
                    var enums = LuaSettingsUtility.LoadCsv<LuaIncludedEnum>(ToLuaSettingsUtility.Settings.IncludedEnumCsv);
                    if (enums == null)
                        includedEnums = new Dictionary<string, LuaIncludedEnum>();
                    else
                        includedEnums = enums.ToDictionary(key => key.FieldFullName);
                }
                return includedEnums;
            }
        }

        public static ToLuaPlatformFlags GetPlatformFlags(string name)
        {
            var flags = ToLuaPlatformFlags.All; // deny list

            if (IncludedEnums.TryGetValue(name, out var value))
                flags = ToLuaPlatformUtility.From(value.Android, value.iOS, value.Android || value.iOS);

            return flags;
        }


    }
}
