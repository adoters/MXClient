require "Common/define"

require "3rd/pblua/P_Protocol"

PromptCtrl = {};
local this = PromptCtrl;

local panel;
local prompt;
local transform;
local gameObject;
---------------------------------------------------------------------------
--构建函数
---------------------------------------------------------------------------
function PromptCtrl.New()
	logWarn("PromptCtrl.New--->>");
	return this;
end

function PromptCtrl.Awake()
	logWarn("PromptCtrl.Awake--->>");
	panelMgr:CreatePanel('Prompt', this.OnCreate);
end
---------------------------------------------------------------------------
--启动事件
---------------------------------------------------------------------------
function PromptCtrl.OnCreate(obj)
	gameObject = obj;
	transform = obj.transform;

	panel = transform:GetComponent('UIPanel');
	prompt = transform:GetComponent('LuaBehaviour');
	logWarn("Start lua--->>"..gameObject.name);

	prompt:AddClick(PromptPanel.btnOpen, this.OnClick);
	resMgr:LoadPrefab('prompt', { 'PromptItem' }, this.InitPanel);
end
---------------------------------------------------------------------------
--初始化面板
---------------------------------------------------------------------------
function PromptCtrl.InitPanel(objs)
	local count = 100; 
	local parent = PromptPanel.gridParent;
	for i = 1, count do
		local go = newObject(objs[0]);
		go.name = 'Item'..tostring(i);
		go.transform:SetParent(parent);
		go.transform.localScale = Vector3.one;
		go.transform.localPosition = Vector3.zero;
        prompt:AddClick(go, this.OnItemClick);

	    local label = go.transform:FindChild('Text');
	    label:GetComponent('Text').text = tostring(i);
	end
end
---------------------------------------------------------------------------
--滚动项单击
---------------------------------------------------------------------------
function PromptCtrl.OnItemClick(go)
    log(go.name);
    
    this.TestSendPblua()
end
---------------------------------------------------------------------------
--单击事件
---------------------------------------------------------------------------
function PromptCtrl.OnClick(go)
	logWarn("OnClick---->>>"..go.name);
end
---------------------------------------------------------------------------
--测试发送PBLUA
---------------------------------------------------------------------------
function PromptCtrl.TestSendPblua()
    local meta = P_Protocol.Meta()
    meta.type_t = P_Protocol.META_TYPE_C2S_ENTER_GAME
    ---------------------------------------------
    local enter_game = P_Protocol.EnterGame()
    enter_game.player_id = 20
    meta.stuff = enter_game:SerializeToString()
    ---------------------------------------------
    local msg = meta:SerializeToString()
    networkMgr:SendProtocol(msg);
end
---------------------------------------------------------------------------
--关闭事件
---------------------------------------------------------------------------
function PromptCtrl.Close()
	panelMgr:ClosePanel(CtrlNames.Prompt);
end