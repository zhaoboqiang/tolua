
using System;
using System.Text;

namespace LuaInterface.Editor
{
    [System.Flags]
    public enum ToLuaPlatformFlags
    {
        None = 0,
        Android = 1,
        iOS = 2,
        Editor = 4,

        All = Android | iOS | Editor
    }

    public static class ToLuaPlatformUtility
    {
        public static ToLuaPlatformFlags From(bool android, bool iOS, bool editor)
        {
            var flags = ToLuaPlatformFlags.None;

            if (android)
                flags |= ToLuaPlatformFlags.Android;
            
            if (iOS)
                flags |= ToLuaPlatformFlags.iOS;
            
            if (editor)
                flags |= ToLuaPlatformFlags.Editor;

            return flags;
        }

        public static string GetText(ToLuaPlatformFlags flags)
        {
            var text = string.Empty;

            if ((flags & ToLuaPlatformFlags.All) == ToLuaPlatformFlags.All)
                return text;

            if ((flags & ToLuaPlatformFlags.iOS) != ToLuaPlatformFlags.None)
                text += "UNITY_IOS";

            if ((flags & ToLuaPlatformFlags.Android) != ToLuaPlatformFlags.None)
            {
                if (text != string.Empty)
                    text += " || ";
                text += "UNITY_ANDROID";
            }

            if ((flags & ToLuaPlatformFlags.Editor) != ToLuaPlatformFlags.None)
            {
                if (text != string.Empty)
                    text += " || ";
                text += "UNITY_EDITOR";
            }

            return text;
        }

        public static void BeginPlatformMacro(StringBuilder sb, string flags)
        {
            if (flags != string.Empty)
            {
                sb.AppendLineEx($"#if {flags}");
            }
        }

        public static void EndPlatformMacro(StringBuilder sb, string flags)
        {
            if (flags != string.Empty)
            {
                sb.AppendLineEx($"#endif // {flags}");
            }
        }
    }
}
