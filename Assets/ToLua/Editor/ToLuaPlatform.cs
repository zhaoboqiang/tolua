
using System;

namespace LuaInterface.Editor
{
    [System.Flags]
    public enum ToLuaPlatformFlags
    {
        None,
        Android,
        iOS,
        Editor,

        All = Android | iOS | Editor
    }

    public static class ToLuaPlatformUtility
    {
        public static ToLuaPlatformFlags From(bool android, bool iOS)
        {
            var flags = ToLuaPlatformFlags.Editor;

            if (android)
                flags |= ToLuaPlatformFlags.Android;
            
            if (iOS)
                flags |= ToLuaPlatformFlags.iOS;

            return flags;
        }
    } 
}
