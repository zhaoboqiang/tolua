
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;

namespace LuaInterface.Editor
{
    public static class ReflectMethods
    {
        private static Dictionary<string, LuaIncludedMethod> includedMethods;
        public static Dictionary<string, LuaIncludedMethod> IncludedMethods
        {
            get
            {
                if (includedMethods == null)
                {
                    var methods = LuaSettingsUtility.LoadCsv<LuaIncludedMethod>(ToLuaSettingsUtility.Settings.IncludedMethodCsv);
                    if (methods == null)
                        includedMethods = new Dictionary<string, LuaIncludedMethod>();
                    else
                        includedMethods = methods.ToDictionary(key => key.MethodName);
                }
                return includedMethods;
            }
        }

        public static ToLuaPlatformFlags GetPlatformFlags(string name)
        {
            var flags = ToLuaPlatformFlags.All; // deny list

            if (IncludedMethods.TryGetValue(name, out var value))
                flags = ToLuaPlatformUtility.From(value.Android, value.iOS, value.Android || value.iOS);

            return flags;
        }
    }
}
