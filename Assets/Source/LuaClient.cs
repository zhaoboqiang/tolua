/*
Copyright (c) 2015-2017 topameng(topameng@qq.com)

Permission is hereby granted, free of charge, to any person obtaining a copy
of this software and associated documentation files (the "Software"), to deal
in the Software without restriction, including without limitation the rights
to use, copy, modify, merge, publish, distribute, sublicense, and/or sell
copies of the Software, and to permit persons to whom the Software is
furnished to do so, subject to the following conditions:

The above copyright notice and this permission notice shall be included in all
copies or substantial portions of the Software.

THE SOFTWARE IS PROVIDED "AS IS", WITHOUT WARRANTY OF ANY KIND, EXPRESS OR
IMPLIED, INCLUDING BUT NOT LIMITED TO THE WARRANTIES OF MERCHANTABILITY,
FITNESS FOR A PARTICULAR PURPOSE AND NONINFRINGEMENT. IN NO EVENT SHALL THE
AUTHORS OR COPYRIGHT HOLDERS BE LIABLE FOR ANY CLAIM, DAMAGES OR OTHER
LIABILITY, WHETHER IN AN ACTION OF CONTRACT, TORT OR OTHERWISE, ARISING FROM,
OUT OF OR IN CONNECTION WITH THE SOFTWARE OR THE USE OR OTHER DEALINGS IN THE
SOFTWARE.
*/

using UnityEngine;
using LuaInterface;
using System.IO;
using System;
using UnityEngine.SceneManagement;

public class LuaClient : MonoBehaviour
{
    public static LuaClient Instance { get; private set; }

    private LuaState L = null;
    private LuaLooper loop = null;
    private LuaFunction levelLoaded = null;

    protected bool openLuaSocket = false;
    protected bool beZbStart = false;

    protected virtual LuaFileUtils InitLoader()
    {
        return LuaFileUtils.Instance;
    }

    protected virtual void LoadLuaFiles()
    {
        OnLoadFinished();
    }

    protected virtual void OpenLibs()
    {
        L.OpenLibs(LuaDLL.luaopen_pb);
        L.OpenLibs(LuaDLL.luaopen_struct);
        L.OpenLibs(LuaDLL.luaopen_lpeg);

        if (LuaConst.openLuaSocket)
        {
            OpenLuaSocket();
        }

        if (LuaConst.openLuaDebugger)
        {
            OpenZbsDebugger();
        }
    }

    public void OpenZbsDebugger(string ip = "localhost")
    {
        if (!Directory.Exists(LuaConst.zbsDir))
        {
            Debugger.LogWarning("ZeroBraneStudio not install or LuaConst.zbsDir not right");
            return;
        }

        if (!LuaConst.openLuaSocket)
        {
            OpenLuaSocket();
        }

        if (!string.IsNullOrEmpty(LuaConst.zbsDir))
        {
            L.AddSearchPath(LuaConst.zbsDir);
        }

        L.LuaDoString($"DebugServerIp = '{ip}'", "@LuaClient.cs");
    }

    [MonoPInvokeCallback(typeof(LuaCSFunction))]
    static int LuaOpen_Socket_Core(IntPtr L)
    {
        return LuaDLL.luaopen_socket_core(L);
    }

    [MonoPInvokeCallback(typeof(LuaCSFunction))]
    static int LuaOpen_Mime_Core(IntPtr L)
    {
        return LuaDLL.luaopen_mime_core(L);
    }

    private void OpenLuaSocket()
    {
        LuaConst.openLuaSocket = true;

        L.BeginPreLoad();
        L.RegFunction("socket.core", LuaOpen_Socket_Core);
        L.RegFunction("mime.core", LuaOpen_Mime_Core);
        L.EndPreLoad();
    }

    //cjson 比较特殊，只new了一个table，没有注册库，这里注册一下
    protected void OpenCJson()
    {
        L.LuaGetField(LuaIndexes.LUA_REGISTRYINDEX, "_LOADED");
        L.OpenLibs(LuaDLL.luaopen_cjson);
        L.LuaSetField(-2, "cjson");

        L.OpenLibs(LuaDLL.luaopen_cjson_safe);
        L.LuaSetField(-2, "cjson.safe");
    }

    protected virtual void CallMain()
    {
        var main = L.GetFunction("Main");
        main.Call();
        main.Dispose();
        main = null;
    }

    protected virtual void StartMain()
    {
        L.DoFile("Main.lua");
        levelLoaded = L.GetFunction("OnLevelWasLoaded");
        CallMain();
    }

    private void StartLooper()
    {
        loop = gameObject.AddComponent<LuaLooper>();
        loop.luaState = L;
    }

    protected virtual void Bind()
    {
        LuaSettingsUtility.Settings.luaRegister(L);
        LuaCoroutine.Register(L, this);
    }

    private void Init()
    {
        LuaSettingsUtility.Initialize(new LuaSettings
        {
            luaRegister = LuaBinder.Register,
            delegates = LuaDelegates.delegates,
            UsingCsvPath = Application.dataPath + "/Settings/Usings.csv" 
        });
        
        InitLoader();
        L = new LuaState();
        OpenLibs();
        L.LuaSetTop(0);
        Bind();
        LoadLuaFiles();
    }

    protected void Awake()
    {
        Instance = this;
        Init();

        SceneManager.sceneLoaded += OnSceneLoaded;
    }

    protected virtual void OnLoadFinished()
    {
        L.Start();
        StartLooper();
        StartMain();
    }

    void OnLevelLoaded(int level)
    {
        if (levelLoaded != null)
        {
            levelLoaded.BeginPCall();
            levelLoaded.Push(level);
            levelLoaded.PCall();
            levelLoaded.EndPCall();
        }

        if (L != null)
        {
            L.RefreshDelegateMap();
        }
    }

    void OnSceneLoaded(Scene scene, LoadSceneMode mode)
    {
        OnLevelLoaded(scene.buildIndex);
    }

    public virtual void Destroy()
    {
        if (L != null)
        {
            SceneManager.sceneLoaded -= OnSceneLoaded;

            L.Call("OnApplicationQuit", false);
            DetachProfiler();
            var state = L;
            L = null;

            if (levelLoaded != null)
            {
                levelLoaded.Dispose();
                levelLoaded = null;
            }

            if (loop != null)
            {
                loop.Destroy();
                loop = null;
            }

            state.Dispose();
            Instance = null;
        }
    }

    protected void OnDestroy()
    {
        Destroy();
    }

    protected void OnApplicationQuit()
    {
        Destroy();
    }

    public static LuaState GetMainState()
    {
        return Instance.L;
    }

    public LuaLooper GetLooper()
    {
        return loop;
    }

    LuaTable profiler = null;

    public void AttachProfiler()
    {
        if (profiler == null)
        {
            profiler = L.Require<LuaTable>("UnityEngine.Profiler");
            profiler.Call("start", profiler);
        }
    }

    public void DetachProfiler()
    {
        if (profiler != null)
        {
            profiler.Call("stop", profiler);
            profiler.Dispose();
            LuaProfiler.Clear();
        }
    }
}