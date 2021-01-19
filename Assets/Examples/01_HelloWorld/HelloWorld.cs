using UnityEngine;
using LuaInterface;
using System;

[NoToLua]
public class HelloWorld : MonoBehaviour
{
    void Awake()
    {
        var L = new LuaState();
        L.Start();
        string hello =
            @"                
                print('hello tolua#')                                  
            ";
        
        L.DoString(hello, "HelloWorld.cs");
        L.CheckTop();
        L.Dispose();
    }
}
