﻿//this source code was auto-generated by tolua#, do not modify it
using System;
using LuaInterface;

[NoToLua]
public class TestProtolWrap
{
	public static void Register(LuaState L)
	{
		L.BeginStaticLibs("TestProtol");
		L.RegVar("data", get_data, set_data);
		L.EndStaticLibs();
	}

	[MonoPInvokeCallbackAttribute(typeof(LuaCSFunction))]
	static int get_data(IntPtr L)
	{
		try
		{
			LuaDLL.tolua_pushlstring(L, TestProtol.data, TestProtol.data.Length);
			return 1;
		}
		catch (Exception e)
		{
			return LuaDLL.toluaL_exception(L, e);
		}
	}

	[MonoPInvokeCallbackAttribute(typeof(LuaCSFunction))]
	static int set_data(IntPtr L)
	{
		try
		{
			byte[] arg0 = ToLua.CheckByteBuffer(L, 2);
			TestProtol.data = arg0;
			return 0;
		}
		catch (Exception e)
		{
			return LuaDLL.toluaL_exception(L, e);
		}
	}
}

