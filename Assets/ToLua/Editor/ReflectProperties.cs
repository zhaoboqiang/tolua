
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;

namespace LuaInterface.Editor
{
    public static class ReflectProperties
    {
        private static Dictionary<string, LuaIncludedProperty> includedProperties;
        private static Dictionary<string, LuaIncludedProperty> IncludedFields
        {
            get
            {
                if (includedProperties == null)
                {
                    var fields = LuaSettingsUtility.LoadCsv<LuaIncludedProperty>(ToLuaSettingsUtility.Settings.PropertyCsv);
                    if (fields == null)
                        includedProperties = new Dictionary<string, LuaIncludedProperty>();
                    else
                        includedProperties = fields.ToDictionary(key => key.FieldFullName);
                }
                return includedProperties;
            }
        }

        public static void Reset()
        {
            includedProperties = null;
        }

        public static LuaIncludedProperty Lookup(string name)
        {
            var property = new LuaIncludedProperty();

            if (!IncludedFields.TryGetValue(name, out property))
            {
                property.CanRead = true;
                property.CanWrite = true;
            }

            return property;
        }

        public static LuaIncludedProperty Lookup(MemberInfo memberInfo)
        {
            var name = $"{ToLuaMembers.GetTypeName(memberInfo)}.{memberInfo.Name}";
            return Lookup(name);
        }
 
    }
}
