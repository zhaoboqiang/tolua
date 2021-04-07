using System;

namespace LuaInterface
{
    public interface ToLuaSettings
    {
        string SaveDir { get; }
        string WrapperSaveDir { get; }
        string ToluaBaseType { get; }
        string baseLuaDir { get; }
        string injectionFilesPath { get; }

        string AssemblyCsv { get; } 
        string NamespaceCsv { get; } 
        string TypeCsv { get; } 
        string FieldCsv { get; } 
        string MethodCsv { get; } 
        string PropertyCsv { get; } 
        string UsingCsv { get; }

        Type[] DelegateTypes { get; }
    }
}