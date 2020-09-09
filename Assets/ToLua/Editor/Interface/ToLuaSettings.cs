using System;
using System.Collections.Generic;

namespace LuaInterface
{

    public interface ToLuaSettings
    {
        string saveDir { get; }
        string toluaBaseType { get; }
        string baseLuaDir { get; }
        string injectionFilesPath { get; }

        string IncludedAssemblyCsv { get; } 
        string IncludedNamespaceCsv { get; } 
        string IncludedTypeCsv { get; } 
    }
}