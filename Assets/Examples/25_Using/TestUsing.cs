using UnityEngine;
using System.IO;
using LuaInterface;

//use menu Lua->Copy lua files to Resources. 之后才能发布到手机
[NoToLua]
public class TestUsing : LuaClient 
{
    string tips = "Test Using";

    protected override LuaFileUtils InitLoader()
    {
        return new LuaResLoader();
    }

    protected override void CallMain()
    {
        var test0 = L.GetFunction("Test0");
        test0.Call();
        test0.Dispose();

        var test1 = L.GetFunction("Test1");
        test1.Call();
        test1.Dispose();
    }

    protected override void StartMain()
    {
        L.DoFile("TestUsing.lua");
        CallMain();
    }

    new void Awake()
    {
        Application.logMessageReceived += Logger;
        base.Awake();
    }

    new void OnApplicationQuit()
    {
        base.OnApplicationQuit();

        Application.logMessageReceived -= Logger;
    }

    void Logger(string msg, string stackTrace, LogType type)
    {
        tips += msg;
        tips += "\r\n";
    }

    void OnGUI()
    {
        GUI.Label(new Rect(Screen.width / 2 - 200, Screen.height / 2 - 200, 400, 400), tips);
    }
}
