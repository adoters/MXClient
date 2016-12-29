require "Logic/LuaReader"

local AssetManager = require "Logic/AssetManager"

---------------------------------------------------------------------------
--入口函数
---------------------------------------------------------------------------
function Main()					
	--local hoster = AssetManager.Get(393227)
end
---------------------------------------------------------------------------
--场景切换通知
---------------------------------------------------------------------------
function OnLevelWasLoaded(level)
	Time.timeSinceLevelLoad = 0
end