using System;
using System.Collections.Generic;
using System.Linq;
using UnityEditor;

namespace LuaInterface.Editor
{
    public static class ReflectUsings
    {
        private static Dictionary<string, LuaUsingSetting> settings;

        public static Dictionary<string, LuaUsingSetting> Settings
        {
            get
            {
                if (settings == null)
                {
                    var settingArray = LuaSettingsUtility.LoadCsv<LuaUsingSetting>(LuaSettingsUtility.Settings.UsingCsvPath, ':');
                    if (settingArray == null)
                        settings = new Dictionary<string, LuaUsingSetting>();
                    else
                        settings = settingArray.ToDictionary(key => key.FullName);
                }
                return settings;
            }
        }

        public static void Reset()
        {
            settings = null;
        }

        private static void UpdateCsv(List<LuaUsingSetting> newSettings)
        {
            // Load previous configurations
            var oldSettings = Settings;

            // Merge previous configurations
            for (int index = 0, count = newSettings.Count; index < count; ++index)
            {
                var newSetting = newSettings[index];

                if (oldSettings.TryGetValue(newSetting.FullName, out var oldSetting))
                {
                    newSettings[index] = oldSetting;
                }
            }

            // Merge not exist previous configurations
            var mergedSettings = newSettings.ToDictionary(key => key.FullName);
            foreach (var kv in oldSettings)
            {
                if (mergedSettings.ContainsKey(kv.Key))
                    continue;

                mergedSettings.Add(kv.Key, kv.Value);
            }

            // Sort
            var resultSettings = mergedSettings.Values.ToList();
            resultSettings.Sort((lhs, rhs) => lhs.FullName.CompareTo(rhs.FullName));

            // Save configurations
            var lines = new List<string> { "FullName:Preload" };
            lines.AddRange(from setting in resultSettings
                           select $"{setting.FullName}:{setting.Preload}");
            ReflectUtility.SaveCsv(lines, ToLuaSettingsUtility.Settings.UsingCsv);
        }
        
        private static void AddNewSetting(List<LuaUsingSetting> newSettings, Type type)
        {
            newSettings.Add(new LuaUsingSetting
            {
                FullName = ToLuaTypes.GetFullName(type),
                Preload = false
            });
        }

        [MenuItem("Reflect/Update usings")]
        public static void Print()
        {
            var newSettings = new List<LuaUsingSetting>();

            foreach (var type in ToLuaSettingsUtility.Types)
                AddNewSetting(newSettings, type);

            foreach (var type in ToLuaSettingsUtility.DelegateTypes)
                AddNewSetting(newSettings, type);

            UpdateCsv(newSettings);
        }
    }
}
