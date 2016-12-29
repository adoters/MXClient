#define MISS_WARNING

using System;
using System.Collections;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.IO;
using System.Runtime.InteropServices;
using System.Text;
using UnityEngine;

namespace LuaInterface
{
    public class LuaState : LuaStatePtr, IDisposable
    {
        public ObjectTranslator _translator = new ObjectTranslator();
        public LuaReflection _reflection = new LuaReflection();

        public int ArrayMetatable { get; private set; }
        public int DelegateMetatable { get; private set; }
        public int TypeMetatable { get; private set; }
        public int EnumMetatable { get; private set; }
        public int IterMetatable { get; private set; }
        public int OutMetatable { get; private set; }
        public int EventMetatable { get; private set; }

        //function ref                
        public int PackBounds { get; private set; }
        public int UnpackBounds { get; private set; }
        public int PackRay { get; private set; }
        public int UnpackRay { get; private set; }
        public int PackRaycastHit { get; private set; }        
        public int PackTouch { get; private set; }

        public bool LogGC 
        {
            get
            {
                return _be_loggc;
            }

            set
            {
                _be_loggc = value;
                _translator.LogGC = value;
            }
        }
        
        Dictionary<string, WeakReference> _func_map = new Dictionary<string, WeakReference>();
        Dictionary<int, WeakReference> _func_weak_reference = new Dictionary<int, WeakReference>();

        List<GCRef> _gc_list = new List<GCRef>();
        List<LuaBaseRef> _sub_list = new List<LuaBaseRef>();

        Dictionary<Type, int> _meta_map = new Dictionary<Type, int>();        
        Dictionary<Enum, object> _enum_map = new Dictionary<Enum, object>();
        Dictionary<Type, LuaCSFunction> _preload_map = new Dictionary<Type, LuaCSFunction>();

        Dictionary<int, Type> _types_map = new Dictionary<int, Type>();
        HashSet<Type> _general_set = new HashSet<Type>();
        HashSet<string> _module_set = null;

        private static LuaState _main_state = null;
        private static Dictionary<IntPtr, LuaState> _state_map = new Dictionary<IntPtr, LuaState>();

        private int _begin_count = 0;
        private bool _be_loggc = false;

#if UNITY_EDITOR
        private bool beStart = false;
#endif

#if MISS_WARNING
        HashSet<Type> _miss_set = new HashSet<Type>();
#endif

        public LuaState()            
        {
            if (_main_state == null)
            {
                _main_state = this;
            }
            
            LuaException.Init();            
            _L = LuaNewState();
            _state_map.Add(_L, this);                        
            OpenToLuaLibs();
            ToLua.OpenLibs(_L);
            OpenBaseLibs();                        
            LuaSetTop(0);
            InitLuaPath();
        }

        void OpenBaseLibs()
        {            
            BeginModule(null);

            BeginModule("System");
            System_ObjectWrap.Register(this);
            System_NullObjectWrap.Register(this);            
            System_StringWrap.Register(this);
            System_DelegateWrap.Register(this);
            System_EnumWrap.Register(this);
            System_ArrayWrap.Register(this);
            System_TypeWrap.Register(this);                                               
            BeginModule("Collections");
            System_Collections_IEnumeratorWrap.Register(this);

            BeginModule("ObjectModel");
            System_Collections_ObjectModel_ReadOnlyCollectionWrap.Register(this);
            EndModule();//ObjectModel

            BeginModule("Generic");
            System_Collections_Generic_ListWrap.Register(this);
            System_Collections_Generic_DictionaryWrap.Register(this);
            System_Collections_Generic_KeyValuePairWrap.Register(this);

            BeginModule("Dictionary");
            System_Collections_Generic_Dictionary_KeyCollectionWrap.Register(this);
            System_Collections_Generic_Dictionary_ValueCollectionWrap.Register(this);
            EndModule();//Dictionary
            EndModule();//Generic
            EndModule();//Collections     
            EndModule();//end System

            BeginModule("LuaInterface");
            LuaInterface_LuaOutWrap.Register(this);
            LuaInterface_EventObjectWrap.Register(this);
            EndModule();//end LuaInterface

            BeginModule("UnityEngine");
            UnityEngine_ObjectWrap.Register(this);            
            UnityEngine_CoroutineWrap.Register(this);
            EndModule(); //end UnityEngine

            EndModule(); //end global
                        
            LuaUnityLibs.OpenLibs(_L);            
            LuaReflection.OpenLibs(_L);
            ArrayMetatable = _meta_map[typeof(System.Array)];
            TypeMetatable = _meta_map[typeof(System.Type)];
            DelegateMetatable = _meta_map[typeof(System.Delegate)];
            EnumMetatable = _meta_map[typeof(System.Enum)];
            IterMetatable = _meta_map[typeof(IEnumerator)];
            EventMetatable = _meta_map[typeof(EventObject)];
        }

        void InitLuaPath()
        {
            InitPackagePath();

            if (!LuaFileUtils.Instance.beZip)
            {
#if UNITY_EDITOR
                if (!Directory.Exists(LuaConst.luaDir))
                {
                    string msg = string.Format("luaDir path not exists: {0}, configer it in LuaConst.cs", LuaConst.luaDir);
                    throw new LuaException(msg);
                }

                if (!Directory.Exists(LuaConst.toluaDir))
                {
                    string msg = string.Format("toluaDir path not exists: {0}, configer it in LuaConst.cs", LuaConst.toluaDir);
                    throw new LuaException(msg);
                }

                AddSearchPath(LuaConst.toluaDir);
                AddSearchPath(LuaConst.luaDir);
#endif
                if (LuaFileUtils.Instance.GetType() == typeof(LuaFileUtils))
                {
                    AddSearchPath(LuaConst.luaResDir);
                }
            }
        }

        void OpenBaseLuaLibs()
        {
            DoFile("tolua.lua");            //tolua table名字已经存在了,不能用require
            LuaUnityLibs.OpenLuaLibs(_L);
        }

        public void Start()
        {
#if UNITY_EDITOR
            beStart = true;
#endif
            Debugger.Log("LuaState start");
            OpenBaseLuaLibs();
            PackBounds = GetFuncRef("Bounds.New");
            UnpackBounds = GetFuncRef("Bounds.Get");
            PackRay = GetFuncRef("Ray.New");
            UnpackRay = GetFuncRef("Ray.Get");
            PackRaycastHit = GetFuncRef("RaycastHit.New");
            PackTouch = GetFuncRef("Touch.New");
        }

        public int OpenLibs(LuaCSFunction open)
        {
            int ret = open(_L);            
            return ret;
        }

        public void BeginPreLoad()
        {
            LuaGetGlobal("package");
            LuaGetField(-1, "preload");
            _module_set = new HashSet<string>();
        }

        public void EndPreLoad()
        {
            LuaPop(2);
            _module_set = null;
        }

        public void AddPreLoad(string name, LuaCSFunction func, Type type)
        {            
            if (!_preload_map.ContainsKey(type))
            {
                LuaDLL.tolua_pushcfunction(_L, func);
                LuaSetField(-2, name);
                _preload_map[type] = func;
                string module = type.Namespace;

                if (!string.IsNullOrEmpty(module) && !_module_set.Contains(module))
                {
                    LuaDLL.tolua_addpreload(_L, module);
                    _module_set.Add(module);
                }
            }            
        }

        //慎用，需要自己保证不会重复Add相同的name,并且上面函数没有使用过这个name
        public void AddPreLoad(string name, LuaCSFunction func)
        {
            LuaDLL.tolua_pushcfunction(_L, func);
            LuaSetField(-2, name);
        }

        public int BeginPreModule(string name)
        {
            int top = LuaGetTop();

            if (string.IsNullOrEmpty(name))
            {
                LuaDLL.lua_pushvalue(_L, LuaIndexes.LUA_GLOBALSINDEX);
                ++_begin_count;
                return top;
            }
            else if (LuaDLL.tolua_beginpremodule(_L, name))
            {
                ++_begin_count;
                return top;
            }
            
            throw new LuaException(string.Format("create table {0} fail", name));            
        }

        public void EndPreModule(int reference)
        {
            --_begin_count;            
            LuaDLL.tolua_endpremodule(_L, reference);
        }

        public void BindPreModule(Type t, LuaCSFunction func)
        {
            _preload_map[t] = func;
        }

        public LuaCSFunction GetPreModule(Type t)
        {
            LuaCSFunction func = null;
            _preload_map.TryGetValue(t, out func);
            return func;
        }

        public bool BeginModule(string name)
        {
#if UNITY_EDITOR
            if (name != null)
            {                
                LuaTypes type = LuaType(-1);

                if (type != LuaTypes.LUA_TTABLE)
                {                    
                    throw new LuaException("open global module first");
                }
            }
#endif
            if (LuaDLL.tolua_beginmodule(_L, name))
            {
                ++_begin_count;
                return true;
            }

            LuaSetTop(0);
            throw new LuaException(string.Format("create table {0} fail", name));            
        }

        public void EndModule()
        {
            --_begin_count;            
            LuaDLL.tolua_endmodule(_L);
        }

        void BindTypeRef(int reference, Type t)
        {
            _meta_map.Add(t, reference);
            _types_map.Add(reference, t);

            if (t.IsGenericTypeDefinition)
            {
                genericSet.Add(t);
            }
        }

        public Type GetClassType(int reference)
        {
            Type t = null;
            _types_map.TryGetValue(reference, out t);
            return t;
        }

        [MonoPInvokeCallbackAttribute(typeof(LuaCSFunction))]
        public static int Collect(IntPtr L)
        {
            int udata = LuaDLL.tolua_rawnetobj(L, 1);

            if (udata != -1)
            {
                ObjectTranslator translator = GetTranslator(L);
                translator.RemoveObject(udata);
            }

            return 0;
        }

        string GetToLuaTypeName(Type t)
        {
            if (t.IsGenericType)
            {
                string str = t.Name;
                int pos = str.IndexOf('`');

                if (pos > 0)
                {
                    str = str.Substring(0, pos);
                }

                return str;
            }

            return t.Name;
        }

        public int BeginClass(Type t, Type baseType, string name = null)
        {
            if (_begin_count == 0)
            {
                throw new LuaException("must call BeginModule first");
            }

            int baseMetaRef = 0;
            int reference = 0;            

            if (name == null)
            {
                name = GetToLuaTypeName(t);
            }

            if (baseType != null && !_meta_map.TryGetValue(baseType, out baseMetaRef))
            {
                LuaCreateTable();
                baseMetaRef = LuaRef(LuaIndexes.LUA_REGISTRYINDEX);                
                BindTypeRef(baseMetaRef, baseType);
            }

            if (_meta_map.TryGetValue(t, out reference))
            {
                LuaDLL.tolua_beginclass(_L, name, baseMetaRef, reference);
                RegFunction("__gc", Collect);
            }
            else
            {
                reference = LuaDLL.tolua_beginclass(_L, name, baseMetaRef);
                RegFunction("__gc", Collect);                
                BindTypeRef(reference, t);
            }

            return reference;
        }

        public void EndClass()
        {
            LuaDLL.tolua_endclass(_L);
        }

        public int BeginEnum(Type t)
        {
            if (_begin_count == 0)
            {
                throw new LuaException("must call BeginModule first");
            }

            int reference = LuaDLL.tolua_beginenum(_L, t.Name);
            RegFunction("__gc", Collect);            
            BindTypeRef(reference, t);
            return reference;
        }

        public void EndEnum()
        {
            LuaDLL.tolua_endenum(_L);
        }

        public void BeginStaticLibs(string name)
        {
            if (_begin_count == 0)
            {
                throw new LuaException("must call BeginModule first");
            }

            LuaDLL.tolua_beginstaticclass(_L, name);
        }

        public void EndStaticLibs()
        {
            LuaDLL.tolua_endstaticclass(_L);
        }

        public void RegFunction(string name, LuaCSFunction func)
        {
            IntPtr fn = Marshal.GetFunctionPointerForDelegate(func);
            LuaDLL.tolua_function(_L, name, fn);            
        }

        public void RegVar(string name, LuaCSFunction get, LuaCSFunction set)
        {            
            IntPtr fget = IntPtr.Zero;
            IntPtr fset = IntPtr.Zero;

            if (get != null)
            {
                fget = Marshal.GetFunctionPointerForDelegate(get);
            }

            if (set != null)
            {
                fset = Marshal.GetFunctionPointerForDelegate(set);
            }

            LuaDLL.tolua_variable(_L, name, fget, fset);
        }

        public void RegConstant(string name, double d)
        {
            LuaDLL.tolua_constant(_L, name, d);
        }

        public void RegConstant(string name, bool flag)
        {
            LuaDLL.lua_pushstring(_L, name);
            LuaDLL.lua_pushboolean(_L, flag);
            LuaDLL.lua_rawset(_L, -3);
        }

        int GetFuncRef(string name)
        {
            if (PushLuaFunction(name, false))
            {
                return LuaRef(LuaIndexes.LUA_REGISTRYINDEX);
            }

            throw new LuaException("get lua function reference failed: " + name);                         
        }

        public static LuaState Get(IntPtr ptr)
        {
#if !MULTI_STATE
            return _main_state;
#else

            if (mainState != null && mainState.L == ptr)
            {
                return mainState;
            }

            LuaState state = null;

            if (stateMap.TryGetValue(ptr, out state))
            {
                return state;
            }
            else
            {                
                return Get(LuaDLL.tolua_getmainstate(ptr));
            }
#endif
        }

        public static ObjectTranslator GetTranslator(IntPtr ptr)
        {
#if !MULTI_STATE
            return _main_state._translator;
#else
            if (mainState != null && mainState.L == ptr)
            {
                return mainState.translator;
            }

            return Get(ptr).translator;
#endif
        }

        public static LuaReflection GetReflection(IntPtr ptr)
        {
#if !MULTI_STATE
            return _main_state._reflection;
#else
            if (mainState != null && mainState.L == ptr)
            {
                return mainState.reflection;
            }

            return Get(ptr).reflection;
#endif            
        }

        public object[] DoString(string chunk, string chunkName = "LuaState.cs")
        {
#if UNITY_EDITOR
            if (!beStart)
            {
                throw new LuaException("you must call Start() first to initialize LuaState");
            }
#endif
            byte[] buffer = Encoding.UTF8.GetBytes(chunk);
            return LuaLoadBuffer(buffer, chunkName);
        }        

        public object[] DoFile(string fileName)
        {
#if UNITY_EDITOR
            if (!beStart)
            {
                throw new LuaException("you must call Start() first to initialize LuaState");
            }
#endif                        
            byte[] buffer = LuaFileUtils.Instance.ReadFile(fileName);

            if (buffer == null)
            {
                string error = string.Format("cannot open {0}: No such file or directory", fileName);
                error += LuaFileUtils.Instance.FindFileError(fileName);
                throw new LuaException(error);
            }

            if (LuaConst.openZbsDebugger)
            {
                fileName = LuaFileUtils.Instance.FindFile(fileName);
            }

            return LuaLoadBuffer(buffer, fileName);
        }

        //注意fileName与lua文件中require一致。
        public void Require(string fileName)
        {
            int top = LuaGetTop();
            int ret = LuaRequire(fileName);

            if (ret != 0)
            {                
                string err = LuaToString(-1);
                LuaSetTop(top);
                throw new LuaException(err, LuaException.GetLastError());
            }

            LuaSetTop(top);            
        }

        public void InitPackagePath()
        {
            LuaGetGlobal("package");
            LuaGetField(-1, "path");
            string current = LuaToString(-1);
            string[] paths = current.Split(';');

            for (int i = 0; i < paths.Length; i++)
            {
                if (!string.IsNullOrEmpty(paths[i]))
                {
                    string path = paths[i].Replace('\\', '/');
                    LuaFileUtils.Instance.AddSearchPath(path);
                }
            }

            LuaPushString("");            
            LuaSetField(-3, "path");
            LuaPop(2);
        }

        string ToPackagePath(string path)
        {
            StringBuilder sb = StringBuilderCache.Acquire();
            sb.Append(path);
            sb.Replace('\\', '/');

            if (sb.Length > 0 && sb[sb.Length - 1] != '/')
            {
                sb.Append('/');
            }

            sb.Append("?.lua");
            return StringBuilderCache.GetStringAndRelease(sb);
        }

        public void AddSearchPath(string fullPath)
        {
            if (!Path.IsPathRooted(fullPath))
            {
                throw new LuaException(fullPath + " is not a full path");
            }

            fullPath = ToPackagePath(fullPath);
            LuaFileUtils.Instance.AddSearchPath(fullPath);        
        }

        public void RemoveSeachPath(string fullPath)
        {
            if (!Path.IsPathRooted(fullPath))
            {
                throw new LuaException(fullPath + " is not a full path");
            }

            fullPath = ToPackagePath(fullPath);
            LuaFileUtils.Instance.RemoveSearchPath(fullPath);
        }        

        public int BeginPCall(int reference)
        {                        
            return LuaDLL.tolua_beginpcall(_L, reference);
        }

        public void PCall(int args, int oldTop)
        {            
            if (LuaPCall(args, LuaDLL.LUA_MULTRET, oldTop) != 0)
            {
                string error = LuaToString(-1);
                throw new LuaException(error, LuaException.GetLastError());
            }            
        }

        public void EndPCall(int oldTop)
        {
            LuaSetTop(oldTop - 1);            
        }

        public void PushArgs(object[] args)
        {
            for (int i = 0; i < args.Length; i++)
            {
                Push(args[i]);
            }
        }

        void CheckNull(LuaBaseRef lbr, string fmt, object arg0)
        {
            if (lbr == null)
            {
                string error = string.Format(fmt, arg0);
                throw new LuaException(error, null, 2);
            }            
        }

        //压入一个存在的或不存在的table, 但不增加引用计数
        bool PushLuaTable(string fullPath, bool checkMap = true)
        {
            if (checkMap)
            {
                WeakReference weak = null;

                if (_func_map.TryGetValue(fullPath, out weak))
                {
                    if (weak.IsAlive)
                    {
                        LuaTable table = weak.Target as LuaTable;
                        CheckNull(table, "{0} not a lua table", fullPath);
                        Push(table);
                        return true;
                    }
                    else
                    {
                        _func_map.Remove(fullPath);
                    }
                }
            }

            if (!LuaDLL.tolua_pushluatable(_L, fullPath))
            {                
                return false;
            }

            return true;
        }

        bool PushLuaFunction(string fullPath, bool checkMap = true)
        {
            if (checkMap)
            {
                WeakReference weak = null;

                if (_func_map.TryGetValue(fullPath, out weak))
                {
                    if (weak.IsAlive)
                    {
                        LuaFunction func = weak.Target as LuaFunction;
                        CheckNull(func, "{0} not a lua function", fullPath);

                        if (func.IsAlive())
                        {
                            func.AddRef();
                            return true;
                        }
                    }

                    _func_map.Remove(fullPath);
                }
            }

            int oldTop = LuaGetTop();
            int pos = fullPath.LastIndexOf('.');

            if (pos > 0)
            {
                string tableName = fullPath.Substring(0, pos);

                if (PushLuaTable(tableName))
                {
                    string funcName = fullPath.Substring(pos + 1);
                    LuaPushString(funcName);
                    LuaRawGet(-2);

                    LuaTypes type = LuaType(-1);

                    if (type == LuaTypes.LUA_TFUNCTION)
                    {
                        LuaInsert(oldTop + 1);
                        LuaSetTop(oldTop + 1);
                        return true;
                    }
                }

                LuaSetTop(oldTop);
                return false;
            }
            else
            {
                LuaGetGlobal(fullPath);
                LuaTypes type = LuaType(-1);

                if (type != LuaTypes.LUA_TFUNCTION)
                {
                    LuaSetTop(oldTop);
                    return false;
                }
            }

            return true;
        }

        void RemoveFromGCList(int reference)
        {            
            lock (_gc_list)
            {
                int index = _gc_list.FindIndex((gc) => { return gc.reference == reference; });

                if (index >= 0)
                {
                    _gc_list.RemoveAt(index);
                }
            }
        }

        public LuaFunction GetFunction(string name, bool beLogMiss = true)
        {
            WeakReference weak = null;

            if (_func_map.TryGetValue(name, out weak))
            {
                if (weak.IsAlive)
                {
                    LuaFunction func = weak.Target as LuaFunction;
                    CheckNull(func, "{0} not a lua function", name);

                    if (func.IsAlive())
                    {
                        func.AddRef();
                        RemoveFromGCList(func.GetReference());
                        return func;
                    }
                }

                _func_map.Remove(name);
            }

            if (PushLuaFunction(name, false))
            {
                int reference = ToLuaRef();

                if (_func_weak_reference.TryGetValue(reference, out weak))
                {
                    if (weak.IsAlive)
                    {
                        LuaFunction func = weak.Target as LuaFunction;
                        CheckNull(func, "{0} not a lua function", name);

                        if (func.IsAlive())
                        {
                            _func_map.Add(name, weak);
                            func.AddRef();
                            RemoveFromGCList(reference);
                            return func;
                        }
                    }

                    _func_weak_reference.Remove(reference);
                }
                
                LuaFunction fun = new LuaFunction(reference, this);
                fun.name = name;
                _func_map.Add(name, new WeakReference(fun));
                _func_weak_reference.Add(reference, new WeakReference(fun));
                RemoveFromGCList(reference);
                if (LogGC) Debugger.Log("Alloc LuaFunction name {0}, id {1}", name, reference);                
                return fun;
            }

            if (beLogMiss)
            {
                Debugger.Log("Lua function {0} not exists", name);                
            }

            return null;
        }

        LuaBaseRef TryGetLuaRef(int reference)
        {            
            WeakReference weak = null;

            if (_func_weak_reference.TryGetValue(reference, out weak))
            {
                if (weak.IsAlive)
                {
                    LuaBaseRef luaRef = (LuaBaseRef)weak.Target;

                    if (luaRef.IsAlive())
                    {
                        luaRef.AddRef();
                        return luaRef;
                    }
                }                

                _func_weak_reference.Remove(reference);
            }

            return null;
        }

        public LuaFunction GetFunction(int reference)
        {
            LuaFunction func = TryGetLuaRef(reference) as LuaFunction;

            if (func == null)
            {                
                func = new LuaFunction(reference, this);
                _func_weak_reference.Add(reference, new WeakReference(func));
                if (LogGC) Debugger.Log("Alloc LuaFunction name , id {0}", reference);      
            }

            RemoveFromGCList(reference);
            return func;
        }

        public LuaTable GetTable(string fullPath, bool beLogMiss = true)
        {
            WeakReference weak = null;

            if (_func_map.TryGetValue(fullPath, out weak))
            {
                if (weak.IsAlive)
                {
                    LuaTable table = weak.Target as LuaTable;
                    CheckNull(table, "{0} not a lua table", fullPath);

                    if (table.IsAlive())
                    {
                        table.AddRef();
                        RemoveFromGCList(table.GetReference());
                        return table;
                    }
                }

                _func_map.Remove(fullPath);
            }

            if (PushLuaTable(fullPath, false))
            {
                int reference = ToLuaRef();
                LuaTable table = null;

                if (_func_weak_reference.TryGetValue(reference, out weak))
                {
                    if (weak.IsAlive)
                    {
                        table = weak.Target as LuaTable;
                        CheckNull(table, "{0} not a lua table", fullPath);

                        if (table.IsAlive())
                        {
                            _func_map.Add(fullPath, weak);
                            table.AddRef();
                            RemoveFromGCList(reference);
                            return table;
                        }
                    }

                    _func_weak_reference.Remove(reference);
                }

                table = new LuaTable(reference, this);
                table.name = fullPath;
                _func_map.Add(fullPath, new WeakReference(table));
                _func_weak_reference.Add(reference, new WeakReference(table));
                if (LogGC) Debugger.Log("Alloc LuaTable name {0}, id {1}", fullPath, reference);     
                RemoveFromGCList(reference);
                return table;
            }

            if (beLogMiss)
            {
                Debugger.LogWarning("Lua table {0} not exists", fullPath);
            }

            return null;
        }

        public LuaTable GetTable(int reference)
        {
            LuaTable table = TryGetLuaRef(reference) as LuaTable;

            if (table == null)
            {                
                table = new LuaTable(reference, this);
                _func_weak_reference.Add(reference, new WeakReference(table));
            }

            RemoveFromGCList(reference);
            return table;
        }

        public LuaThread GetLuaThread(int reference)
        {
            LuaThread thread = TryGetLuaRef(reference) as LuaThread;

            if (thread == null)
            {                
                thread = new LuaThread(reference, this);
                _func_weak_reference.Add(reference, new WeakReference(thread));
            }

            RemoveFromGCList(reference);
            return thread;
        }

        public bool CheckTop()
        {
            int n = LuaGetTop();

            if (n != 0)
            {
                Debugger.LogWarning("Lua stack top is {0}", n);
                return false;
            }

            return true;
        }

        public void Push(bool b)
        {
            LuaDLL.lua_pushboolean(_L, b);
        }

        public void Push(double d)
        {
            LuaDLL.lua_pushnumber(_L, d);
        }

        public void Push(uint un)
        {
            LuaDLL.lua_pushnumber(_L, un);
        }

        public void Push(int n)
        {
            LuaDLL.lua_pushinteger(_L, n);
        }

        public void Push(short s)
        {
            LuaDLL.lua_pushnumber(_L, s);
        }

        public void Push(ushort us)
        {
            LuaDLL.lua_pushnumber(_L, us);
        }

        public void Push(long l)
        {
            LuaDLL.tolua_pushint64(_L, l);
        }

        public void Push(ulong ul)
        {
            LuaDLL.tolua_pushuint64(_L, ul);
        }

        public void Push(string str)
        {
            LuaDLL.lua_pushstring(_L, str);
        }

        public void Push(IntPtr p)
        {
            LuaDLL.lua_pushlightuserdata(_L, p);
        }

        public void Push(Vector3 v3)
        {            
            LuaDLL.tolua_pushvec3(_L, v3.x, v3.y, v3.z);
        }

        public void Push(Vector2 v2)
        {
            LuaDLL.tolua_pushvec2(_L, v2.x, v2.y);
        }

        public void Push(Vector4 v4)
        {
            LuaDLL.tolua_pushvec4(_L, v4.x, v4.y, v4.z, v4.w);
        }

        public void Push(Color clr)
        {
            LuaDLL.tolua_pushclr(_L, clr.r, clr.g, clr.b, clr.a);
        }

        public void Push(Quaternion q)
        {
            LuaDLL.tolua_pushquat(_L, q.x, q.y, q.z, q.w);
        }          

        public void Push(Ray ray)
        {
            ToLua.Push(_L, ray);
        }

        public void Push(Bounds bound)
        {
            ToLua.Push(_L, bound);
        }

        public void Push(RaycastHit hit)
        {
            ToLua.Push(_L, hit);
        }

        public void Push(Touch touch)
        {
            ToLua.Push(_L, touch);
        }

        public void PushLayerMask(LayerMask mask)
        {
            LuaDLL.tolua_pushlayermask(_L, mask.value);
        }

        public void Push(LuaByteBuffer bb)
        {
            LuaDLL.lua_pushlstring(_L, bb.buffer, bb.buffer.Length);
        }

        public void PushByteBuffer(byte[] buffer)
        {
            LuaDLL.lua_pushlstring(_L, buffer, buffer.Length);
        }

        public void Push(LuaBaseRef lbr)
        {
            if (lbr == null)
            {                
                LuaPushNil();
            }
            else
            {
                LuaGetRef(lbr.GetReference());
            }
        }

        void PushUserData(object o, int reference)
        {
            int index;

            if (_translator.Getudata(o, out index))
            {
                if (LuaDLL.tolua_pushudata(_L, index))
                {
                    return;
                }
            }

            index = _translator.AddObject(o);
            LuaDLL.tolua_pushnewudata(_L, reference, index);
        }

        public void Push(Array array)
        {
            if (array == null)
            {                
                LuaPushNil();
            }
            else
            {
                PushUserData(array, ArrayMetatable);
            }
        }

        public void Push(Type t)
        {
            if (t == null)
            {
                LuaPushNil();
            }
            else
            {
                PushUserData(t, TypeMetatable);
            }
        }

        public void Push(Delegate ev)
        {
            if (ev == null)
            {                
                LuaPushNil();
            }
            else
            {
                PushUserData(ev, DelegateMetatable);
            }
        }

        public object GetEnumObj(Enum e)
        {
            object o = null;

            if (!_enum_map.TryGetValue(e, out o))
            {
                o = e;
                _enum_map.Add(e, o);
            }

            return o;
        }

        public void Push(Enum e)
        {
            if (e == null)
            {                
                LuaPushNil();
            }
            else
            {
                object o = GetEnumObj(e);
                PushUserData(o, EnumMetatable);
            }
        }

        public void Push(IEnumerator iter)
        {
            ToLua.Push(_L, iter);
        }

        public void Push(UnityEngine.Object obj)
        {
            ToLua.Push(_L, obj);
        }

        public void Push(UnityEngine.TrackedReference tracker)
        {
            ToLua.Push(_L, tracker);
        }

        public void PushValue(ValueType vt)
        {
            ToLua.PushValue(_L, vt);
        }        

        public void Push(object obj)
        {
            ToLua.Push(_L, obj);
        }

        public void PushObject(object obj)
        {
            if (obj.GetType().IsEnum)
            {
                ToLua.Push(_L, (Enum)obj);                
            }
            else                
            {
                ToLua.PushObject(_L, obj);
            }
        }

        Vector3 ToVector3(int stackPos)
        {
            float x, y, z;
            LuaDLL.tolua_getvec3(_L, stackPos, out x, out y, out z);
            return new Vector3(x, y, z);
        }

        public Vector3 CheckVector3(int stackPos)
        {            
            LuaValueType type = LuaDLL.tolua_getvaluetype(_L, stackPos);

            if (type != LuaValueType.Vector3)
            {
                LuaTypeError(stackPos, "Vector3", type.ToString());
                return Vector3.zero;
            }
            
            float x, y, z;
            LuaDLL.tolua_getvec3(_L, stackPos, out x, out y, out z);
            return new Vector3(x, y, z);
        }

        public Quaternion CheckQuaternion(int stackPos)
        {            
            LuaValueType type = LuaDLL.tolua_getvaluetype(_L, stackPos);

            if (type != LuaValueType.Vector4)
            {
                LuaTypeError(stackPos, "Quaternion", type.ToString());
                return Quaternion.identity;
            }

            float x, y, z, w;
            LuaDLL.tolua_getquat(_L, stackPos, out x, out y, out z, out w);
            return new Quaternion(x, y, z, w);
        }

        public Vector2 CheckVector2(int stackPos)
        {            
            LuaValueType type = LuaDLL.tolua_getvaluetype(_L, stackPos);

            if (type != LuaValueType.Vector2)
            {
                LuaTypeError(stackPos, "Vector2", type.ToString());                
                return Vector2.zero;
            }

            float x, y;
            LuaDLL.tolua_getvec2(_L, stackPos, out x, out y);
            return new Vector2(x, y);
        }

        public Vector4 CheckVector4(int stackPos)
        {            
            LuaValueType type = LuaDLL.tolua_getvaluetype(_L, stackPos);

            if (type != LuaValueType.Vector4)
            {
                LuaTypeError(stackPos, "Vector4", type.ToString());                    
                return Vector4.zero;
            }

            float x, y, z, w;
            LuaDLL.tolua_getvec4(_L, stackPos, out x, out y, out z, out w);
            return new Vector4(x, y, z, w);
        }

        public Color CheckColor(int stackPos)
        {            
            LuaValueType type = LuaDLL.tolua_getvaluetype(_L, stackPos);

            if (type != LuaValueType.Color)
            {
                LuaTypeError(stackPos, "Color", type.ToString());    
                return Color.black;
            }

            float r, g, b, a;
            LuaDLL.tolua_getclr(_L, stackPos, out r, out g, out b, out a);
            return new Color(r, g, b, a);
        }

        public Ray CheckRay(int stackPos)
        {            
            LuaValueType type = LuaDLL.tolua_getvaluetype(_L, stackPos);

            if (type != LuaValueType.Ray)
            {
                LuaTypeError(stackPos, "Ray", type.ToString());
                return new Ray();
            }
            
            int oldTop = BeginPCall(UnpackRay);
            LuaPushValue(stackPos);

            try
            {
                PCall(1, oldTop);
                Vector3 origin = ToVector3(oldTop + 1);
                Vector3 dir = ToVector3(oldTop + 2);
                EndPCall(oldTop);                
                return new Ray(origin, dir);
            }
            catch(Exception e)
            {
                EndPCall(oldTop);
                throw e;
            }
        }

        public Bounds CheckBounds(int stackPos)
        {            
            LuaValueType type = LuaDLL.tolua_getvaluetype(_L, stackPos);

            if (type != LuaValueType.Bounds)
            {
                LuaTypeError(stackPos, "Bounds", type.ToString());    
                return new Bounds();
            }
            
            int oldTop = BeginPCall(UnpackBounds);
            LuaPushValue(stackPos);

            try
            {
                PCall(1, oldTop);
                Vector3 center = ToVector3(oldTop + 1);
                Vector3 size = ToVector3(oldTop + 2);
                EndPCall(oldTop);
                return new Bounds(center, size);
            }
            catch(Exception e)
            {
                EndPCall(oldTop);
                throw e;
            }
        }

        public LayerMask CheckLayerMask(int stackPos)
        {            
            LuaValueType type = LuaDLL.tolua_getvaluetype(_L, stackPos);

            if (type != LuaValueType.LayerMask)
            {
                LuaTypeError(stackPos, "LayerMask", type.ToString());
                return 0;
            }
            
            return LuaDLL.tolua_getlayermask(_L, stackPos);
        }

        public long CheckLong(int stackPos)
        {
            return LuaDLL.tolua_checkint64(_L, stackPos);
        }

        public ulong CheckULong(int stackPos)
        {
            return LuaDLL.tolua_checkuint64(_L, stackPos);
        }

        public string CheckString(int stackPos)
        {
            return ToLua.CheckString(_L, stackPos);
        }

        public Delegate CheckDelegate(int stackPos)
        {            
            int udata = LuaDLL.tolua_rawnetobj(_L, stackPos);

            if (udata != -1)
            {
                object obj = _translator.GetObject(udata);

                if (obj != null)
                {
                    Type type = obj.GetType();

                    if (type.IsSubclassOf(typeof(System.MulticastDelegate)))
                    {
                        return (Delegate)obj;
                    }

                    LuaTypeError(stackPos, "Delegate", type.FullName);
                }

                return null;
            }
            else if (LuaIsNil(stackPos))
            {
                return null;
            }

            LuaTypeError(stackPos, "Delegate");
            return null;
        }

        public char[] CheckCharBuffer(int stackPos)
        {
            return ToLua.CheckCharBuffer(_L, stackPos);
        }

        public byte[] CheckByteBuffer(int stackPos)
        {
            return ToLua.CheckByteBuffer(_L, stackPos);
        }

        public T[] CheckNumberArray<T>(int stackPos)
        {
            return ToLua.CheckNumberArray<T>(_L, stackPos);
        }

        public object CheckObject(int stackPos, Type type)
        {
            return ToLua.CheckObject(_L, stackPos, type);
        }

        public object CheckVarObject(int stackPos, Type type)
        {
            return ToLua.CheckVarObject(_L, stackPos, type);
        }

        public object[] CheckObjects(int oldTop)
        {
            int newTop = LuaGetTop();

            if (oldTop == newTop)
            {
                return null;
            }
            else
            {
                List<object> returnValues = new List<object>();

                for (int i = oldTop + 1; i <= newTop; i++)
                {
                    returnValues.Add(ToVariant(i));
                }

                return returnValues.ToArray();
            }
        }

        public LuaFunction CheckLuaFunction(int stackPos)
        {
            return ToLua.CheckLuaFunction(_L, stackPos);
        }

        public LuaTable CheckLuaTable(int stackPos)
        {
            return ToLua.CheckLuaTable(_L, stackPos);
        }

        public LuaThread CheckLuaThread(int stackPos)
        {
            return ToLua.CheckLuaThread(_L, stackPos);
        }

        public object ToVariant(int stackPos)
        {
            return ToLua.ToVarObject(_L, stackPos);
        }    

        public void CollectRef(int reference, string name, bool isGCThread = false)
        {
            if (!isGCThread)
            {                
                Collect(reference, name, false);
            }
            else
            {
                lock (_gc_list)
                {
                    _gc_list.Add(new GCRef(reference, name));
                }
            }
        }

        public void DelayDispose(LuaBaseRef br)
        {
            if (br != null)
            {
                _sub_list.Add(br);
            }
        }

        public int Collect()
        {
            int count = _gc_list.Count;

            if (count > 0)
            {
                lock (_gc_list)
                {
                    for (int i = 0; i < _gc_list.Count; i++)
                    {
                        int reference = _gc_list[i].reference;
                        string name = _gc_list[i].name;
                        Collect(reference, name, true);
                    }

                    _gc_list.Clear();
                    return count;
                }
            }

            for (int i = 0; i < _sub_list.Count; i++)
            {
                _sub_list[i].Dispose();
            }

            _sub_list.Clear();
            _translator.Collect();
            return 0;
        }

        public object this[string fullPath]
        {
            get
            {
                int oldTop = LuaGetTop();
                int pos = fullPath.LastIndexOf('.');
                object obj = null;

                if (pos > 0)
                {
                    string tableName = fullPath.Substring(0, pos);

                    if (PushLuaTable(tableName))
                    {
                        string name = fullPath.Substring(pos + 1);
                        LuaPushString(name);
                        LuaRawGet(-2);
                        obj = ToVariant(-1);
                    }    
                    else
                    {
                        LuaSetTop(oldTop);
                        return null;
                    }
                }
                else
                {
                    LuaGetGlobal(fullPath);
                    obj = ToVariant(-1);
                }

                LuaSetTop(oldTop);
                return obj;
            }

            set
            {
                int oldTop = LuaGetTop();
                int pos = fullPath.LastIndexOf('.');

                if (pos > 0)
                {
                    string tableName = fullPath.Substring(0, pos);
                    IntPtr p = LuaFindTable(LuaIndexes.LUA_GLOBALSINDEX, tableName);

                    if (p == IntPtr.Zero)
                    {
                        string name = fullPath.Substring(pos + 1);
                        LuaPushString(name);
                        Push(value);
                        LuaSetTable(-3);
                    }
                    else
                    {
                        LuaSetTop(oldTop);
                        int len = LuaDLL.tolua_strlen(p);
                        string str = LuaDLL.lua_ptrtostring(p, len);
                        throw new LuaException(string.Format("{0} not a Lua table", str));
                    }
                }
                else
                {
                    Push(value);
                    LuaSetGlobal(fullPath);                    
                }

                LuaSetTop(oldTop);
            }
        }

        //慎用
        public void ReLoad(string moduleFileName)
        {
            LuaGetGlobal("package");
            LuaGetField(-1, "loaded");
            LuaPushString(moduleFileName);
            LuaGetTable(-2);                          

            if (!LuaIsNil(-1))
            {
                LuaPushString(moduleFileName);                        
                LuaPushNil();
                LuaSetTable(-4);                      
            }

            LuaPop(3);
            string require = string.Format("require '{0}'", moduleFileName);
            DoString(require, "ReLoad");
        }

        public int GetMetaReference(Type t)
        {
            int reference = -1;
            _meta_map.TryGetValue(t, out reference);
            return reference;
        }

        public int GetMissMetaReference(Type t)
        {       
            int reference = -1;
            Type type = GetBaseType(t);

            while (type != null)
            {
                if (_meta_map.TryGetValue(type, out reference))
                {
#if MISS_WARNING
                    if (!_miss_set.Contains(t))
                    {
                        Debugger.LogWarning("Type {0} not wrap to lua, push as {1}, the warning is only raised once", LuaMisc.GetTypeName(t), LuaMisc.GetTypeName(type));
                    }

                    _miss_set.Add(t);
#endif   
                    return reference;              
                }

                type = GetBaseType(type);
            }            

            if (reference <= 0)
            {
                type = typeof(object);
                reference = LuaStatic.GetMetaReference(_L, type);                
            }

#if MISS_WARNING
            if (!_miss_set.Contains(t))
            {                
                Debugger.LogWarning("Type {0} not wrap to lua, push as {1}, the warning is only raised once", LuaMisc.GetTypeName(t), LuaMisc.GetTypeName(type));
            }

            _miss_set.Add(t);
#endif     

            return reference;
        }

        Type GetBaseType(Type t)
        {
            if (t.IsGenericType)
            {
                return GetSpecialGenericType(t);
            }

            return LuaMisc.GetExportBaseType(t);
        }

        Type GetSpecialGenericType(Type t)
        {
            Type generic = t.GetGenericTypeDefinition();

            if (genericSet.Contains(generic))
            {
                return t == generic ? t.BaseType : generic;
            }

            return t.BaseType;
        }

        void CloseBaseRef()
        {
            LuaUnRef(PackBounds);
            LuaUnRef(UnpackBounds);
            LuaUnRef(PackRay);
            LuaUnRef(UnpackRay);
            LuaUnRef(PackRaycastHit);
            LuaUnRef(PackTouch);   
        }
        
        public void Dispose()
        {
            if (IntPtr.Zero != _L)
            {
                LuaGC(LuaGCOptions.LUA_GCCOLLECT, 0);
                Collect();

                foreach (KeyValuePair<Type, int> kv in _meta_map)
                {
                    LuaUnRef(kv.Value);
                }

                List<LuaBaseRef> list = new List<LuaBaseRef>();

                foreach (KeyValuePair<int, WeakReference> kv in _func_weak_reference)
                {
                    if (kv.Value.IsAlive)
                    {
                        list.Add((LuaBaseRef)kv.Value.Target);
                    }
                }

                for (int i = 0; i < list.Count; i++)
                {
                    list[i].Dispose(true);
                }

                CloseBaseRef();
                _func_weak_reference.Clear();
                _func_map.Clear();
                _meta_map.Clear();                
                _types_map.Clear();
                _enum_map.Clear();
                _preload_map.Clear();
                genericSet.Clear();
                _state_map.Remove(_L);
                LuaClose();
                _translator.Dispose();
                _translator = null;                    
#if MISS_WARNING
                _miss_set.Clear();
#endif
                Debugger.Log("LuaState destroy");
            }

            if (_main_state == this)
            {
                _main_state = null;
            }

#if UNITY_EDITOR
            beStart = false;
#endif

            LuaFileUtils.Instance.Dispose();
            System.GC.SuppressFinalize(this);            
        }

        //public virtual void Dispose(bool dispose)
        //{
        //}

        public override int GetHashCode()
        {
            return base.GetHashCode();
        }

        public override bool Equals(object o)
        {
            if (o == null) return _L == IntPtr.Zero;
            LuaState state = o as LuaState;

            if (state == null || state._L != _L)
            {
                return false;
            }

            return _L != IntPtr.Zero;
        }

        public static bool operator == (LuaState a, LuaState b)
        {
            if (System.Object.ReferenceEquals(a, b))
            {
                return true;
            }

            object l = a;
            object r = b;

            if (l == null && r != null)
            {
                return b._L == IntPtr.Zero;
            }

            if (l != null && r == null)
            {
                return a._L == IntPtr.Zero;
            }

            if (a._L != b._L)
            {
                return false;
            }

            return a._L != IntPtr.Zero;
        }

        public static bool operator != (LuaState a, LuaState b)
        {
            return !(a == b);
        }

        public void PrintTable(string name)
        {
            LuaTable table = GetTable(name);
            LuaDictTable dict = table.ToDictTable();
            table.Dispose();            
            var iter2 = dict.GetEnumerator();

            while (iter2.MoveNext())
            {
                Debugger.Log("map item, k,v is {0}:{1}", iter2.Current.Key, iter2.Current.Value);
            }

            iter2.Dispose();
            dict.Dispose();
        }

        protected void Collect(int reference, string name, bool beThread)
        {
            if (beThread)
            {
                WeakReference weak = null;

                if (name != null)
                {
                    _func_map.TryGetValue(name, out weak);

                    if (weak != null && !weak.IsAlive)
                    {
                        _func_map.Remove(name);
                        weak = null;
                    }
                }
                
                _func_weak_reference.TryGetValue(reference, out weak);

                if (weak != null && !weak.IsAlive)
                {
                    ToLuaUnRef(reference);
                    _func_weak_reference.Remove(reference);

                    if (LogGC)
                    {
                        string str = name == null ? "null" : name;
                        Debugger.Log("collect lua reference name {0}, id {1} in thread", str, reference);
                    }
                }
            }
            else
            {
                if (name != null)
                {
                    WeakReference weak = null;
                    _func_map.TryGetValue(name, out weak);
                    
                    if (weak != null && weak.IsAlive)
                    {
                        LuaBaseRef lbr = (LuaBaseRef)weak.Target;

                        if (reference == lbr.GetReference())
                        {
                            _func_map.Remove(name);
                        }
                    }
                }

                ToLuaUnRef(reference);
                _func_weak_reference.Remove(reference);

                if (LogGC)
                {
                    string str = name == null ? "null" : name;
                    Debugger.Log("collect lua reference name {0}, id {1} in main", str, reference);
                }
            }
        }

        protected object[] LuaLoadBuffer(byte[] buffer, string chunkName)
        {                        
            ToLuaPushTraceback();
            int oldTop = LuaGetTop();

            if (LuaLoadBuffer(buffer, buffer.Length, chunkName) == 0)
            {
                if (LuaPCall(0, LuaDLL.LUA_MULTRET, oldTop) == 0)
                {
                    object[] result = CheckObjects(oldTop);
                    LuaSetTop(oldTop - 1);
                    return result;
                }
            }

            string err = LuaToString(-1);
            LuaSetTop(oldTop - 1);                        
            throw new LuaException(err, LuaException.GetLastError());
        }
    }
}