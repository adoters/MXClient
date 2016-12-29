using UnityEngine;
using System.Collections.Generic;
using LuaInterface;
using System.Collections;
using System.IO;
using System;

public class LuaClient : MonoBehaviour
{
    public static LuaClient Instance
    {
        get;
        protected set;
    }

    protected LuaState _lua_state = null;
    protected LuaLooper _loop = null;
    protected LuaFunction levelLoaded = null;
    protected bool _open_lua_socket = false;

    protected virtual LuaFileUtils InitLoader()
    {
        if (LuaFileUtils.Instance != null)
        {
            return LuaFileUtils.Instance;
        }

        return new LuaFileUtils();
    }

    protected virtual void LoadLuaFiles()
    {
        OnLoadFinished();
    }

    protected virtual void OpenLibs()
    {
        _lua_state.OpenLibs(LuaDLL.luaopen_pb);
        _lua_state.OpenLibs(LuaDLL.luaopen_struct);
        _lua_state.OpenLibs(LuaDLL.luaopen_lpeg);
#if UNITY_STANDALONE_OSX || UNITY_EDITOR_OSX
        luaState.OpenLibs(LuaDLL.luaopen_bit);
#endif

        if (LuaConst.openLuaSocket) OpenLuaSocket();            
        if (LuaConst.openZbsDebugger) OpenZbsDebugger();
    }

    public void OpenZbsDebugger(string ip = "localhost")
    {
        if (!Directory.Exists(LuaConst.zbsDir))
        {
            Debugger.LogWarning("ZeroBraneStudio not install or LuaConst.zbsDir not right");
            return;
        }

        if (!LuaConst.openLuaSocket) OpenLuaSocket();

        if (!string.IsNullOrEmpty(LuaConst.zbsDir)) _lua_state.AddSearchPath(LuaConst.zbsDir);

        _lua_state.LuaDoString(string.Format("DebugServerIp = '{0}'", ip));
    }

    [MonoPInvokeCallbackAttribute(typeof(LuaCSFunction))]
    static int LuaOpen_Socket_Core(IntPtr L)
    {        
        return LuaDLL.luaopen_socket_core(L);
    }

    [MonoPInvokeCallbackAttribute(typeof(LuaCSFunction))]
    static int LuaOpen_Mime_Core(IntPtr L)
    {
        return LuaDLL.luaopen_mime_core(L);
    }

    protected void OpenLuaSocket()
    {
        LuaConst.openLuaSocket = true;

        _lua_state.BeginPreLoad();
        _lua_state.RegFunction("socket.core", LuaOpen_Socket_Core);
        _lua_state.RegFunction("mime.core", LuaOpen_Mime_Core);                
        _lua_state.EndPreLoad();                     
    }

    //CJSON比较特殊，只NEW了一个TABLE，没有注册库，这里注册一下
    protected void OpenCJson()
    {
        _lua_state.LuaGetField(LuaIndexes.LUA_REGISTRYINDEX, "_LOADED");
        _lua_state.OpenLibs(LuaDLL.luaopen_cjson);
        _lua_state.LuaSetField(-2, "cjson");

        _lua_state.OpenLibs(LuaDLL.luaopen_cjson_safe);
        _lua_state.LuaSetField(-2, "cjson.safe");                               
    }

    //调用LUA主函数
    protected virtual void CallMain()
    {
        LuaFunction main = _lua_state.GetFunction("Main");
        main.Call();
        main.Dispose();
        main = null;                
    }

    protected virtual void StartMain()
    {
        _lua_state.DoFile("Main.lua");
        levelLoaded = _lua_state.GetFunction("OnLevelWasLoaded");
        CallMain();
    }

    protected void StartLooper()
    {
        _loop = gameObject.AddComponent<LuaLooper>();
        _loop.luaState = _lua_state;
    }

    protected virtual void Bind()
    {        
        LuaBinder.Bind(_lua_state);
        LuaCoroutine.Register(_lua_state, this);
    }

    protected void Init()
    {        
        InitLoader();
        _lua_state = new LuaState();
        OpenLibs();
        _lua_state.LuaSetTop(0);
        Bind();
        LoadLuaFiles();    
    }

    protected void Awake()
    {
        Instance = this;
        Init();
    }

    protected virtual void OnLoadFinished()
    {
        _lua_state.Start();
        StartLooper();
        StartMain();
    }

    protected void OnLevelWasLoaded(int level)
    {
        if (levelLoaded != null)
        {
            levelLoaded.BeginPCall();
            levelLoaded.Push(level);
            levelLoaded.PCall();
            levelLoaded.EndPCall();
        }
    }

    protected void Destroy()
    {
        if (_lua_state != null)
        {
            LuaState state = _lua_state;
            _lua_state = null;

            if (levelLoaded != null)
            {
                levelLoaded.Dispose();
                levelLoaded = null;
            }

            if (_loop != null)
            {
                _loop.Destroy();
                _loop = null;
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
        return Instance._lua_state;
    }

    public LuaLooper GetLooper()
    {
        return _loop;
    }
}
