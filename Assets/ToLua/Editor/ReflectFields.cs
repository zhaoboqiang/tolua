
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;

namespace LuaInterface.Editor
{
    public static class ReflectFields
    {
        private static Dictionary<string, LuaIncludedField> includedFields;
        public static Dictionary<string, LuaIncludedField> IncludedFields
        {
            get
            {
                if (includedFields == null)
                {
                    var fields = LuaSettingsUtility.LoadCsv<LuaIncludedField>(ToLuaSettingsUtility.Settings.IncludedFieldCsv);
                    if (fields == null)
                        includedFields = new Dictionary<string, LuaIncludedField>();
                    else
                        includedFields = fields.ToDictionary(key => key.FieldFullName);
                }
                return includedFields;
            }
        }

        public static ToLuaPlatformFlags GetPlatformFlags(string name)
        {
            var flags = ToLuaPlatformFlags.All; // deny list

            if (IncludedFields.TryGetValue(name, out var value))
                flags = ToLuaPlatformUtility.From(value.Android, value.iOS, value.Android || value.iOS);

            return flags;
        }
    }
}
