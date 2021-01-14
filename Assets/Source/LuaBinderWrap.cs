using System;
using LuaInterface;

public static class LuaBinderWrap
{
    static LuaState L;

    private static bool Using(LuaState L, string wrapFullName)
    {
        if (!LuaBinder.Binders.TryGetValue(wrapFullName, out var item))
            return false;

        if (item.UsingCount++ > 0)
            return true;

        // prepare namespace
        var delimiterIndex = wrapFullName.IndexOf('<');
        if (delimiterIndex > 0)
        {
            // strip template <>
            wrapFullName = wrapFullName.Substring(0, delimiterIndex);
        }

        var names = wrapFullName.Split('.');
        var count = names.Length - 1;

		L.BeginModule(null); // empty namesapce

        for (int index = 0; index < count; ++index)
            L.BeginModule(names[index]);

        // register class
        item.Binder(L);

        // end namespace
        for (int index = 0; index < count; ++index)
            L.EndModule();

		L.EndModule(); // empty namesapce

        return true;
    }

    public static void Register(LuaState L)
	{
        LuaBinderWrap.L = L;

		L.RegFunction("using", Using);
	}

	[MonoPInvokeCallback(typeof(LuaCSFunction))]
	static int Using(IntPtr L)
	{
		try
		{
			ToLua.CheckArgsCount(L, 1);
			var arg0 = ToLua.CheckString(L, 1);
			var o = Using(LuaBinderWrap.L, arg0);
			LuaDLL.lua_pushboolean(L, o);

			return 1;
		}
		catch (Exception e)
		{
			return LuaDLL.toluaL_exception(L, e);
		}
	}
}

