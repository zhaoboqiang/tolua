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

        string ExcludeAssemblyCsv { get; } 

        Type[] dynamicList { get; }

        //重载函数，相同参数个数，相同位置out参数匹配出问题时, 需要强制匹配解决
        //使用方法参见例子14
        List<Type> outList { get; }

        //ngui优化，下面的类没有派生类，可以作为sealed class
        List<Type> sealedList { get; }
    }
}