using System;

namespace LuaInterface
{
    public delegate Delegate DelegateCreate(LuaFunction func, LuaTable self, bool flag);
}