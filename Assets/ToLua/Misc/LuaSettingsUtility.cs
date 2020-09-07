
using System;
using UnityEngine;

namespace LuaInterface
{
    public static class LuaSettingsUtility
    {
        public static LuaSettings Settings { get; private set; }

        public static void Initialize(LuaSettings settings)
        {
            Settings = settings;
        }

        public static T[] LoadCsv<T>(string fileName)
        {
            var text = System.IO.File.ReadAllText(Application.dataPath + "/Editor/" + fileName + ".csv");
            if (string.IsNullOrEmpty(text))
                return default;
            return CSVSerializer.Deserialize<T>(text);
        }
    }
}