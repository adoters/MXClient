require "Common/define"
require "3rd/pblua/P_Protocol"
local Network = require "Logic/Network"
local Event = require 'Event'

---------------------------------------------------------------------------
--成员变量
---------------------------------------------------------------------------
local Player = {}
local this = Player

local _player = nil --玩家数据
---------------------------------------------------------------------------
--获取玩家ID
---------------------------------------------------------------------------
function Player.GetID()
	return _player.common_prop.entity_id
end

---------------------------------------------------------------------------
--加载场景
---------------------------------------------------------------------------
function Player.LoadScene()
	--resMgr:LoadPrefab("Scene", { 'PromptItem' }, this.OnLoadSuccess);
end

function Player.OnLoadSuccess()
	this.CmdEnterScene()
end
---------------------------------------------------------------------------
--发送协议
---------------------------------------------------------------------------
function Player.SendProtocol(message)
	Network:SendProtocol(message)
end
---------------------------------------------------------------------------
--进入游戏
---------------------------------------------------------------------------
function Player.CmdEnterGame(player_id)
    local enter_game = P_Protocol.EnterGame()
    enter_game.player_id = player_id
    -------------------------------------------
    this.SendProtocol(enter_game)
end
---------------------------------------------------------------------------
--进入场景
---------------------------------------------------------------------------
function Player.CmdEnterScene(scene_id)
    local enter_scene = P_Protocol.EnterScene()
    enter_scene.scene_id = scene_id
    ---------------------------------------------
    this.SendProtocol(enter_scene)
end

--[[
	协议处理：
	
	1.在Network中注册接收协议对应的响应函数；
	
	2.在本文件中实现响应函数；
--]]

---------------------------------------------------------------------------
--同步玩家数据
---------------------------------------------------------------------------
function Player.OnPlayerInfo(stuff)
	log("收到玩家数据...")
    local player_info = P_Protocol.PlayerInfo()
    player_info:ParseFromString(stuff)
    print(stuff)
    --更新玩家数据
    print(player_info)
    _player = player_info.player
    print(_player)
 	--设置玩家ID
    print(_player.common_prop.entity_id)
    print('Load data success, player_id:>'.._player.common_prop.entity_id)
end
---------------------------------------------------------------------------
--周围物体、玩家移动
---------------------------------------------------------------------------
function Player.OnSurroundChanged(stuff)
    logWarn('OnSurroundChanged...');
    local surrouds = P_Protocol.SurroundingsChanged()
    surrouds:ParseFromString(stuff)
    
    local entity_id = surrouds.entity_id
    logWarn(entity_id)
end
---------------------------------------------------------------------------
return Player