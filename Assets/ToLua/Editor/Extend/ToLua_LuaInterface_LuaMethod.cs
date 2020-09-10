using System;
using LuaInterface;

public class ToLua_LuaInterface_LuaMethod
{
    public static string CallDefined =
@"		try
		{			
			var obj = (LuaMethod)ToLua.CheckObject(L, 1, typeof(LuaMethod));            
			return obj.Call(L);						
		}
		catch(Exception e)
		{
			return LuaDLL.toluaL_exception(L, e);
		}";

    public static string DestroyDefined =
@"		try
		{
			ToLua.CheckArgsCount(L, 1);
			var obj = (LuaMethod)ToLua.CheckObject(L, 1, typeof(LuaMethod));
			obj.Destroy();
            ToLua.Destroy(L);
			return 0;
		}
		catch(Exception e)
		{
			return LuaDLL.toluaL_exception(L, e);
		}";

    [UseDefined]
    public int Call(IntPtr L)
    {
        return 0;
    }

    [UseDefined]
    public void Destroy()
    {

    }
}
