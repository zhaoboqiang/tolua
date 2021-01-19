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
using System;
using UnityEngine;
using LuaInterface;

public class LuaLooper : MonoBehaviour 
{    
    public LuaBeatEvent UpdateEvent
    {
        get;
        private set;
    }

    public LuaBeatEvent LateUpdateEvent
    {
        get;
        private set;
    }

    public LuaBeatEvent FixedUpdateEvent
    {
        get;
        private set;
    }

    public LuaState L = null;

    private void Start() 
    {
        try
        {
            UpdateEvent = GetEvent("UpdateBeat");
            LateUpdateEvent = GetEvent("LateUpdateBeat");
            FixedUpdateEvent = GetEvent("FixedUpdateBeat");
        }
        catch (Exception e)
        {
            Destroy(this);
            throw e;
        }        
	}

    private LuaBeatEvent GetEvent(string name)
    {
        var table = L.GetTable(name);

        if (table == null)
            throw new LuaException($"Lua table {name} not exists");

        var e = new LuaBeatEvent(table);
        table.Dispose();
        table = null;
        return e;
    }

    private void ThrowException()
    {
        var error = L.LuaToString(-1);
        L.LuaPop(2);                
        throw new LuaException(error, LuaException.GetLastError());
    }

    private void Update()
    {
        if (Application.isEditor)
        {
            if (L == null)
            {
                return;
            }
        }

        if (L.LuaUpdate(Time.deltaTime, Time.unscaledDeltaTime) != 0)
            ThrowException();

        L.LuaPop(1);
        L.Collect();
        if (Application.isEditor)
        {
            L.CheckTop();
        }
    }

    private void LateUpdate()
    {
        if (Application.isEditor)
        {
            if (L == null)
            {
                return;
            }
        }

        if (L.LuaLateUpdate() != 0)
            ThrowException();

        L.LuaPop(1);
    }

    private void FixedUpdate()
    {
        if (Application.isEditor)
        {
            if (L == null)
            {
                return;
            }
        }

        if (L.LuaFixedUpdate(Time.fixedDeltaTime) != 0)
            ThrowException();

        L.LuaPop(1);
    }

    public void Destroy()
    {
        if (L == null)
            return;
        
        if (UpdateEvent != null)
        {
            UpdateEvent.Dispose();
            UpdateEvent = null;
        }

        if (LateUpdateEvent != null)
        {
            LateUpdateEvent.Dispose();
            LateUpdateEvent = null;
        }

        if (FixedUpdateEvent != null)
        {
            FixedUpdateEvent.Dispose();
            FixedUpdateEvent = null;
        }

        L = null;
    }

    private void OnDestroy()
    {
        if (L != null)
        {
            Destroy();
        }
    }
}
