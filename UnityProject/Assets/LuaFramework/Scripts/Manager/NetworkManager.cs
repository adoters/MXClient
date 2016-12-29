using UnityEngine;
using System;
using System.Collections;
using System.Collections.Generic;
using LuaInterface;

namespace LuaFramework {
    public class NetworkManager : Manager {

        private SocketClient _socket;
        static readonly object _lock = new object();
        static Queue<ByteBuffer> _events = new Queue<ByteBuffer>();

        SocketClient SocketClient {
            get { 
                if (_socket == null) _socket = new SocketClient();
                return _socket;                    
            }
        }

        void Awake() {
            Init();
        }

        void Init() {
            SocketClient.OnRegister();
        }

        public void OnInit() {
            CallMethod("Start");
        }

        public void Unload() {
            CallMethod("Unload");
        }

        //ִ��Lua����
        public object[] CallMethod(string func, params object[] args) {
            return Util.CallMethod("Network", func, args);
        }

        public static void AddEvent(ByteBuffer data) {
            lock (_lock) {
                _events.Enqueue(data);
            }
        }

        void Update() {
            if (_events.Count > 0) {
                while (_events.Count > 0) {
                    ByteBuffer data = _events.Dequeue();
                    facade.SendMessageCommand(NotiConst.DISPATCH_MESSAGE, data);
                }
            }
        }

        //������������
        public void SendConnect() {
            SocketClient.SendConnect();
        }

        //��������
        public void SendMessage(ByteBuffer buffer) {
            SocketClient.SendMessage(buffer);
        }

        public void SendProtocol(string meta) {
            SocketClient.SendProtocol(meta);
        }

        //��������
        void OnDestroy() {
            SocketClient.OnRemove();
            Debug.Log("~NetworkManager was destroy");
        }
    }
}