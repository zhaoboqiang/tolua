using System.Reflection;

namespace LuaInterface.Editor
{
    public static class ToLuaMembers
    {
        public static string GetTypeName(MemberInfo memberInfo)
        {
            return ToLuaTypes.GetFullName(memberInfo.ReflectedType);
        }

        public static string GetName(MemberInfo memberInfo)
        {
            return GetTypeName(memberInfo) + "." + memberInfo.Name;
        }
    }
}