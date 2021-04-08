using System.Reflection;
using System.Collections.Generic;
using System.Text;
using System.Linq;
using System;

namespace LuaInterface.Editor
{
    public static class ReflectMethods
    {
        static readonly NLog.Logger log = NLog.LoggerFactory.GetLogger(typeof(ToLuaTypes).Name);

        private static Dictionary<string, LuaMethodSetting> methodSettings;
        public static Dictionary<string, LuaMethodSetting> MethodSettings
        {
            get
            {
                if (methodSettings == null)
                {
                    var methods = LuaSettingsUtility.LoadCsv<LuaMethodSetting>(ToLuaSettingsUtility.Settings.MethodCsv, ':');
                    if (methods == null)
                        methodSettings = new Dictionary<string, LuaMethodSetting>();
                    else
                        methodSettings = methods.ToDictionary(key => key.MethodSignature);
                }
                return methodSettings;
            }
        }

        public static void Reset()
        {
            methodSettings = null;
        }

        private static string GetParameterText(ParameterInfo parameterInfo)
        {
            var parameterText = ToLuaTypes.GetFullName(parameterInfo.ParameterType);

            if ((parameterInfo.Attributes & ParameterAttributes.In) != ParameterAttributes.None)
                return "in " + parameterText;

            if ((parameterInfo.Attributes & ParameterAttributes.Out) != ParameterAttributes.None)
                return "out " + parameterText;
            
            return parameterText;
        }

        private static string GetMethodSignature(MethodInfo methodInfo)
        {
            var signature = new StringBuilder();

            var fullName = ToLuaTypes.GetFullName(methodInfo.ReflectedType);

            signature.Append($"{fullName}.{methodInfo.Name}(");

            var parameterTexts = new List<string>();
            foreach (var parameter in methodInfo.GetParameters())
            {
                var parameterText = GetParameterText(parameter);
                parameterTexts.Add(parameterText);
            }
            signature.Append(string.Join(",", parameterTexts));
            signature.Append(")");

            return signature.ToString();
        }

        public static ToLuaPlatformFlags GetPlatformFlags(MethodInfo methodInfo)
        {
            var flags = ToLuaPlatformFlags.All;
            if (methodInfo == null)
                return flags;

            var signature = GetMethodSignature(methodInfo);
            if (MethodSettings.TryGetValue(signature, out var value))
                flags = ToLuaPlatformUtility.From(value.Android, value.iOS, value.Editor);

            /*
            // return
            var returnType = methodInfo.ReturnType;
            flags &= ReflectTypes.GetPlatformFlags(returnType);

            // parameters
            foreach (var parameter in methodInfo.GetParameters())
                flags &= ReflectTypes.GetPlatformFlags(parameter.GetType());
            */
            return flags;
        }
 
        public static bool Included(MethodInfo methodInfo)
        {
            try
            {
                if (ToLuaTypes.IsUnsupported(methodInfo))
                    return false; 
    
                var flags = ToLuaPlatformFlags.All;
                if (methodInfo == null)
                    return false;

                var signature = GetMethodSignature(methodInfo);
                if (MethodSettings.TryGetValue(signature, out var value))
                {
                    flags = ToLuaPlatformUtility.From(value.Android, value.iOS, value.Editor);
                    if (flags == ToLuaPlatformFlags.None)
                    {
                        return false;
                    }
                }

                return true;
            }
            catch (Exception exception)
            {
                log.Error($"{methodInfo.Name} {exception.Message}");
                return true;
            }
        }
    }
}


