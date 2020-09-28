
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

        public static bool IsMethodIncluded(string methodName)
        {
            if (IncludedMethods.TryGetValue(methodName, out var value))
            {
#if UNITY_IOS
                if (value.iOS)
                    return true;
#elif UNITY_ANDROID
                if (value.Android)
                    return true;
#else
                if (value.iOS || value.Android)
                    return true;
#endif
                return false;
            }
            return true;
        }


        public static bool AndroidSupported(Type type)
        {
            // TODO:
            return true;
        }
        
        public static bool iOSSupported(Type type)
        {
            // TODO:
            return true;
        }
    }
}
