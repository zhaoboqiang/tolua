using System;
using System.Collections.Generic;
using LuaInterface;

public static class LuaBinder
{
	public static Dictionary<string, LuaBinderRuntime.Type> TypeBinders = new Dictionary<string, LuaBinderRuntime.Type>()
	{
	};

	public static Dictionary<string, LuaBinderRuntime.Function> FunctionBinders = new Dictionary<string, LuaBinderRuntime.Function>()
	{
	};
}
