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

    public string[] ExcludedAssemblies
    {
        get
        {
            return new[]
            {
                "Assembly-CSharp-Editor",
                "CString",
                "Debugger",
                "LuaInterface.Editor",
                "ExCSS.Unity",
                "JetBrains.Rider.Unity.Editor.Plugin.Full.Repacked",
                "Mono.Security",
                "mscorlib",
                "netstandard",
                "nunit.framework",
                "System",
                "System.Configuration",
                "System.Core",
                "System.Xml",
                "System.Xml.Linq",
                "Unity.Cecil",
                "ToLua.Injection.Editor",
                "ToLua.Scripts",
                "ToLua.Scripts.Editor",
                "Unity.CollabProxy.Editor",
                "Unity.CompilationPipeline.Common",
                "Unity.Legacy.NRefactory",
                "Unity.Rider.Editor",
                "Unity.SerializationLogic",
                "Unity.TextMeshPro.Editor",
                "Unity.Timeline.Editor",
                "Unity.VSCode.Editor",
                "UnityEditor",
                "UnityEditor.Graphs",
                "UnityEditor.TestRunner",
                "UnityEditor.UI",
                "UnityEditor.VR",
                "UnityEditor.WindowsStandalone.Extensions",
                // UnityEngine.AnimationModule
                // UnityEngine.ARModule
                // UnityEngine.AssetBundleModule
                // UnityEngine.AudioModule
                // UnityEngine.ClothModule
                // UnityEngine.ClusterInputModule
                // UnityEngine.ClusterRendererModule
                // UnityEngine.CoreModule
                // UnityEngine.CrashReportingModule
                // UnityEngine.DirectorModule
                // UnityEngine.DSPGraphModule
                // UnityEngine.GameCenterModule
                // UnityEngine.GridModule
                // UnityEngine.HotReloadModule
                // UnityEngine.ImageConversionModule
                // UnityEngine.IMGUIModule
                // UnityEngine.InputLegacyModule
                // UnityEngine.InputModule
                // UnityEngine.JSONSerializeModule
                // UnityEngine.LocalizationModule
                // UnityEngine.ParticleSystemModule
                // UnityEngine.PerformanceReportingModule
                // UnityEngine.Physics2DModule
                // UnityEngine.PhysicsModule
                // UnityEngine.ProfilerModule
                // UnityEngine.ScreenCaptureModule
                // UnityEngine.SharedInternalsModule
                // UnityEngine.SpriteMaskModule
                // UnityEngine.SpriteShapeModule
                // UnityEngine.StreamingModule
                // UnityEngine.SubstanceModule
                // UnityEngine.SubsystemsModule
                // UnityEngine.TerrainModule
                // UnityEngine.TerrainPhysicsModule
                "UnityEngine.TestRunner",
                // UnityEngine.TextCoreModule
                // UnityEngine.TextRenderingModule
                // UnityEngine.TilemapModule
                // UnityEngine.TLSModule
                // UnityEngine.UI
                // UnityEngine.UIElementsModule
                // UnityEngine.UIModule
                // UnityEngine.UmbraModule
                // UnityEngine.UNETModule
                // UnityEngine.UnityAnalyticsModule
                // UnityEngine.UnityConnectModule
                "UnityEngine.UnityTestProtocolModule",
                // UnityEngine.UnityWebRequestAssetBundleModule
                // UnityEngine.UnityWebRequestAudioModule
                // UnityEngine.UnityWebRequestModule
                // UnityEngine.UnityWebRequestTextureModule
                // UnityEngine.UnityWebRequestWWWModule
                // UnityEngine.VehiclesModule
                // UnityEngine.VFXModule
                // UnityEngine.VideoModule
                // UnityEngine.VRModule
                // UnityEngine.WindModule
                // UnityEngine.XRModule
            };
        }
    }


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