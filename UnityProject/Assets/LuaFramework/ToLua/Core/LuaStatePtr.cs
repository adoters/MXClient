using UnityEngine;
using System;
using System.IO;
using System.Runtime.InteropServices;
using System.Text;

namespace LuaInterface
{
    public class LuaStatePtr
    {
        protected IntPtr _L;

        public int LuaUpValueIndex(int i)
        {
            return LuaIndexes.LUA_GLOBALSINDEX - i;
        }

        public IntPtr LuaNewState()
        {
            return LuaDLL.luaL_newstate();
        }

        public void LuaClose()
        {
            LuaDLL.lua_close(_L);
            _L = IntPtr.Zero;
        }

        public IntPtr LuaNewThread()
        {
            return LuaDLL.lua_newthread(_L);
        }        

        public IntPtr LuaAtPanic(IntPtr panic)
        {
            return LuaDLL.lua_atpanic(_L, panic);
        }

        public int LuaGetTop()
        {
            return LuaDLL.lua_gettop(_L);
        }

        public void LuaSetTop(int newTop)
        {
            LuaDLL.lua_settop(_L, newTop);
        }

        public void LuaPushValue(int idx)
        {
            LuaDLL.lua_pushvalue(_L, idx);
        }

        public void LuaRemove(int index)
        {
            LuaDLL.lua_remove(_L, index);
        }

        public void LuaInsert(int idx)
        {
            LuaDLL.lua_insert(_L, idx);
        }

        public void LuaReplace(int idx)
        {
            LuaDLL.lua_replace(_L, idx);
        }

        public bool LuaCheckStack(int args)
        {
            return LuaDLL.lua_checkstack(_L, args) != 0;
        }

        public void LuaXMove(IntPtr to, int n)
        {
            LuaDLL.lua_xmove(_L, to, n);
        }

        public bool LuaIsNumber(int idx)
        {
            return LuaDLL.lua_isnumber(_L, idx) != 0;
        }

        public bool LuaIsString(int index)
        {
            return LuaDLL.lua_isstring(_L, index) != 0;
        }

        public bool LuaIsCFunction(int index)
        {
            return LuaDLL.lua_iscfunction(_L, index) != 0;
        }

        public bool LuaIsUserData(int index)
        {
            return LuaDLL.lua_isuserdata(_L, index) != 0;
        }

        public bool LuaIsNil(int n)
        {
            return LuaDLL.lua_isnil(_L, n);
        }

        public LuaTypes LuaType(int index)
        {
            return LuaDLL.lua_type(_L, index);
        }

        public string LuaTypeName(LuaTypes type)
        {
            return LuaDLL.lua_typename(_L, type);
        }

        public string LuaTypeName(int idx)
        {
            return LuaDLL.luaL_typename(_L, idx);
        }

        public bool LuaEqual(int idx1, int idx2)
        {
            return LuaDLL.lua_equal(_L, idx1, idx2) != 0;
        }

        public bool LuaRawEqual(int idx1, int idx2)
        {
            return LuaDLL.lua_rawequal(_L, idx1, idx2) != 0;
        }

        public bool LuaLessThan(int idx1, int idx2)
        {
            return LuaDLL.lua_lessthan(_L, idx1, idx2) != 0;
        }

        public double LuaToNumber(int idx)
        {
            return LuaDLL.lua_tonumber(_L, idx);
        }

        public int LuaToInteger(int idx)
        {
            return LuaDLL.lua_tointeger(_L, idx);
        }

        public bool LuaToBoolean(int idx)
        {
            return LuaDLL.lua_toboolean(_L, idx);
        }

        public string LuaToString(int index)
        {
            return LuaDLL.lua_tostring(_L, index);
        }

        public IntPtr LuaToLString(int index, out int len)
        {
            return LuaDLL.tolua_tolstring(_L, index, out len);
        }

        public IntPtr LuaToCFunction(int idx)
        {
            return LuaDLL.lua_tocfunction(_L, idx);
        }

        public IntPtr LuaToUserData(int idx)
        {
            return LuaDLL.lua_touserdata(_L, idx);
        }

        public IntPtr LuaToThread(int idx)
        {
            return LuaDLL.lua_tothread(_L, idx);
        }

        public IntPtr LuaToPointer(int idx)
        {
            return LuaDLL.lua_topointer(_L, idx);
        }

        public int LuaObjLen(int index)
        {
            return LuaDLL.tolua_objlen(_L, index);
        }

        public void LuaPushNil()
        {
            LuaDLL.lua_pushnil(_L);
        }

        public void LuaPushNumber(double number)
        {
            LuaDLL.lua_pushnumber(_L, number);
        }

        public void LuaPushInteger(int n)
        {
            LuaDLL.lua_pushnumber(_L, n);
        }

        public void LuaPushLString(byte[] str, int size)
        {
            LuaDLL.lua_pushlstring(_L, str, size);
        }

        public void LuaPushString(string str)
        {
            LuaDLL.lua_pushstring(_L, str);
        }

        public void LuaPushCClosure(IntPtr fn, int n)
        {
            LuaDLL.lua_pushcclosure(_L, fn, n);
        }

        public void LuaPushBoolean(bool value)
        {
            LuaDLL.lua_pushboolean(_L, value ? 1 : 0);
        }

        public void LuaPushLightUserData(IntPtr udata)
        {
            LuaDLL.lua_pushlightuserdata(_L, udata);
        }

        public int LuaPushThread()
        {
            return LuaDLL.lua_pushthread(_L);
        }

        public void LuaGetTable(int idx)
        {
            LuaDLL.lua_gettable(_L, idx);
        }

        public void LuaGetField(int index, string key)
        {
            LuaDLL.lua_getfield(_L, index, key);
        }

        public void LuaRawGet(int idx)
        {
            LuaDLL.lua_rawget(_L, idx);
        }

        public void LuaRawGetI(int tableIndex, int index)
        {
            LuaDLL.lua_rawgeti(_L, tableIndex, index);
        }

        public void LuaCreateTable(int narr = 0, int nec = 0)
        {
            LuaDLL.lua_createtable(_L, narr, nec);
        }

        public IntPtr LuaNewUserData(int size)
        {
            return LuaDLL.tolua_newuserdata(_L, size);
        }

        public int LuaGetMetaTable(int idx)
        {
            return LuaDLL.lua_getmetatable(_L, idx);
        }

        public void LuaGetEnv(int idx)
        {
            LuaDLL.lua_getfenv(_L, idx);
        }

        public void LuaSetTable(int idx)
        {
            LuaDLL.lua_settable(_L, idx);
        }

        public void LuaSetField(int idx, string key)
        {
            LuaDLL.lua_setfield(_L, idx, key);
        }

        public void LuaRawSet(int idx)
        {
            LuaDLL.lua_rawset(_L, idx);
        }

        public void LuaRawSetI(int tableIndex, int index)
        {
            LuaDLL.lua_rawseti(_L, tableIndex, index);
        }

        public void LuaSetMetaTable(int objIndex)
        {
            LuaDLL.lua_setmetatable(_L, objIndex);
        }

        public void LuaSetEnv(int idx)
        {
            LuaDLL.lua_setfenv(_L, idx);
        }

        public void LuaCall(int nArgs, int nResults)
        {
            LuaDLL.lua_call(_L, nArgs, nResults);
        }

        public int LuaPCall(int nArgs, int nResults, int errfunc)
        {
            return LuaDLL.lua_pcall(_L, nArgs, nResults, errfunc);
        }

        public int LuaYield(int nresults)
        {
            return LuaDLL.lua_yield(_L, nresults);
        }

        public int LuaResume(int narg)
        {
            return LuaDLL.lua_resume(_L, narg);
        }

        public int LuaStatus()
        {
            return LuaDLL.lua_status(_L);
        }

        public void LuaGC(LuaGCOptions what, int data = 0)
        {
            LuaDLL.lua_gc(_L, what, data);
        }

        public bool LuaNext(int index)
        {
            return LuaDLL.lua_next(_L, index) != 0;
        }

        public void LuaConcat(int n)
        {
            LuaDLL.lua_concat(_L, n);
        }

        public void LuaPop(int amount)
        {
            LuaDLL.lua_pop(_L, amount);
        }

        public void LuaNewTable()
        {
            LuaDLL.lua_createtable(_L, 0 , 0);
        }

        public void LuaPushFunction(LuaCSFunction func)
        {
            IntPtr fn = Marshal.GetFunctionPointerForDelegate(func);
            LuaDLL.lua_pushcclosure(_L, fn, 0);
        }

        public bool lua_isfunction(int n)
        {
            return LuaDLL.lua_type(_L, n) == LuaTypes.LUA_TFUNCTION;
        }

        public bool lua_istable(int n)
        {
            return LuaDLL.lua_type(_L, n) == LuaTypes.LUA_TTABLE;
        }

        public bool lua_islightuserdata(int n)
        {
            return LuaDLL.lua_type(_L, n) == LuaTypes.LUA_TLIGHTUSERDATA;
        }

        public bool lua_isnil(int n)
        {
            return LuaDLL.lua_type(_L, n) == LuaTypes.LUA_TNIL;
        }

        public bool lua_isboolean(int n)
        {
            LuaTypes type = LuaDLL.lua_type(_L, n);
            return type == LuaTypes.LUA_TBOOLEAN || type == LuaTypes.LUA_TNIL;
        }

        public bool lua_isthread(int n)
        {
            return LuaDLL.lua_type(_L, n) == LuaTypes.LUA_TTHREAD;
        }

        public bool lua_isnone(int n)
        {
            return LuaDLL.lua_type(_L, n) == LuaTypes.LUA_TNONE;
        }

        public bool lua_isnoneornil(int n)
        {
            return LuaDLL.lua_type(_L, n) <= LuaTypes.LUA_TNIL;
        }

        public void LuaRawGlobal(string name)
        {
            LuaDLL.lua_pushstring(_L, name);
            LuaDLL.lua_rawget(_L, LuaIndexes.LUA_GLOBALSINDEX);
        }

        public void LuaSetGlobal(string name)
        {
            LuaDLL.lua_setglobal(_L, name);
        }

        public void LuaGetGlobal(string name)
        {
            LuaDLL.lua_getglobal(_L, name);
        }

        public void LuaOpenLibs()
        {
            LuaDLL.luaL_openlibs(_L);
        }

        public int AbsIndex(int i)
        {
            return (i > 0 || i <= LuaIndexes.LUA_REGISTRYINDEX) ? i : LuaDLL.lua_gettop(_L) + i + 1;
        }

        public int LuaGetN(int i)
        {
            return LuaDLL.luaL_getn(_L, i);
        }

        public double LuaCheckNumber(int stackPos)
        {
            return LuaDLL.luaL_checknumber(_L, stackPos);
        }

        public int LuaCheckInteger(int idx)
        {
            return LuaDLL.luaL_checkinteger(_L, idx);
        }

        public bool LuaCheckBoolean(int stackPos)
        {
            return LuaDLL.luaL_checkboolean(_L, stackPos);
        }

        public string LuaCheckLString(int numArg, out int len)
        {
            return LuaDLL.luaL_checklstring(_L, numArg, out len);
        }

        public int LuaLoadBuffer(byte[] buff, int size, string name)
        {
            return LuaDLL.luaL_loadbuffer(_L, buff, size, name);
        }

        public IntPtr LuaFindTable(int idx, string fname, int szhint = 1)
        {
            return LuaDLL.luaL_findtable(_L, idx, fname, szhint);
        }

        public int LuaTypeError(int stackPos, string tname, string t2 = null)
        {
            return LuaDLL.luaL_typerror(_L, stackPos, tname, t2);
        }

        public bool LuaDoString(string chunk, string chunkName = "LuaStatePtr.cs")
        {
            byte[] buffer = Encoding.UTF8.GetBytes(chunk);
            int status = LuaDLL.luaL_loadbuffer(_L, buffer, buffer.Length, chunkName);

            if (status != 0)
            {
                return false;                
            }

            return LuaDLL.lua_pcall(_L, 0, LuaDLL.LUA_MULTRET, 0) == 0;
            //return LuaDLL.luaL_dostring(L, chunk);
        }

        public bool LuaDoFile(string fileName)
        {
            int top = LuaGetTop();

            if (LuaDLL.luaL_dofile(_L, fileName))
            {
                return true;
            }

            string err = LuaToString(-1);
            LuaSetTop(top);
            throw new LuaException(err, LuaException.GetLastError());
        }

        public void LuaGetMetaTable(string meta)
        {
            LuaDLL.luaL_getmetatable(_L, meta);
        }

        public int LuaRef(int t)
        {
            return LuaDLL.luaL_ref(_L, t);
        }

        public void LuaGetRef(int reference)
        {
            LuaDLL.lua_getref(_L, reference);
        }

        public void LuaUnRef(int reference)
        {
            LuaDLL.lua_unref(_L, reference);
        }

        public int LuaRequire(string fileName)
        {
#if UNITY_EDITOR
            string str = Path.GetExtension(fileName);

            if (str == ".lua")
            {
                throw new LuaException("Require not need file extension: " + str);
            }
#endif
            return LuaDLL.tolua_require(_L, fileName);
        }

        //适合Awake OnSendMsg使用
        public void ThrowLuaException(Exception e)
        {
            if (LuaException.InstantiateCount > 0 || LuaException.SendMsgCount > 0)
            {
                LuaDLL.toluaL_exception(_L, e);
            }
            else
            {
                throw e;
            }
        }

        public int ToLuaRef()
        {
            return LuaDLL.toluaL_ref(_L);
        }

        public int LuaUpdate(float delta, float unscaled)
        {
            return LuaDLL.tolua_update(_L, delta, unscaled);
        }

        public int LuaLateUpdate()
        {
            return LuaDLL.tolua_lateupdate(_L);
        }

        public int LuaFixedUpdate(float fixedTime)
        {
            return LuaDLL.tolua_fixedupdate(_L, fixedTime);
        }

        public void OpenToLuaLibs()
        {
            LuaDLL.tolua_openlibs(_L);
        }

        public void ToLuaPushTraceback()
        {
            LuaDLL.tolua_pushtraceback(_L);
        }

        public void ToLuaUnRef(int reference)
        {
            LuaDLL.toluaL_unref(_L, reference);
        }
    }
}