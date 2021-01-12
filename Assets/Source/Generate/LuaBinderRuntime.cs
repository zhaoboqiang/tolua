using LuaInterface;

public static class LuaBinderRuntime
{
	public delegate void Binder(LuaState L);

	public struct Item
	{
		public int UsingCount; 
		public Binder Binder;
	};

	public static void Register(LuaState L)
	{
		L.BeginModule(null);
		LuaBinderWrap.Register(L);
		L.EndModule();
	}
}
