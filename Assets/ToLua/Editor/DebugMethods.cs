using System;
using System.IO;
using System.Reflection;
using System.Text;
using System.Linq;
using UnityEditor;
using UnityEngine;

namespace LuaInterface.Editor
{
    public static class DebugMethods
    {
        private static void DebugMethod(StringBuilder sb, Type type, MethodInfo methodInfo)
        {
            sb.AppendLine($"GenericMethod:{methodInfo.IsGenericMethod}");
            sb.AppendLine($"GenericMethodDefinition:{methodInfo.IsGenericMethodDefinition}");

            var parameters = methodInfo.GetParameters();
            sb.AppendLine($"Parameters({parameters.Length})");
            for (int index = 0, count = parameters.Length; index < count; ++index)
            {
                var parameter = parameters[index];
                var parameterType = parameter.ParameterType;
                var parameterTypeName = ToLuaExport.GetGenericParameterType(methodInfo, parameterType);
                sb.AppendLine($"\t[{index}]:");
                sb.AppendLine($"\t\t{parameterType.Name}, {parameterTypeName}, {parameter.Name}, {(parameterType.IsGenericParameter ? parameterType.GenericParameterPosition : -1)}");
                sb.AppendLine($"\t\t{parameter.DefaultValue}");
            }

            var genericParameters = methodInfo.GetGenericArguments();
            sb.AppendLine($"GenericParameters({genericParameters.Length})");
            for (int index = 0, count = genericParameters.Length; index < count; ++index)
            {
                var parameter = genericParameters[index];

                var constraints = parameter.GetGenericParameterConstraints();

                sb.AppendLine($"\t[{index}]:{parameter.GetType().Name}, {parameter.Name}, {parameter.GenericParameterPosition}, {constraints.Length}");

                for (int constraintIndex = 0, constraintCount = constraints.Length; constraintIndex < constraintCount; ++constraintIndex)
                {
                    var constraint = constraints[constraintIndex];
                    sb.AppendLine($"\t\t[{constraintIndex}]:{constraint.Name}");
                }
            }

            var customAttributes = methodInfo.GetCustomAttributes().ToArray();
            for (int index = 0, count = customAttributes.Length; index < count; ++index)
            {
                var customAttribute = customAttributes[index];
                sb.AppendLine($"\t[{index}]:{customAttribute}");
            }

            /*
            var genericName = LuaMisc.GetGenericName(methodInfo);
            sb.AppendLine($"{genericName}");
            */

        }

        private static void DebugMethod(Type type, string methodName)
        {
            var sb = new StringBuilder();

            sb.AppendLineEx($"{methodName}");
            sb.AppendLineEx($"{type.AssemblyQualifiedName}");
            sb.AppendLineEx();

            var methods = type.GetMethods(BindingFlags.Instance | BindingFlags.Public | BindingFlags.Static | BindingFlags.IgnoreCase);
            foreach (var method in methods)
            {
                if (method.Name == methodName)
                {
                    DebugMethod(sb, type, method);
                    sb.AppendLineEx();
                }
            }

            File.WriteAllText($"{Application.dataPath}/Editor/debug_method.txt", sb.ToString());
        }

        [MenuItem("Reflect/Debug methods")]
        public static void Main()
        {
            DebugMethod(typeof(System.Collections.Generic.Dictionary<string,string>), "Remove");
        }
    }
}
