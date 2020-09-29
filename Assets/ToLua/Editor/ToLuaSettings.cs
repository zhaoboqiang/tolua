using System;
using System.Collections.Generic;

namespace LuaInterface
{
    public interface ToLuaSettings
    {
        string SaveDir { get; }
        string WrapperSaveDir { get; }
        string ToluaBaseType { get; }
        string baseLuaDir { get; }
        string injectionFilesPath { get; }

        string IncludedAssemblyCsv { get; } 
        string IncludedNamespaceCsv { get; } 
        string IncludedTypeCsv { get; } 
        string IncludedMethodCsv { get; } 
        string IncludedEnumCsv { get; } 
    }
}