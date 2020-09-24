using System;
using System.IO;
using System.Reflection;
using System.Text;
using UnityEditor;
using UnityEngine;

namespace LuaInterface.Editor
{
    public static class DebugMethods
    {
        
        private static void DebugMethod(Type type, MethodInfo methodInfo)
        {
            var sb = new StringBuilder();
            sb.AppendLine($"GenericMethod:{methodInfo.IsGenericMethod}");
            sb.AppendLine($"GenericMethodDefinition:{methodInfo.IsGenericMethodDefinition}");

            var parameters = methodInfo.GetParameters();
            sb.AppendLine($"Parameters({parameters.Length})");
            for (int index = 0, count = parameters.Length; index < count; ++index)
            {
                var parameter = parameters[index];
                var parameterType = parameter.ParameterType;
                sb.AppendLine($"\t[{index}]:{parameterType.Name}, {parameter.Name}, {(parameterType.IsGenericParameter ? parameterType.GenericParameterPosition : -1)}");
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
                    sb.AppendLine($"\t\t[{constraintIndex}]:{constraint.GetType().Name}, {constraint.Name}");
                }
            }

            File.WriteAllText($"{Application.dataPath}/Editor/debug_method.txt", sb.ToString());
        }

        private static void DebugMethod(Type type, string methodName)
        {
            var methods = type.GetMethods(BindingFlags.Instance | BindingFlags.Public | BindingFlags.Static | BindingFlags.IgnoreCase);
            var method = type.GetMethod(methodName);
            DebugMethod(type, method);
        }

        [MenuItem("Reflect/Debug methods")]
        public static void Main()
        {
            DebugMethod(typeof(UnityEngine.Playables.PlayableGraph), "Connect");
        }
    }
}
