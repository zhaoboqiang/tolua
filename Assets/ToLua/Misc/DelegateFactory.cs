using System;
using System.Collections.Generic;

namespace LuaInterface
{
    [NoToLua]
    public class DelegateFactory
    {
        private static Dictionary<Type, DelegateCreate> dict => LuaSettingsUtility.Settings.delegates;
        static DelegateFactory factory = new DelegateFactory();

        public static Delegate CreateDelegate(Type t, LuaFunction func = null)
        {
            if (!dict.TryGetValue(t, out var Create))
            {
                throw new LuaException($"Delegate {LuaMisc.GetTypeName(t)} not register");
            }

            if (func != null)
            {
                var state = func.GetLuaState();
                var target = state.GetLuaDelegate(func);

                if (target != null)
                {
                    return Delegate.CreateDelegate(t, target, target.method);
                }
                else
                {
                    var d = Create(func, null, false);
                    target = d.Target as LuaDelegate;
                    state.AddLuaDelegate(target, func);
                    return d;
                }
            }

            return Create(null, null, false);
        }

        public static Delegate CreateDelegate(Type t, LuaFunction func, LuaTable self)
        {
            if (!dict.TryGetValue(t, out var Create))
            {
                throw new LuaException($"Delegate {LuaMisc.GetTypeName(t)} not register");
            }

            if (func != null)
            {
                var state = func.GetLuaState();
                var target = state.GetLuaDelegate(func, self);

                if (target != null)
                {
                    return Delegate.CreateDelegate(t, target, target.method);
                }
                else
                {
                    var d = Create(func, self, true);
                    target = d.Target as LuaDelegate;
                    state.AddLuaDelegate(target, func, self);
                    return d;
                }
            }

            return Create(null, null, true);
        }

        public static Delegate RemoveDelegate(Delegate obj, LuaFunction func)
        {
            var state = func.GetLuaState();
            var ds = obj.GetInvocationList();

            for (var i = 0; i < ds.Length; i++)
            {
                var ld = ds[i].Target as LuaDelegate;

                if (ld != null && ld.func == func)
                {
                    obj = Delegate.Remove(obj, ds[i]);
                    state.DelayDispose(ld.func);
                    break;
                }
            }

            return obj;
        }

        public static Delegate RemoveDelegate(Delegate obj, Delegate dg)
        {
            var remove = dg.Target as LuaDelegate;

            if (remove == null)
            {
                obj = Delegate.Remove(obj, dg);
                return obj;
            }

            var state = remove.func.GetLuaState();
            var ds = obj.GetInvocationList();

            for (var i = 0; i < ds.Length; i++)
            {
                var ld = ds[i].Target as LuaDelegate;

                if (ld != null && ld == remove)
                {
                    obj = Delegate.Remove(obj, ds[i]);
                    state.DelayDispose(ld.func);
                    state.DelayDispose(ld.self);
                    break;
                }
            }

            return obj;
        }
    }
}

