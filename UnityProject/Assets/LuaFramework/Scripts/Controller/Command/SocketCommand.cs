using UnityEngine;
using System.Collections;
using System.Collections.Generic;
using LuaFramework;

public class SocketCommand : ControllerCommand {

    public override void Execute(IMessage message) {
        object data = message.Body;
        if (data == null) return;
        ByteBuffer buffer = (ByteBuffer)data;
        if (buffer == null) return;
        //调用LUA函数
        Util.CallMethod("Network", "OnSocket", buffer); 
	}
}
