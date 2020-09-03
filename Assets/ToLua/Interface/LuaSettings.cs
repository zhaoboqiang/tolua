using System;
using System.Collections.Generic;

namespace LuaInterface
{
    public struct LuaSettings
    {
        public LuaRegister luaRegister;

        public Dictionary<Type, DelegateCreate> delegates;
    }
}