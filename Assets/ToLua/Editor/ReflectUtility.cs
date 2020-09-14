
using System.Collections.Generic;
using System.IO;
using UnityEngine;

namespace LuaInterface.Editor
{
    public static class ReflectUtility
    {
        public static void SaveCsv(List<string> lines, string filepath)
        {
            File.WriteAllText(filepath, string.Join("\n", lines));
        }
    }
}
