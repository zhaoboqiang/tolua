using System;
using System.Collections.Generic;
using LuaInterface;

public static class LuaBinder
{
	public static void Register(LuaState L)
	{
		throw new LuaException("Please generate LuaBinder files first!");
	}

	public static Dictionary<string, LuaBinderRuntime.Item> Binders = new Dictionary<string, LuaBinderRuntime.Item>()
	{
	};
}
