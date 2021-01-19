using System;
using LuaInterface;

[NoToLua]
public static class TestProtol
{
    [LuaByteBufferAttribute]
    public static byte[] data; 
}
