using LuaInterface;
using UnityEngine;

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

		var settings = LuaSettingsUtility.LoadCsv<LuaUsingSetting>(LuaSettingsUtility.Settings.UsingCsvPath, ':');

        float t = Time.realtimeSinceStartup;

		foreach (var setting in settings)
		{
			if (setting.Preload)
			{
				LuaBinderWrap.Using(L, setting.FullName);
			}
		}

        Debugger.Log("Preregister lua type cost time: {0}", Time.realtimeSinceStartup - t);
	}
}
