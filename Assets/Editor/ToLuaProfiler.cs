using UnityEditor;
using UnityEngine;

namespace LuaInterface.Editor
{
    public class ToLuaProfiler
    {
        [MenuItem("Lua/Attach Profiler", false, 151)]
        static void AttachProfiler()
        {
            if (!Application.isPlaying)
            {
                EditorUtility.DisplayDialog("警告", "请在运行时执行此功能", "确定");
                return;
            }

            LuaClient.Instance.AttachProfiler();
        }

        [MenuItem("Lua/Detach Profiler", false, 152)]
        static void DetachProfiler()
        {
            if (!Application.isPlaying)
            {
                return;
            }

            LuaClient.Instance.DetachProfiler();
        }
    }
}