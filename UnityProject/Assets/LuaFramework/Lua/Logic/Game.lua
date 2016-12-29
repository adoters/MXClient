local lpeg = require "lpeg"

local json = require "cjson"
local util = require "3rd/cjson/util"

require "Logic/LuaClass"
require "Logic/CtrlManager"
require "Common/functions"
require "Controller/PromptCtrl"

--管理器--
Game = {};
local this = Game;

local game; 
local transform;
local gameObject;
local WWW = UnityEngine.WWW;

function Game.InitViewPanels()
	for i = 1, #PanelNames do
		require ("View/"..tostring(PanelNames[i]))
	end
end

--初始化完成，发送链接服务器信息--
function Game.OnInitOK()
    AppConst.SocketPort = 40000;
    AppConst.SocketAddress = "10.236.100.114";
    networkMgr:SendConnect();

    --注册LuaView--
    this.InitViewPanels();

    this.test_pblua_func();
    coroutine.start(this.test_coroutine);

    CtrlManager.Init();
    local ctrl = CtrlManager.GetCtrl(CtrlNames.Prompt);
    if ctrl ~= nil and AppConst.ExampleMode == 1 then
        ctrl:Awake();
    end
    logWarn('LuaFramework InitOK--->>>');
end

--测试协同--应该叫协程
function Game.test_coroutine()    
    log("协程测试开始");
    coroutine.wait(1);	
    log("协程测试结束");
	
    local www = WWW("http://bbs.ulua.org/readme.txt");
    coroutine.www(www);
    log(www.text);    	
end

--测试pblua--
function Game.test_pblua_func()
    local msg = "hello world.";
    LuaHelper.OnCallLuaFunc(msg, this.OnPbluaCall);
end

--pblua callback--
function Game.OnPbluaCall(data)
    log(data);
end

--销毁--
function Game.OnDestroy()
	logWarn('OnDestroy--->>>');
end
