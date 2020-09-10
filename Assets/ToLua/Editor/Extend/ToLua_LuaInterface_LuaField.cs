using System;
using LuaInterface;

public class ToLua_LuaInterface_LuaField
{
    public static string GetDefined =
@"		try
		{			
			var obj = (LuaField)ToLua.CheckObject(L, 1, typeof(LuaField));            
            return obj.Get(L);						
		}
		catch (Exception e)
		{
			return LuaDLL.toluaL_exception(L, e);
		}";

    public static string SetDefined =
@"		try
		{			
            LuaField obj = (LuaField)ToLua.CheckObject(L, 1, typeof(LuaField));            
            return obj.Set(L);
        }
        catch (Exception e)
		{
			return LuaDLL.toluaL_exception(L, e);
		}";

    [UseDefined]
    public int Set(IntPtr L)
    {
        return 0;
    }

    [UseDefined]
    public int Get(IntPtr L)
    {
        return 0;
    }
}
