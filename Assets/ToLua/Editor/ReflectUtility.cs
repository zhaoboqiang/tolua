
using System.Collections.Generic;
using System.IO;
using UnityEngine;

namespace LuaInterface.Editor
{
    public static class ReflectUtility
    {
        private static string GetCsvFilepath(string filename)
        {
            return Application.dataPath + "/Editor/" + filename + ".csv";
        }

        public static void SaveCsv(List<string> lines, string filename)
        {
            var filepath = GetCsvFilepath(filename);
            File.WriteAllText(filepath, string.Join("\n", lines));
        }
    }
}
