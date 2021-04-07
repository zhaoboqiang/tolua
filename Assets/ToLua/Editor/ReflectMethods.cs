using System.Reflection;
using System.Collections.Generic;
using System.Text;
using System.Linq;

namespace LuaInterface.Editor
{
    public static class ReflectMethods
    {
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
            var parameterText = parameterInfo.ParameterType.ToString();

            if ((parameterInfo.Attributes & ParameterAttributes.In) != ParameterAttributes.None)
                return "in " + parameterText;

            if ((parameterInfo.Attributes & ParameterAttributes.Out) != ParameterAttributes.None)
                return "out " + parameterText;
            
            return parameterText;
        }

        private static string GetMethodSignature(MethodInfo methodInfo)
        {
            var signature = new StringBuilder();
            signature.Append($"{methodInfo.ReflectedType.FullName}.{methodInfo.Name}(");
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

            // return
            var returnType = methodInfo.ReturnType;
            flags &= ReflectTypes.GetPlatformFlags(returnType);

            // parameters
            foreach (var parameter in methodInfo.GetParameters())
                flags &= ReflectTypes.GetPlatformFlags(parameter.GetType());

            return flags;
        }
 
        public static bool Included(MethodInfo methodInfo)
        {
            if (ToLuaTypes.IsUnsupported(methodInfo))
                return false; 
 
            if (GetPlatformFlags(methodInfo) == ToLuaPlatformFlags.None)
                return false;

            return true;
        }
    }
}


