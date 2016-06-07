using System;
using System.Text;
using Assets.TinyZ.Socket;
using Assets.TinyZ.Socket.Codec;
using Google.ProtocolBuffers;
using UnityEngine;

public class SocketTest : MonoBehaviour
{
    private SimpleSocket _simpleSocket;


    private ProtobufDecoder _decoder;

    void Awake()
    {
        ExtensionRegistry registry = ExtensionRegistry.CreateInstance();
        registry.Add(ProtobufMsgEnterGame.MsgEnterGame);
        registry.Add(ProtobufMsgLogin.MsgLogin);
        registry.Add(MsgBuyItem.msgBuyItem);
        _decoder = new ProtobufDecoder(ServerMessage.DefaultInstance, registry);
    }

    // Use this for initialization
    void Start()
    {

    }

    private EventHandler<SocketEventArgs> _receiveMessageCompletedhandler;

    private bool _isNeedSend;

    private bool _isDoneConnected = true;

    // Update is called once per frame
    void Update()
    {
        if (!_isDoneConnected)
        {
            if (_simpleSocket != null && _simpleSocket.Connected)
            {
                _simpleSocket.Close();
                _simpleSocket = null;
            }
            // 
            if (!_host.Equals("") && !_port.Equals(""))
            {
                _simpleSocket = new SimpleSocket();
                _simpleSocket.Connect(_host, Int32.Parse(_port));
                _simpleSocket.ReceiveMessageCompleted -= _receiveMessageCompletedhandler;
                _simpleSocket.ReceiveMessageCompleted += _receiveMessageCompletedhandler = (s, e) =>
                {
                    var str = e.Data;
                    if (str != null)
                    {
                        _receivedMsg = Encoding.UTF8.GetString(str);
                        Debug.Log(_receivedMsg);

                        // TODO: 测试PRPTOCOL BUFFERS
//                        IMessageLite message = _decoder.Decode(str);
//                        Debug.Log(message.ToString());
                    }
                };
                _isDoneConnected = true;
            }
        }
        if (_simpleSocket != null && (_isNeedSend && _simpleSocket.Connected))
        {
            //_simpleSocket.OnSendMessage(Encoding.UTF8.GetBytes(_data));

            // TODO: 测试PRPTOCOL BUFFERS
//            MsgEnterGame msgEnterGame = MsgEnterGame.CreateBuilder().SetName("tinyz9009").SetServerId(10019).Build();
//            _simpleSocket.OnSendMessage(ProtobufMsgEnterGame.MsgEnterGame, msgEnterGame);

            _isNeedSend = false;
        }

    }

    private string _data = "";
    private string _host = "192.168.0.123";
    private string _port = "9003";


    private string _receivedMsg = "";


    void OnGUI()
    {
        // TODO: 测试消息格式： {"id":0,"uid":19881105, "sceneId": 1, "msg":"我是大好人"}

        GUI.TextField(new Rect(100, 20, 40, 20), "Host", 20);
        _host = GUI.TextField(new Rect(145, 20, 100, 20), _host, 100);
        _port = GUI.TextField(new Rect(255, 20, 100, 20), _port, 100);
        if (GUI.Button(new Rect(370, 20, 50, 20), "连接"))
        {
            _isDoneConnected = false;
        }
        if (GUI.Button(new Rect(430, 20, 50, 20), "断开"))
        {
            if (_simpleSocket != null && _simpleSocket.Connected)
            {
                _simpleSocket.Disconnect(false);
                _simpleSocket.Close();
                Debug.Log(1);
            }
        }


        _data = GUI.TextField(new Rect(100, 50, 200, 20), _data, 500);
        if (GUI.Button(new Rect(320, 50, 50, 20), "发送"))
        {
            _isNeedSend = true;
        }

        // 输出消息
        GUI.Label(new Rect(100, 200, 300, 20), "Msg:" + _receivedMsg);


        // 数据
//        _data = GUI.TextField(new Rect(80, 20, 100, 20), _data, 500);
//        if (GUI.Button(new Rect(80, 80, 50, 20), "发送"))
//        {
//            _isNeedSend = true;
//        }
//
//        if (GUI.Button(new Rect(80, 150, 50, 20), "连接服务器"))
//        {
//            _isDoneConnected = false;
//        }
    }

}