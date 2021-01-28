using System;
using LuaInterface;

public static class LuaBinderWrap
{
    public static bool UsingType(LuaState L, string wrapFullName)
    {
        if (!LuaBinder.TypeBinders.TryGetValue(wrapFullName, out var item))
            return false;

        if (item.UsingCount++ > 0)
            return true;

        // prepare namespace
        var delimiterIndex = wrapFullName.IndexOf('<'); // handle this condition: ns0.ns1.ns2.t<ns3.t>
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

    private static string RemoveChar(string str, char c)
    {
        int index = str.IndexOf(c);

        while (index > 0)
        {
            str = str.Remove(index, 1);
            index = str.IndexOf(c);
        }

        return str;
    }

    private static string NormalizeName(string str)
    {
        str = str.Replace('<', '_');
        str = RemoveChar(str, '>');
        str = str.Replace('.', '_');
        return str.Replace(',', '_');
    }

    public static bool UsingFunction(LuaState L, string wrapFullName)
    {
        if (!LuaBinder.FunctionBinders.TryGetValue(wrapFullName, out var item))
            return false;

        if (item.UsingCount++ > 0)
            return true;

        var fullName = wrapFullName;

        // prepare namespace
        var delimiterIndex = fullName.IndexOf('<'); // handle this condition: ns0.ns1.ns2.t<ns3.t>
        if (delimiterIndex > 0)
            fullName = fullName.Substring(0, delimiterIndex); // strip template <>

        var names = fullName.Split('.');
        var count = names.Length - 1;

        var name = names[count];
        if (delimiterIndex > 0)
            name += NormalizeName(wrapFullName.Substring(delimiterIndex));

		L.BeginModule(null); // empty namesapce

        for (int index = 0; index < count; ++index)
            L.BeginModule(names[index]);

        // register function
        L.RegFunction(name, item.Binder);

        // end namespace
        for (int index = 0; index < count; ++index)
            L.EndModule();

		L.EndModule(); // empty namesapce

        return true;
    }

    public static bool Using(LuaState L, string wrapFullName)
    {
        if (UsingType(L, wrapFullName))
            return true;
        return UsingFunction(L, wrapFullName);
    }

    public static void Register(LuaState L)
	{
		L.RegFunction("using", Using);
	}

	[MonoPInvokeCallback(typeof(LuaCSFunction))]
	static int Using(IntPtr L)
	{
		try
		{
			ToLua.CheckArgsCount(L, 1);
			var arg0 = ToLua.CheckString(L, 1);
			var o = Using(LuaState.Get(L), arg0);
			LuaDLL.lua_pushboolean(L, o);

			return 1;
		}
		catch (Exception e)
		{
			return LuaDLL.toluaL_exception(L, e);
		}
	}
}

