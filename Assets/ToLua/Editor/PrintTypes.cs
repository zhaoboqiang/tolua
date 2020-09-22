using System;
using System.Collections.Generic;
using UnityEditor;
using UnityEngine;

namespace LuaInterface.Editor
{
    public static class PrintTypes
    {
        private static void SaveCsv(List<Type> types)
        {
            var lines = new List<string> { "FullName,Namespace,Name,Public,NestedPublic,Generic,Abstract" };
            foreach (var type in types)
                lines.Add($"{type.FullName},{type.Namespace},{type.Name},{type.IsPublic},{type.IsNestedPublic},{type.IsGenericType},{type.IsAbstract}");
            ReflectUtility.SaveCsv(lines, Application.dataPath + "/Editor/all_types.csv");
        }

        [MenuItem("Print/Types")]
        public static void Print()
        {
            var assemblies = AppDomain.CurrentDomain.GetAssemblies();
            var types = new List<Type>();

            foreach (var assembly in assemblies)
            {
                foreach (var type in assembly.GetTypes())
                {
                    types.Add(type);
                }
            }

            SaveCsv(types);
        }
    }
}
