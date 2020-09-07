using UnityEngine;
using System;
using System.Collections.Generic;
using LuaInterface;
using LuaInterface.Editor;
using UnityEditor;

[InitializeOnLoad]
public class CustomToLuaSettings : ToLuaSettings
{
    static CustomToLuaSettings()
    {
        ToLuaSettingsUtility.Initialize(new CustomToLuaSettings());

        LuaSettingsUtility.Initialize(new LuaSettings
        {
            luaRegister = LuaBinder.Register,
            delegates = LuaDelegates.delegates
        });
    }

    public string saveDir => Application.dataPath + "/Source/Generate/";
    public string toluaBaseType => Application.dataPath + "/ToLua/BaseType/";
    public string baseLuaDir => Application.dataPath + "/ToLua/Lua/";
    public string injectionFilesPath => Application.dataPath + "/ToLua/Injection/";

    public string IncludedAssemblyCsv => "included_assembly";

    public Type[] dynamicList => new[]
    {
        typeof(MeshRenderer),
        typeof(BoxCollider),
        typeof(MeshCollider),
        typeof(SphereCollider),
        typeof(CharacterController),
        typeof(CapsuleCollider),
        typeof(Animation),
        typeof(AnimationClip),
        typeof(AnimationState),
        typeof(SkinWeights),
        typeof(RenderTexture),
        typeof(Rigidbody),
    };

    //重载函数，相同参数个数，相同位置out参数匹配出问题时, 需要强制匹配解决
    //使用方法参见例子14
    public List<Type> outList => new List<Type>()
    {
    };

    //ngui优化，下面的类没有派生类，可以作为sealed class
    public List<Type> sealedList => new List<Type>()
    {
        /*typeof(Transform),
        typeof(UIRoot),
        typeof(UICamera),
        typeof(UIViewport),
        typeof(UIPanel),
        typeof(UILabel),
        typeof(UIAnchor),
        typeof(UIAtlas),
        typeof(UIFont),
        typeof(UITexture),
        typeof(UISprite),
        typeof(UIGrid),
        typeof(UITable),
        typeof(UIWrapGrid),
        typeof(UIInput),
        typeof(UIScrollView),
        typeof(UIEventListener),
        typeof(UIScrollBar),
        typeof(UICenterOnChild),
        typeof(UIScrollView),        
        typeof(UIButton),
        typeof(UITextList),
        typeof(UIPlayTween),
        typeof(UIDragScrollView),
        typeof(UISpriteAnimation),
        typeof(UIWrapContent),
        typeof(TweenWidth),
        typeof(TweenAlpha),
        typeof(TweenColor),
        typeof(TweenRotation),
        typeof(TweenPosition),
        typeof(TweenScale),
        typeof(TweenHeight),
        typeof(TypewriterEffect),
        typeof(UIToggle),
        typeof(Localization),*/
    };
}