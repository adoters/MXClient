require "Common/define"
require "Common/functions"
require "3rd/pblua/P_Protocol"

Event = require 'events'
---------------------------------------------------------------------------
--成员变量
---------------------------------------------------------------------------
Network = {}
local this = Network

---------------------------------------------------------------------------
--初始化
---------------------------------------------------------------------------
function Network.Start() 
    log("Network.Start!!")
    local Player = require "Logic/Player"   --玩家
    --注册协议响应函数
    Event.AddListener(P_Protocol.META_TYPE_S2C_PLAYER_INFO, Player.OnPlayerInfo)  --同步玩家数据
    Event.AddListener(P_Protocol.META_TYPE_S2C_SURROUNDINGS_CHANGED, Player.OnSurroundChanged)  --周围物体移动
end
---------------------------------------------------------------------------
--接收服务器发来的数据，而直接转换Meta数，从而获取stuff中的数据
---------------------------------------------------------------------------
function Network.OnSocket(buffer)
    if not buffer then return end
    local data = buffer:ReadBuffer()
    ----------------------------------------------------
    local meta = P_Protocol.Meta()
    meta:ParseFromString(data)
    log('OnSocket:type_t:>'..meta.type_t)
    if not meta or meta.type_t <= 0 or meta.type_t >= P_Protocol.META_TYPE_S2C_COUNT then return end    --非法协议
    ----------------------------------------------------
    Event.Brocast(meta.type_t, meta.stuff)
end
---------------------------------------------------------------------------
--当连接建立时
---------------------------------------------------------------------------
function Network.OnConnect() 
    logWarn("Game Server connected!!")
end
---------------------------------------------------------------------------
--异常断线
---------------------------------------------------------------------------
function Network.OnException() 
    NetManager:SendConnect()
   	logError("OnException------->>>>")
end
---------------------------------------------------------------------------
--连接中断，或者被踢掉
---------------------------------------------------------------------------
function Network.OnDisconnect() 
    logError("OnDisconnect------->>>>")
end
---------------------------------------------------------------------------
--卸载网络监听
---------------------------------------------------------------------------
function Network.Unload()
    Event.RemoveListener(P_Protocol.META_TYPE_S2C_PLAYER_INFO)
    log('Unload Network...');
end
---------------------------------------------------------------------------
--发送协议
---------------------------------------------------------------------------
function Network.SendProtocol(message)
    local meta = P_Protocol.Meta()
    meta.type_t = message.type_t
    meta.stuff = message:SerializeToString()
    ---------------------------------------------
    local msg = meta:SerializeToString()
    networkMgr:SendProtocol(msg)
end
---------------------------------------------------------------------------
return Network
