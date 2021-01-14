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

		var settings = LuaSettingsUtility.LoadCsv<LuaUsingSetting>(LuaSettingsUtility.Settings.UsingCsvPath);

		foreach (var setting in settings)
		{
			if (setting.Preload)
			{
				LuaBinderWrap.Using(L, setting.FullName);
			}
		}
	}
}
