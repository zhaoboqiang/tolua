using UnityEngine;
using System.Collections;
using LuaInterface;

public class ToLua_UnityEngine_Object     
{        
    public static string DestroyDefined =
@"        try
        {
            int count = LuaDLL.lua_gettop(L);

            if (count == 1)
            {
                var arg0 = (UnityEngine.Object)ToLua.CheckObject<UnityEngine.Object>(L, 1);
                ToLua.Destroy(L);
                UnityEngine.Object.Destroy(arg0);
                return 0;
            }
            else if (count == 2)
            {                
                var arg1 = (float)LuaDLL.luaL_checknumber(L, 2);
                var udata = LuaDLL.tolua_rawnetobj(L, 1);
                var translator = LuaState.GetTranslator(L);
                translator.DelayDestroy(udata, arg1);
                return 0;
            }
            else
            {
                return LuaDLL.luaL_throw(L, ""invalid arguments to method: Object.Destroy"");
            }
        }
        catch (Exception e)
        {
            return LuaDLL.toluaL_exception(L, e);
        }";
    
    public static string DestroyImmediateDefined =
@"        try
        {
            int count = LuaDLL.lua_gettop(L);

            if (count == 1)
            {
                var arg0 = (UnityEngine.Object)ToLua.CheckObject<UnityEngine.Object>(L, 1);
                ToLua.Destroy(L);
                UnityEngine.Object.DestroyImmediate(arg0);
                return 0;
            }
            else if (count == 2)
            {
                var arg0 = (UnityEngine.Object)ToLua.CheckObject<UnityEngine.Object>(L, 1);
                var arg1 = LuaDLL.luaL_checkboolean(L, 2);
                ToLua.Destroy(L);
                UnityEngine.Object.DestroyImmediate(arg0, arg1);
                return 0;
            }
            else
            {
                return LuaDLL.luaL_throw(L, ""invalid arguments to method: Object.DestroyImmediate"");
            }
        }
        catch (Exception e)
        {
            return LuaDLL.toluaL_exception(L, e);
        }";

    public static string InstantiateDefined =
@"		IntPtr L0 = LuaException.L;

        try
        {
            ++LuaException.InstantiateCount;            
            LuaException.L = L;
            int count = LuaDLL.lua_gettop(L);

            if (count == 1)
            {
                var arg0 = (UnityEngine.Object)ToLua.CheckObject<UnityEngine.Object>(L, 1);
                var o = UnityEngine.Object.Instantiate(arg0);

                if (LuaDLL.lua_toboolean(L, LuaDLL.lua_upvalueindex(1)))
                {
                    var error = LuaDLL.lua_tostring(L, -1);
                    LuaDLL.lua_pop(L, 1);
                    throw new LuaException(error, LuaException.GetLastError());
                }
                else
                {
                    ToLua.Push(L, o);
                }

                LuaException.L = L0;
                --LuaException.InstantiateCount;
                return 1;
            }
#if UNITY_5_4_OR_NEWER
            else if (count == 2)
            {
                var arg0 = (UnityEngine.Object)ToLua.CheckObject<UnityEngine.Object>(L, 1);
                var arg1 = (UnityEngine.Transform)ToLua.CheckObject<UnityEngine.Transform>(L, 2);
                var o = UnityEngine.Object.Instantiate(arg0, arg1);

                if (LuaDLL.lua_toboolean(L, LuaDLL.lua_upvalueindex(1)))
                {
                    string error = LuaDLL.lua_tostring(L, -1);
                    LuaDLL.lua_pop(L, 1);
                    throw new LuaException(error, LuaException.GetLastError());
                }
                else
                {
                    ToLua.Push(L, o);
                }

                LuaException.L = L0;
                --LuaException.InstantiateCount;
                return 1;
            }
#endif
            else if (count == 3 && TypeChecker.CheckTypes<UnityEngine.Vector3, UnityEngine.Quaternion>(L, 2))
            {
                var arg0 = (UnityEngine.Object)ToLua.CheckObject<UnityEngine.Object>(L, 1);
                var arg1 = ToLua.ToVector3(L, 2);
                var arg2 = ToLua.ToQuaternion(L, 3);
                var o = UnityEngine.Object.Instantiate(arg0, arg1, arg2);

                if (LuaDLL.lua_toboolean(L, LuaDLL.lua_upvalueindex(1)))
                {
                    var error = LuaDLL.lua_tostring(L, -1);
                    LuaDLL.lua_pop(L, 1);
                    throw new LuaException(error, LuaException.GetLastError());
                }
                else
                {
                    ToLua.Push(L, o);
                }

                LuaException.L = L0;
                --LuaException.InstantiateCount;
                return 1;
            }
#if UNITY_5_4_OR_NEWER
            else if (count == 3 && TypeChecker.CheckTypes<UnityEngine.Transform, bool>(L, 2))
            {
                var arg0 = (UnityEngine.Object)ToLua.CheckObject<UnityEngine.Object>(L, 1);
                var arg1 = (UnityEngine.Transform)ToLua.ToObject(L, 2);
                var arg2 = LuaDLL.lua_toboolean(L, 3);
                var o = UnityEngine.Object.Instantiate(arg0, arg1, arg2);

                if (LuaDLL.lua_toboolean(L, LuaDLL.lua_upvalueindex(1)))
                {
                    var error = LuaDLL.lua_tostring(L, -1);
                    LuaDLL.lua_pop(L, 1);
                    throw new LuaException(error, LuaException.GetLastError());
                }
                else
                {
                    ToLua.Push(L, o);
                }

                LuaException.L = L0;
                --LuaException.InstantiateCount;
                return 1;
            }
            else if (count == 4)
            {
                var arg0 = (UnityEngine.Object)ToLua.CheckObject<UnityEngine.Object>(L, 1);
                var arg1 = ToLua.CheckVector3(L, 2);
                var arg2 = ToLua.CheckQuaternion(L, 3);
                var arg3 = (UnityEngine.Transform)ToLua.CheckObject<UnityEngine.Transform>(L, 4);
                var o = UnityEngine.Object.Instantiate(arg0, arg1, arg2, arg3);

                if (LuaDLL.lua_toboolean(L, LuaDLL.lua_upvalueindex(1)))
                {
                    var error = LuaDLL.lua_tostring(L, -1);
                    LuaDLL.lua_pop(L, 1);
                    throw new LuaException(error, LuaException.GetLastError());
                }
                else
                {
                    ToLua.Push(L, o);
                }

                LuaException.L = L0;
                --LuaException.InstantiateCount;
                return 1;
            }
#endif
            else
            {
                LuaException.L = L0;
                --LuaException.InstantiateCount;
                return LuaDLL.luaL_throw(L, ""invalid arguments to method: UnityEngine.Object.Instantiate"");
            }
        }
        catch (Exception e)
        {
            LuaException.L = L0;
            --LuaException.InstantiateCount;
            return LuaDLL.toluaL_exception(L, e);
        }";


    [UseDefined]
    public static void Destroy(Object obj)
    {
        
    }

    [UseDefined]
    public static void DestroyImmediate(Object obj)
    {

    }

    [NoToLua]
    public static void DestroyObject(Object obj)
    {

    }

    [NoToLua]
    public static void DestroyObject(Object obj, float t)
    {

    }

    [UseDefined]
    public static void Instantiate(Object original)
    {

    }
}
