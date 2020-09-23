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
//打开开关没有写入导出列表的纯虚类自动跳过
//#define JUMP_NODEFINED_ABSTRACT         

using UnityEngine;
using UnityEditor;
using System;
using System.Collections.Generic;
using System.Reflection;
using System.Text;
using System.IO;
using System.Diagnostics;
using LuaInterface;
using Debug = UnityEngine.Debug;
using Debugger = LuaInterface.Debugger;
using LuaInterface.Editor;

[InitializeOnLoad]
public static class ToLuaMenu
{
    private static bool beAutoGen = false;
    private static bool beCheck = true;

    static ToLuaMenu()
    {
        AutoGenerateWraps();
    }

    private static void AutoGenerateWraps()
    {
        var settings = ToLuaSettingsUtility.Settings;
        if (settings == null)
            return;

        var dir = settings.SaveDir;
        var files = Directory.GetFiles(dir, "*.cs", SearchOption.TopDirectoryOnly);

        if (files.Length < 3 && beCheck)
        {
            if (EditorUtility.DisplayDialog("自动生成", "点击确定自动生成常用类型注册文件， 也可通过菜单逐步完成此功能", "确定", "取消"))
            {
                beAutoGen = true;
                GenLuaDelegates();
                AssetDatabase.Refresh();
                GenerateClassWraps();
                GenLuaBinder();
                beAutoGen = false;
            }

            beCheck = false;
        }
    }

    static string RemoveNameSpace(string name, string space)
    {
        if (space != null)
        {
            name = name.Remove(0, space.Length + 1);
        }

        return name;
    }

    public class BindType
    {
        public string name { get; private set; } //类名称
        public Type type { get; private set; }
        public bool IsStatic => type.IsClass && type.IsAbstract && type.IsSealed;
        public string wrapName = ""; //产生的wrap文件名字
        public string LibName { get; private set; } //注册到lua的名字
        public Type baseType { get; private set; }
        public string nameSpace { get; private set; } //注册到lua的table层级

        public BindType(Type t)
        {
            if (typeof(System.MulticastDelegate).IsAssignableFrom(t))
            {
                throw new NotSupportedException(
                    $"\nDon't export Delegate {LuaMisc.GetTypeName(t)} as a class, register it in customDelegateList");
            }

            type = t;
            nameSpace = ToLuaExport.GetNameSpace(t, out var libName);
            name = ToLuaExport.CombineTypeStr(nameSpace, libName);
            LibName = ToLuaExport.ConvertToLibSign(libName);

            switch (name)
            {
                case "object":
                    wrapName = "System_Object";
                    name = "System.Object";
                    break;
                case "string":
                    wrapName = "System_String";
                    name = "System.String";
                    break;
                default:
                    wrapName = name.Replace('.', '_');
                    wrapName = ToLuaExport.ConvertToLibSign(wrapName);
                    break;
            }

            baseType = LuaMisc.GetExportBaseType(type);
        }
    }

    private static void GenerateClassWrap(BindType bindType)
    {
        var wrapperSaveDir = ToLuaSettingsUtility.Settings.WrapperSaveDir;

        ToLuaExport.Clear();
        ToLuaExport.className = bindType.name;
        ToLuaExport.type = bindType.type;
        ToLuaExport.isStaticClass = bindType.IsStatic;
        ToLuaExport.baseType = bindType.baseType;
        ToLuaExport.wrapClassName = bindType.wrapName;
        ToLuaExport.libClassName = bindType.LibName;
        ToLuaExport.Generate(wrapperSaveDir);
    }

    [MenuItem("Lua/Gen lua wrap file for debug", false, 1)]
    public static void GenerateClassWrapForDebug()
    {
        if (!beAutoGen && EditorApplication.isCompiling)
        {
            EditorUtility.DisplayDialog("警告", "请等待编辑器完成编译再执行此功能", "确定");
            return;
        }

        var saveDir = ToLuaSettingsUtility.Settings.SaveDir;
        if (!File.Exists(saveDir))
            Directory.CreateDirectory(saveDir);

        var wrapperSaveDir = ToLuaSettingsUtility.Settings.WrapperSaveDir;
        if (!File.Exists(wrapperSaveDir))
            Directory.CreateDirectory(wrapperSaveDir);

        foreach (var bindType in ToLuaSettingsUtility.BindTypes)
            ToLuaExport.allTypes.Add(bindType.type);

        GenerateClassWrap(new BindType(typeof(UnityEngine.Events.UnityEvent<float>)));

        Debug.Log("Generate lua binding file over");
        ToLuaExport.allTypes.Clear();
        AssetDatabase.Refresh();
    }

    [MenuItem("Lua/Gen Lua Wrap Files", false, 1)]
    public static void GenerateClassWraps()
    {
        if (!beAutoGen && EditorApplication.isCompiling)
        {
            EditorUtility.DisplayDialog("警告", "请等待编辑器完成编译再执行此功能", "确定");
            return;
        }

        var saveDir = ToLuaSettingsUtility.Settings.SaveDir;
        if (!File.Exists(saveDir))
            Directory.CreateDirectory(saveDir);

        var wrapperSaveDir = ToLuaSettingsUtility.Settings.WrapperSaveDir;
        if (!File.Exists(wrapperSaveDir))
            Directory.CreateDirectory(wrapperSaveDir);

        foreach (var bindType in ToLuaSettingsUtility.BindTypes)
            ToLuaExport.allTypes.Add(bindType.type);

        foreach (var bindType in ToLuaSettingsUtility.BindTypes)
            GenerateClassWrap(bindType);

        Debug.Log("Generate lua binding files over");
        ToLuaExport.allTypes.Clear();
        AssetDatabase.Refresh();
    }

    static HashSet<Type> GetCustomTypeDelegates()
    {
        var list = ToLuaSettingsUtility.BindTypes;
        var set = new HashSet<Type>();
        var binding = BindingFlags.Public | BindingFlags.Static | BindingFlags.IgnoreCase | BindingFlags.Instance;

        for (int i = 0; i < list.Length; i++)
        {
            var type = list[i].type;
            var fields = type.GetFields(BindingFlags.GetField | BindingFlags.SetField | binding);
            var props = type.GetProperties(BindingFlags.GetProperty | BindingFlags.SetProperty | binding);

            var methods = type.IsInterface ? type.GetMethods() : type.GetMethods(BindingFlags.Instance | binding);

            for (int j = 0; j < fields.Length; j++)
            {
                var field = fields[j];
                var t = field.FieldType;

                if (ToLuaExport.IsDelegateType(t))
                {
                    set.Add(t);
                }
            }

            for (int j = 0; j < props.Length; j++)
            {
                var prop = props[j];
                var t = prop.PropertyType;

                if (ToLuaExport.IsDelegateType(t))
                {
                    set.Add(t);
                }
            }

            for (int j = 0; j < methods.Length; j++)
            {
                var m = methods[j];
                if (m.IsGenericMethod)
                    continue;

                var pifs = m.GetParameters();
                for (int k = 0; k < pifs.Length; k++)
                {
                    var pif = pifs[k];

                    var t = pif.ParameterType;
                    if (t.IsByRef)
                        t = t.GetElementType();

                    if (ToLuaExport.IsDelegateType(t))
                    {
                        set.Add(t);
                    }
                }
            }
        }

        return set;
    }

    [MenuItem("Lua/Gen Lua Delegates", false, 2)]
    static void GenLuaDelegates()
    {
        if (!beAutoGen && EditorApplication.isCompiling)
        {
            EditorUtility.DisplayDialog("警告", "请等待编辑器完成编译再执行此功能", "确定");
            return;
        }

        ToLuaExport.Clear();
        var list = new List<DelegateType>();
        list.AddRange(ToLuaSettingsUtility.customDelegateList);
        var set = GetCustomTypeDelegates();
        foreach (var t in set)
        {
            if (null == list.Find((p) => p.type == t))
            {
                list.Add(new DelegateType(t));
            }
        }

        ToLuaExport.GenDelegates(list.ToArray());
        set.Clear();
        ToLuaExport.Clear();
        AssetDatabase.Refresh();
        Debug.Log("Create lua delegate over");
    }

    static ToLuaTree<string> InitTree()
    {
        var tree = new ToLuaTree<string>();
        var root = tree.GetRoot();
        var list = ToLuaSettingsUtility.BindTypes;

        for (int i = 0; i < list.Length; i++)
        {
            var space = list[i].nameSpace;
            AddSpaceNameToTree(tree, root, space);
        }

        var dts = ToLuaSettingsUtility.customDelegateList;
        string str = null;

        for (int i = 0; i < dts.Length; i++)
        {
            var space = ToLuaExport.GetNameSpace(dts[i].type, out str);
            AddSpaceNameToTree(tree, root, space);
        }

        return tree;
    }

    static void AddSpaceNameToTree(ToLuaTree<string> tree, ToLuaNode<string> parent, string space)
    {
        if (string.IsNullOrEmpty(space))
            return;

        var ns = space.Split(new char[] { '.' });

        for (int j = 0; j < ns.Length; j++)
        {
            var nodes = tree.Find((_t) => _t == ns[j], j);

            if (nodes.Count == 0)
            {
                var node = new ToLuaNode<string>();
                node.value = ns[j];
                parent.childs.Add(node);
                node.parent = parent;
                node.layer = j;
                parent = node;
            }
            else
            {
                bool flag = false;
                int index = 0;

                for (int i = 0; i < nodes.Count; i++)
                {
                    int count = j;
                    int size = j;
                    var nodecopy = nodes[i];

                    while (nodecopy.parent != null)
                    {
                        nodecopy = nodecopy.parent;
                        if (nodecopy.value != null && nodecopy.value == ns[--count])
                        {
                            size--;
                        }
                    }

                    if (size == 0)
                    {
                        index = i;
                        flag = true;
                        break;
                    }
                }

                if (!flag)
                {
                    var nnode = new ToLuaNode<string>();
                    nnode.value = ns[j];
                    nnode.layer = j;
                    nnode.parent = parent;
                    parent.childs.Add(nnode);
                    parent = nnode;
                }
                else
                {
                    parent = nodes[index];
                }
            }
        }
    }

    static string GetSpaceNameFromTree(ToLuaNode<string> node)
    {
        string name = node.value;

        while (node.parent != null && node.parent.value != null)
        {
            node = node.parent;
            name = node.value + "." + name;
        }

        return name;
    }

    static string RemoveTemplateSign(string str)
    {
        str = str.Replace('<', '_');

        int index = str.IndexOf('>');

        while (index > 0)
        {
            str = str.Remove(index, 1);
            index = str.IndexOf('>');
        }

        return str;
    }

    [MenuItem("Lua/Gen LuaBinder File", false, 4)]
    static void GenLuaBinder()
    {
        if (!beAutoGen && EditorApplication.isCompiling)
        {
            EditorUtility.DisplayDialog("警告", "请等待编辑器完成编译再执行此功能", "确定");
            return;
        }

        var tree = InitTree();
        var sb = new StringBuilder();
        var dtList = new List<DelegateType>();

        var list = new List<DelegateType>();
        list.AddRange(ToLuaSettingsUtility.customDelegateList);
        var set = GetCustomTypeDelegates();

        var root = tree.GetRoot();

        foreach (var t in set)
        {
            if (null == list.Find((p) => p.type == t))
            {
                var dt = new DelegateType(t);
                AddSpaceNameToTree(tree, root, ToLuaExport.GetNameSpace(t, out _));
                list.Add(dt);
            }
        }

        sb.AppendLineEx("//this source code was auto-generated by tolua#, do not modify it");
        sb.AppendLineEx("using System;");
        sb.AppendLineEx("using UnityEngine;");
        sb.AppendLineEx("using LuaInterface;");
        sb.AppendLineEx();
        sb.AppendLineEx("public static class LuaBinder");
        sb.AppendLineEx("{");
        sb.AppendLineEx("\tpublic static void Register(LuaState L)");
        sb.AppendLineEx("\t{");
        sb.AppendLineEx("\t\tfloat t = Time.realtimeSinceStartup;");
        sb.AppendLineEx("\t\tL.BeginModule(null);");

        GenRegisterInfo(null, sb, list, dtList);

        Action<ToLuaNode<string>> begin = (node) =>
        {
            if (node.value == null)
                return;

            sb.Append($"\t\tL.BeginModule(\"{node.value}\");\r\n");
            var space = GetSpaceNameFromTree(node);

            GenRegisterInfo(space, sb, list, dtList);
        };

        Action<ToLuaNode<string>> end = (node) =>
        {
            if (node.value != null)
            {
                sb.AppendLineEx("\t\tL.EndModule();");
            }
        };

        tree.DepthFirstTraversal(begin, end, tree.GetRoot());
        sb.AppendLineEx("\t\tL.EndModule();");

        sb.AppendLineEx("\t\tDebugger.Log(\"Register lua type cost time: {0}\", Time.realtimeSinceStartup - t);");
        sb.AppendLineEx("\t}");

        sb.AppendLineEx($"\t// Delegates: {set.Count}, {list.Count}, {dtList.Count}");

        foreach (var dt in dtList)
        {
            ToLuaExport.GenEventFunction(dt.type, sb);
        }

        sb.AppendLineEx("}\r\n");
        var file = ToLuaSettingsUtility.Settings.SaveDir + "LuaBinder.cs";

        using (var textWriter = new StreamWriter(file, false, Encoding.UTF8))
        {
            textWriter.Write(sb.ToString());
            textWriter.Flush();
            textWriter.Close();
        }

        AssetDatabase.Refresh();
        Debugger.Log("Generate LuaBinder over !");
    }

    static void GenRegisterInfo(string nameSpace, StringBuilder sb, List<DelegateType> delegateList,
        List<DelegateType> wrappedDelegatesCache)
    {
        var bindTypes = ToLuaSettingsUtility.BindTypes;

        for (int i = 0; i < bindTypes.Length; i++)
        {
            var bindType = bindTypes[i];
            if (bindType.nameSpace == nameSpace)
            {
                sb.Append("\t\t" + bindType.wrapName + "Wrap.Register(L);\r\n");
            }
        }

        for (int i = 0; i < delegateList.Count; i++)
        {
            var dt = delegateList[i];
            var type = dt.type;
            var typeSpace = ToLuaExport.GetNameSpace(type, out var funcName);

            if (typeSpace == nameSpace)
            {
                funcName = ToLuaExport.ConvertToLibSign(funcName);
                var abr = dt.abr;
                abr = abr ?? funcName;
                sb.AppendFormat("\t\tL.RegFunction(\"{0}\", {1});\r\n", abr, dt.name);
                wrappedDelegatesCache.Add(dt);
            }
        }
    }

    static void GenPreLoadFunction(BindType bt, StringBuilder sb)
    {
        var funcName = "LuaOpen_" + bt.wrapName;

        sb.AppendLineEx("\r\n\t[MonoPInvokeCallback(typeof(LuaCSFunction))]");
        sb.AppendFormat("\tstatic int {0}(IntPtr L)\r\n", funcName);
        sb.AppendLineEx("\t{");
        sb.AppendLineEx("\t\ttry");
        sb.AppendLineEx("\t\t{");
        sb.AppendLineEx("\t\t\tLuaState state = LuaState.Get(L);");
        sb.AppendFormat("\t\t\tstate.BeginPreModule(\"{0}\");\r\n", bt.nameSpace);
        sb.AppendFormat("\t\t\t{0}Wrap.Register(state);\r\n", bt.wrapName);
        sb.AppendFormat("\t\t\tint reference = state.GetMetaReference(typeof({0}));\r\n", bt.name);
        sb.AppendLineEx("\t\t\tstate.EndPreModule(L, reference);");
        sb.AppendLineEx("\t\t\treturn 1;");
        sb.AppendLineEx("\t\t}");
        sb.AppendLineEx("\t\tcatch(Exception e)");
        sb.AppendLineEx("\t\t{");
        sb.AppendLineEx("\t\t\treturn LuaDLL.toluaL_exception(L, e);");
        sb.AppendLineEx("\t\t}");
        sb.AppendLineEx("\t}");
    }

    static string GetOS()
    {
        return LuaConst.osDir;
    }

    static string CreateStreamDir(string dir)
    {
        dir = Application.streamingAssetsPath + "/" + dir;

        if (!File.Exists(dir))
        {
            Directory.CreateDirectory(dir);
        }

        return dir;
    }

    static void BuildLuaBundle(string subDir, string sourceDir)
    {
        var files = Directory.GetFiles(sourceDir + subDir, "*.bytes");
        var bundleName = subDir == null ? "lua.unity3d" : "lua" + subDir.Replace('/', '_') + ".unity3d";
        bundleName = bundleName.ToLower();

        for (int i = 0; i < files.Length; i++)
        {
            var importer = AssetImporter.GetAtPath(files[i]);

            if (importer)
            {
                importer.assetBundleName = bundleName;
                importer.assetBundleVariant = null;
            }
        }
    }

    private static void DeleteDirectory(string path)
    {
        if (Directory.Exists(path))
        {
            Directory.Delete(path, true);
        }
    }

    static void ClearAllLuaFiles()
    {
        string osPath = Application.streamingAssetsPath + "/" + GetOS();

        if (Directory.Exists(osPath))
        {
            var files = Directory.GetFiles(osPath, "Lua*.unity3d");

            for (int i = 0; i < files.Length; i++)
            {
                File.Delete(files[i]);
            }
        }

        DeleteDirectory(osPath + "/Lua");
        DeleteDirectory(Application.streamingAssetsPath + "/Lua");
        DeleteDirectory(Application.dataPath + "/temp");
        DeleteDirectory(Application.dataPath + "/Resources/Lua");
        DeleteDirectory(Application.persistentDataPath + "/" + GetOS() + "/Lua");
    }

    [MenuItem("Lua/Gen LuaWrap + Binder", false, 4)]
    static void GenLuaWrapBinder()
    {
        if (EditorApplication.isCompiling)
        {
            EditorUtility.DisplayDialog("警告", "请等待编辑器完成编译再执行此功能", "确定");
            return;
        }

        beAutoGen = true;
        AssetDatabase.Refresh();
        GenerateClassWraps();
        GenLuaBinder();
        beAutoGen = false;
    }

    [MenuItem("Lua/Generate All", false, 5)]
    static void GenLuaAll()
    {
        if (EditorApplication.isCompiling)
        {
            EditorUtility.DisplayDialog("警告", "请等待编辑器完成编译再执行此功能", "确定");
            return;
        }

        beAutoGen = true;
        GenLuaDelegates();
        AssetDatabase.Refresh();
        GenerateClassWraps();
        GenLuaBinder();
        beAutoGen = false;
    }

    [MenuItem("Lua/Clear wrap files", false, 6)]
    static void ClearLuaWraps()
    {
        var wrapperSaveDir = ToLuaSettingsUtility.Settings.WrapperSaveDir;
        var files = Directory.GetFiles(wrapperSaveDir, "*.cs", SearchOption.TopDirectoryOnly);

        for (int i = 0; i < files.Length; i++)
            File.Delete(files[i]);

        ToLuaExport.Clear();
        var list = new List<DelegateType>();
        ToLuaExport.GenDelegates(list.ToArray());
        ToLuaExport.Clear();

        var sb = new StringBuilder();
        sb.AppendLineEx("using System;");
        sb.AppendLineEx("using LuaInterface;");
        sb.AppendLineEx();
        sb.AppendLineEx("public static class LuaBinder");
        sb.AppendLineEx("{");
        sb.AppendLineEx("\tpublic static void Register(LuaState L)");
        sb.AppendLineEx("\t{");
        sb.AppendLineEx("\t\tthrow new LuaException(\"Please generate LuaBinder files first!\");");
        sb.AppendLineEx("\t}");
        sb.AppendLineEx("}");

        var file = ToLuaSettingsUtility.Settings.SaveDir + "LuaBinder.cs";

        using (var textWriter = new StreamWriter(file, false, Encoding.UTF8))
        {
            textWriter.Write(sb.ToString());
            textWriter.Flush();
            textWriter.Close();
        }

        AssetDatabase.Refresh();
    }

    static void CopyLuaBytesFiles(string sourceDir, string destDir, bool appendext = true,
        string searchPattern = "*.lua", SearchOption option = SearchOption.AllDirectories)
    {
        if (!Directory.Exists(sourceDir))
        {
            return;
        }

        var files = Directory.GetFiles(sourceDir, searchPattern, option);
        int len = sourceDir.Length;

        if (sourceDir[len - 1] == '/' || sourceDir[len - 1] == '\\')
        {
            --len;
        }

        for (int i = 0; i < files.Length; i++)
        {
            string str = files[i].Remove(0, len);
            string dest = destDir + "/" + str;
            if (appendext) dest += ".bytes";
            string dir = Path.GetDirectoryName(dest);
            Directory.CreateDirectory(dir);
            File.Copy(files[i], dest, true);
        }
    }


    [MenuItem("Lua/Copy Lua  files to Resources", false, 51)]
    public static void CopyLuaFilesToRes()
    {
        ClearAllLuaFiles();
        string destDir = Application.dataPath + "/Resources" + "/Lua";
        CopyLuaBytesFiles(LuaConst.luaDir, destDir);
        CopyLuaBytesFiles(LuaConst.toluaDir, destDir);
        AssetDatabase.Refresh();
        Debug.Log("Copy lua files over");
    }

    [MenuItem("Lua/Copy Lua  files to Persistent", false, 52)]
    public static void CopyLuaFilesToPersistent()
    {
        ClearAllLuaFiles();
        string destDir = Application.persistentDataPath + "/" + GetOS() + "/Lua";
        CopyLuaBytesFiles(LuaConst.luaDir, destDir, false);
        CopyLuaBytesFiles(LuaConst.toluaDir, destDir, false);
        AssetDatabase.Refresh();
        Debug.Log("Copy lua files over");
    }

    static void GetAllDirs(string dir, List<string> list)
    {
        var dirs = Directory.GetDirectories(dir);
        list.AddRange(dirs);

        for (int i = 0; i < dirs.Length; i++)
        {
            GetAllDirs(dirs[i], list);
        }
    }

    static void CopyDirectory(string source, string dest, string searchPattern = "*.lua",
        SearchOption option = SearchOption.AllDirectories)
    {
        var files = Directory.GetFiles(source, searchPattern, option);

        for (int i = 0; i < files.Length; i++)
        {
            string str = files[i].Remove(0, source.Length);
            string path = dest + "/" + str;
            string dir = Path.GetDirectoryName(path);
            Directory.CreateDirectory(dir);
            File.Copy(files[i], path, true);
        }
    }

    static void CopyBuildBat(string path, string tempDir)
    {
        if (EditorUserBuildSettings.activeBuildTarget == BuildTarget.StandaloneWindows64)
        {
            File.Copy(path + "/Luajit64/Build.bat", tempDir + "/Build.bat", true);
        }
        else if (EditorUserBuildSettings.activeBuildTarget == BuildTarget.StandaloneWindows)
        {
            if (IntPtr.Size == 4)
            {
                File.Copy(path + "/Luajit/Build.bat", tempDir + "/Build.bat", true);
            }
            else if (IntPtr.Size == 8)
            {
                File.Copy(path + "/Luajit64/Build.bat", tempDir + "/Build.bat", true);
            }
        }
        else if (EditorUserBuildSettings.activeBuildTarget == BuildTarget.iOS)
        {
            //Debug.Log("iOS默认用64位，32位自行考虑");
            File.Copy(path + "/Luajit64/Build.bat", tempDir + "/Build.bat", true);
        }
        else
        {
            File.Copy(path + "/Luajit/Build.bat", tempDir + "/Build.bat", true);
        }
    }

    [MenuItem("Lua/Build Lua files to Resources (PC)", false, 53)]
    public static void BuildLuaToResources()
    {
        ClearAllLuaFiles();
        var tempDir = CreateStreamDir("Lua");
        string destDir = Application.dataPath + "/Resources" + "/Lua";

        string path = Application.dataPath.Replace('\\', '/');
        path = path.Substring(0, path.LastIndexOf('/'));
        CopyBuildBat(path, tempDir);
        CopyLuaBytesFiles(LuaConst.luaDir, tempDir, false);
        var proc = Process.Start(tempDir + "/Build.bat");
        proc.WaitForExit();
        CopyLuaBytesFiles(tempDir + "/Out/", destDir, false, "*.lua.bytes");
        CopyLuaBytesFiles(LuaConst.toluaDir, destDir);

        Directory.Delete(tempDir, true);
        AssetDatabase.Refresh();
    }

    [MenuItem("Lua/Build Lua files to Persistent (PC)", false, 54)]
    public static void BuildLuaToPersistent()
    {
        ClearAllLuaFiles();
        string tempDir = CreateStreamDir("Lua");
        string destDir = Application.persistentDataPath + "/" + GetOS() + "/Lua/";

        string path = Application.dataPath.Replace('\\', '/');
        path = path.Substring(0, path.LastIndexOf('/'));
        CopyBuildBat(path, tempDir);
        CopyLuaBytesFiles(LuaConst.luaDir, tempDir, false);
        Process proc = Process.Start(tempDir + "/Build.bat");
        proc.WaitForExit();
        CopyLuaBytesFiles(LuaConst.toluaDir, destDir, false);

        path = tempDir + "/Out/";
        string[] files = Directory.GetFiles(path, "*.lua.bytes");
        int len = path.Length;

        for (int i = 0; i < files.Length; i++)
        {
            path = files[i].Remove(0, len);
            path = path.Substring(0, path.Length - 6);
            path = destDir + path;

            File.Copy(files[i], path, true);
        }

        Directory.Delete(tempDir, true);
        AssetDatabase.Refresh();
    }

    [MenuItem("Lua/Build bundle files not jit", false, 55)]
    public static void BuildNotJitBundles()
    {
        ClearAllLuaFiles();
        CreateStreamDir(GetOS());

        string tempDir = Application.dataPath + "/temp/Lua";

        if (!File.Exists(tempDir))
            Directory.CreateDirectory(tempDir);

        CopyLuaBytesFiles(LuaConst.luaDir, tempDir);
        CopyLuaBytesFiles(LuaConst.toluaDir, tempDir);

        AssetDatabase.Refresh();
        List<string> dirs = new List<string>();
        GetAllDirs(tempDir, dirs);

        for (int i = 0; i < dirs.Count; i++)
        {
            string str = dirs[i].Remove(0, tempDir.Length);
            BuildLuaBundle(str.Replace('\\', '/'), "Assets/temp/Lua");
        }

        BuildLuaBundle(null, "Assets/temp/Lua");

        AssetDatabase.SaveAssets();
        string output = $"{Application.streamingAssetsPath}/{GetOS()}";
        BuildPipeline.BuildAssetBundles(output, BuildAssetBundleOptions.DeterministicAssetBundle,
            EditorUserBuildSettings.activeBuildTarget);

        AssetDatabase.Refresh();
    }

    [MenuItem("Lua/Build Luajit bundle files   (PC)", false, 56)]
    public static void BuildLuaBundles()
    {
        ClearAllLuaFiles();
        CreateStreamDir(GetOS());

        string tempDir = Application.dataPath + "/temp/Lua";

        if (!File.Exists(tempDir))
            Directory.CreateDirectory(tempDir);

        string path = Application.dataPath.Replace('\\', '/');
        path = path.Substring(0, path.LastIndexOf('/'));
        CopyBuildBat(path, tempDir);
        CopyLuaBytesFiles(LuaConst.luaDir, tempDir, false);
        Process proc = Process.Start(tempDir + "/Build.bat");
        proc.WaitForExit();
        CopyLuaBytesFiles(LuaConst.toluaDir, tempDir + "/Out");

        AssetDatabase.Refresh();

        string sourceDir = tempDir + "/Out";
        var dirs = new List<string>();
        GetAllDirs(sourceDir, dirs);

        for (int i = 0; i < dirs.Count; i++)
        {
            string str = dirs[i].Remove(0, sourceDir.Length);
            BuildLuaBundle(str.Replace('\\', '/'), "Assets/temp/Lua/Out");
        }

        BuildLuaBundle(null, "Assets/temp/Lua/Out");

        AssetDatabase.Refresh();
        var output = $"{Application.streamingAssetsPath}/{GetOS()}";
        BuildPipeline.BuildAssetBundles(output, BuildAssetBundleOptions.DeterministicAssetBundle,
            EditorUserBuildSettings.activeBuildTarget);
        Directory.Delete(Application.dataPath + "/temp/", true);

        AssetDatabase.Refresh();
    }

    [MenuItem("Lua/Clear all Lua files", false, 57)]
    public static void ClearLuaFiles()
    {
        ClearAllLuaFiles();
    }

    static void CreateDefaultWrapFile(string path, string name)
    {
        StringBuilder sb = new StringBuilder();
        path = path + name + ".cs";
        sb.AppendLineEx("using System;");
        sb.AppendLineEx("using LuaInterface;");
        sb.AppendLineEx();
        sb.AppendLineEx("public static class " + name);
        sb.AppendLineEx("{");
        sb.AppendLineEx("\tpublic static void Register(LuaState L)");
        sb.AppendLineEx("\t{");
        sb.AppendLineEx("\t\tthrow new LuaException(\"Please click menu Lua/Gen BaseType Wrap first!\");");
        sb.AppendLineEx("\t}");
        sb.AppendLineEx("}");

        using (var textWriter = new StreamWriter(path, false, Encoding.UTF8))
        {
            textWriter.Write(sb.ToString());
            textWriter.Flush();
            textWriter.Close();
        }
    }

    [MenuItem("Lua/Clear BaseType Wrap", false, 102)]
    static void ClearBaseTypeLuaWrap()
    {
        var toluaBaseType = ToLuaSettingsUtility.Settings.ToluaBaseType;

        CreateDefaultWrapFile(toluaBaseType, "System_ObjectWrap");
        CreateDefaultWrapFile(toluaBaseType, "System_DelegateWrap");
        CreateDefaultWrapFile(toluaBaseType, "System_StringWrap");
        CreateDefaultWrapFile(toluaBaseType, "System_EnumWrap");
        CreateDefaultWrapFile(toluaBaseType, "System_TypeWrap");
        CreateDefaultWrapFile(toluaBaseType, "System_Collections_IEnumeratorWrap");
        CreateDefaultWrapFile(toluaBaseType, "UnityEngine_ObjectWrap");
        CreateDefaultWrapFile(toluaBaseType, "LuaInterface_EventObjectWrap");
        CreateDefaultWrapFile(toluaBaseType, "LuaInterface_LuaMethodWrap");
        CreateDefaultWrapFile(toluaBaseType, "LuaInterface_LuaPropertyWrap");
        CreateDefaultWrapFile(toluaBaseType, "LuaInterface_LuaFieldWrap");
        CreateDefaultWrapFile(toluaBaseType, "LuaInterface_LuaConstructorWrap");

        Debug.Log("Clear base type wrap files over");
        AssetDatabase.Refresh();
    }

    [MenuItem("Lua/Enable Lua Injection &e", false, 102)]
    static void EnableLuaInjection()
    {
        bool EnableSymbols = false;
        if (UpdateMonoCecil(ref EnableSymbols) != -1)
        {
            var curBuildTargetGroup = EditorUserBuildSettings.selectedBuildTargetGroup;
            string existSymbols = PlayerSettings.GetScriptingDefineSymbolsForGroup(curBuildTargetGroup);
            if (!existSymbols.Contains("ENABLE_LUA_INJECTION"))
            {
                PlayerSettings.SetScriptingDefineSymbolsForGroup(curBuildTargetGroup,
                    existSymbols + ";ENABLE_LUA_INJECTION");
            }

            AssetDatabase.Refresh();
        }
    }

#if ENABLE_LUA_INJECTION
    [MenuItem("Lua/Injection Remove &r", false, 5)]
#endif
    static void RemoveInjection()
    {
        if (Application.isPlaying)
        {
            EditorUtility.DisplayDialog("警告", "游戏运行过程中无法操作", "确定");
            return;
        }

        var curBuildTargetGroup = EditorUserBuildSettings.selectedBuildTargetGroup;
        string existSymbols = PlayerSettings.GetScriptingDefineSymbolsForGroup(curBuildTargetGroup);
        PlayerSettings.SetScriptingDefineSymbolsForGroup(curBuildTargetGroup,
            existSymbols.Replace("ENABLE_LUA_INJECTION", ""));
        Debug.Log("Lua Injection Removed!");
    }

    public static int UpdateMonoCecil(ref bool EnableSymbols)
    {
        string appFileName = Environment.GetCommandLineArgs()[0];
        string appPath = Path.GetDirectoryName(appFileName);
        string directory = appPath + "/Data/Managed/";
        if (UnityEngine.Application.platform == UnityEngine.RuntimePlatform.OSXEditor)
            directory = appPath.Substring(0, appPath.IndexOf("MacOS")) + "Managed/";

        string suitedMonoCecilPath = directory + "Unity.Cecil.dll";
        string suitedMonoCecilMdbPath = directory + "Unity.Cecil.Mdb.dll";
        string suitedMonoCecilPdbPath = directory + "Unity.Cecil.Pdb.dll";
        string suitedMonoCecilToolPath = directory + "Unity.CecilTools.dll";

        if (!File.Exists(suitedMonoCecilPath)
            && !File.Exists(suitedMonoCecilMdbPath)
            && !File.Exists(suitedMonoCecilPdbPath)
        )
        {
            EnableSymbols = false;
            Debug.Log("Haven't found Mono.Cecil.dll!Symbols Will Be Disabled");
            return -1;
        }

        bool bInjectionToolUpdated = false;
        var injectionToolPath = ToLuaSettingsUtility.Settings.injectionFilesPath + "Editor/";
        var existMonoCecilPath = injectionToolPath + Path.GetFileName(suitedMonoCecilPath);
        var existMonoCecilPdbPath = injectionToolPath + Path.GetFileName(suitedMonoCecilPdbPath);
        var existMonoCecilMdbPath = injectionToolPath + Path.GetFileName(suitedMonoCecilMdbPath);
        var existMonoCecilToolPath = injectionToolPath + Path.GetFileName(suitedMonoCecilToolPath);

        try
        {
            bInjectionToolUpdated = TryUpdate(suitedMonoCecilPath, existMonoCecilPath) ? true : bInjectionToolUpdated;
            bInjectionToolUpdated = TryUpdate(suitedMonoCecilPdbPath, existMonoCecilPdbPath) || bInjectionToolUpdated;
            bInjectionToolUpdated = TryUpdate(suitedMonoCecilMdbPath, existMonoCecilMdbPath) || bInjectionToolUpdated;
            TryUpdate(suitedMonoCecilToolPath, existMonoCecilToolPath);
        }
        catch (Exception e)
        {
            Debug.LogError(e.ToString());
            return -1;
        }

        EnableSymbols = true;

        return bInjectionToolUpdated ? 1 : 0;
    }

    static bool TryUpdate(string srcPath, string destPath)
    {
        if (GetFileContentMD5(srcPath) != GetFileContentMD5(destPath))
        {
            File.Copy(srcPath, destPath, true);
            return true;
        }

        return false;
    }

    static string GetFileContentMD5(string file)
    {
        if (!File.Exists(file))
            return string.Empty;

        var fs = new FileStream(file, FileMode.Open);
        System.Security.Cryptography.MD5 md5 = new System.Security.Cryptography.MD5CryptoServiceProvider();
        var retVal = md5.ComputeHash(fs);
        fs.Close();

        var sb = StringBuilderCache.Acquire();
        for (int i = 0; i < retVal.Length; i++)
        {
            sb.Append(retVal[i].ToString("x2"));
        }

        return StringBuilderCache.GetStringAndRelease(sb);
    }
}