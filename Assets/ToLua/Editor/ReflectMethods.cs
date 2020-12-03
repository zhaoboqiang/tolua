using System.Reflection;

namespace LuaInterface.Editor
{
    public static class ReflectMethods
    {
        public static ToLuaPlatformFlags GetPlatformFlags(MethodInfo methodInfo)
        {
            var flags = ToLuaPlatformFlags.All;

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
 
            return true;
        }
    }
}


