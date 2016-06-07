// +------------------------+
// |    Author : TinyZ      |
// |   Data : 2014-08-20    |
// |Ma-il : zou90512@126.com|
// |     Version : 1.1      |
// +------------------------+
// 注释： 笔者这里实现了一个基于长度的解码器。用于避免粘包等问题。编码时候的长度描述数字的默认为short类型（长度2字节）。解码时候的长度描述数字默认为int类型（长度4字节）

// GOOGLE_PROTOCOL_BUFFERS : 
//      是否支持Google的Protocol Buffers. 作者自己使用的.
//      Define request Google protocol buffers
//      Example: #define GOOGLE_PROTOCOL_BUFFERS
//      相关资料: 
//      [推荐]protobuf-csharp-port：https://code.google.com/p/protobuf-csharp-port/ . PB最好,最完整的C#实现.使用.net 20版本即可以完美支持Unity3D 4.3x以上版本
//      protobuf-net: https://code.google.com/p/protobuf-net/ 
#define GOOGLE_PROTOCOL_BUFFERS
//
// BIG_ENDIANESS :
//      是否是大端存储. 是=>使用大端存储,将使用Misc类库提供的大端存储工具EndianBitConverter. 否=>使用.Net提供的BitConverter
//      Define is socket endianess is big-endianess(大端) . If endianess is Big-endianess use EndianBitConverter , else use BitConverter
//      相关资料:
//      Miscellaneous Utility Library类库官网: http://www.yoda.arachsys.com/csharp/miscutil/
#define BIG_ENDIANESS

using System;
using System.IO;
using System.Net;
using System.Net.Sockets;
#if BIG_ENDIANESS
using MiscUtil.Conversion;
#endif
#if GOOGLE_PROTOCOL_BUFFERS
using Google.ProtocolBuffers;
using Assets.TinyZ.Socket.Codec;
#endif

namespace Assets.TinyZ.Socket
{
    /// <summary>
    /// 简单的异步Socket实现. 用于Unity3D客户端与JAVA服务端的数据通信.
    /// 
    /// <br/><br/>方法：<br/>
    /// Connect：用于连接远程指定端口地址,连接成功后开启消息接收监听<br/>
    /// OnSendMessage：用于发送字节流消息. 长度不能超过short[65535]的长度<br/>
    /// <br/>事件：<br/>
    /// ReceiveMessageCompleted： 用于回调. 返回接收到的根据基于长度的解码器解码之后获取的数据[字节流]
    /// SendMessageCompleted:   用于回调. 当消息发送完成时
    /// ConnectCompleted： 用于回调. 当成功连接到远程网络地址后调用
    /// 
    /// <br/><br/>
    /// 服务器为JAVA开发。因此编码均为 BigEndian编码
    /// 消息的字节流格式如下：<br/>
    ///     * +------------+-------------+ <br/>
    ///     * |消息程度描述|  内容       | <br/>
    ///     * |    0x04    | ABCD        | <br/>
    ///     * +------------+-------------+ <br/>
    /// 注释: 消息头为消息内容长度描述,后面是相应长度的字节内容. 
    /// <br/><br/>
    /// </summary>
    /// <example>
    /// <code>
    /// // Unity3D客户端示例代码如下:
    /// var _simpleSocket = new SimpleSocket();
    /// _simpleSocket.Connect("127.0.0.1", 9003);
    /// _simpleSocket.ReceiveMessageCompleted += (s, e) =>
    /// {
    ///     var rmc = e as SocketEventArgs;
    ///     if (rmc == null) return;
    ///     var data = rmc.Data as byte[];
    ///     if (data != null)
    ///     {
    ///         // 在Unity3D控制台输出接收到的UTF-8格式字符串 
    ///         Debug.Log(Encoding.UTF8.GetString(data));
    ///     }
    //      _count++;
    /// };
    /// 
    /// // Unity3D客户端发送消息：
    /// _simpleSocket.OnSendMessage(Encoding.UTF8.GetBytes("Hello World!"));
    /// </code>
    /// </example>
    public class SimpleSocket
    {
        #region Construct

        /// <summary>
        /// Socket
        /// </summary>
        private readonly System.Net.Sockets.Socket _socket;

        /// <summary>
        /// SimpleSocket的构造函数
        /// </summary>
        public SimpleSocket()
        {
            _socket = new System.Net.Sockets.Socket(AddressFamily.InterNetwork, SocketType.Stream, ProtocolType.Tcp);
            _socket.SetSocketOption(SocketOptionLevel.Socket, SocketOptionName.KeepAlive, true);
            //_socket.Blocking = false; // ?
        }

        /// <summary>
        /// 初始化Socket, 并设置帧长度
        /// </summary>
        /// <param name="encoderLengthFieldLength">编码是消息长度数字的字节数长度. 1：表示1byte  2：表示2byte[Short类型] 4：表示4byte[int类型] 8：表示8byte[long类型]</param>
        /// <param name="decoderLengthFieldLength">解码时消息长度数字的字节数长度. 1：表示1byte  2：表示2byte[Short类型] 4：表示4byte[int类型] 8：表示8byte[long类型]</param>
        public SimpleSocket(int encoderLengthFieldLength, int decoderLengthFieldLength) : this()
        {
            _encoderLengthFieldLength = encoderLengthFieldLength;
            _decoderLengthFieldLength = decoderLengthFieldLength;
        }

        #endregion


        #region Connect to remote host

        /// <summary>
        /// 连接远程地址完成事件
        /// </summary>
        public event EventHandler<SocketEventArgs> ConnectCompleted;

        /// <summary>
        /// 是否连接状态
        /// </summary>
        /// <see cref="Socket.Connected"/>
        public bool Connected
        {
            get { return _socket != null && _socket.Connected; }
        }

        /// <summary>
        /// 连接指定的远程地址
        /// </summary>
        /// <param name="host">远程地址</param>
        /// <param name="port">端口</param>
        public void Connect(string host, int port)
        {
            _socket.BeginConnect(host, port, OnConnectCallBack, this);
        }

        /// <summary>
        /// 连接指定的远程地址
        /// </summary>
        /// <param name="ipAddress">目标网络协议ip地址</param>
        /// <param name="port">目标端口</param>
        /// 查看:<see cref="IPAddress"/>
        public void Connect(IPAddress ipAddress, int port)
        {
            _socket.BeginConnect(ipAddress, port, OnConnectCallBack, this);
        }

        /// <summary>
        /// 连接端点
        /// </summary>
        /// <param name="endPoint">端点, 标识网络地址</param>
        /// 查看:<see cref="EndPoint"/>
        public void Connect(EndPoint endPoint)
        {
            _socket.BeginConnect(endPoint, OnConnectCallBack, this);
        }

        /// <summary>
        /// 连接的回调函数
        /// </summary>
        /// <param name="ar"></param>
        private void OnConnectCallBack(IAsyncResult ar)
        {
            if (!_socket.Connected) return;
            _socket.EndConnect(ar);
            if (ConnectCompleted != null)
            {
                ConnectCompleted(this, new SocketEventArgs());
            }
            StartReceive();
        }

        #endregion


        #region Send Message

        /// <summary>
        /// 发送消息完成
        /// </summary>
        public event EventHandler<SocketEventArgs> SendMessageCompleted;

        /// <summary>
        /// 编码时长度描述数字的字节长度[default = 2 => 65535字节]
        /// </summary>
        private readonly int _encoderLengthFieldLength = 2;

        /// <summary>
        /// 发送消息
        /// </summary>
        /// <param name="data">要传递的消息内容[字节数组]</param>
        public void OnSendMessage(byte[] data)
        {
            var stream = new MemoryStream();
            switch (_encoderLengthFieldLength)
            {
                case 1:
                    stream.Write(new[] {(byte) data.Length}, 0, 1);
                    break;
#if BIG_ENDIANESS
                case 2:
                    stream.Write(EndianBitConverter.Big.GetBytes((short) data.Length), 0, 2);
                    break;
                case 4:
                    stream.Write(EndianBitConverter.Big.GetBytes(data.Length), 0, 4);
                    break;
                case 8:
                    stream.Write(EndianBitConverter.Big.GetBytes((long) data.Length), 0, 8);
                    break;
#else
                case 2:
                    stream.Write(BitConverter.GetBytes((short) data.Length), 0, 2);
                    break;
                case 4:
                    stream.Write(BitConverter.GetBytes(data.Length), 0, 4);
                    break;
                case 8:
                    stream.Write(BitConverter.GetBytes((long) data.Length), 0, 8);
                    break;
#endif

                default:
                    throw new Exception("unsupported decoderLengthFieldLength: " + _encoderLengthFieldLength +
                                        " (expected: 1, 2, 3, 4, or 8)");
            }
            stream.Write(data, 0, data.Length);
            var all = stream.ToArray();
            stream.Close();
            _socket.BeginSend(all, 0, all.Length, SocketFlags.None, OnSendMessageComplete, all);
        }

        /// <summary>
        /// 发送消息完成的回调函数
        /// </summary>
        /// <param name="ar"></param>
        private void OnSendMessageComplete(IAsyncResult ar)
        {
            var data = ar.AsyncState as byte[];
            SocketError socketError;
            _socket.EndSend(ar, out socketError);
            if (socketError != SocketError.Success)
            {
                _socket.Disconnect(false);
                throw new SocketException((int)socketError);
            }
            if (SendMessageCompleted != null)
            {
                SendMessageCompleted(this, new SocketEventArgs(data));
            }
            //Debug.Log("Send message successful !");
        }

        #endregion


        #region Receive Message

        /// <summary>
        /// the length of the length field. 长度字段的字节长度, 用于长度解码 
        /// </summary>
        private readonly int _decoderLengthFieldLength = 4;

        /// <summary>
        /// 事件消息接收完成
        /// </summary>
        public event EventHandler<SocketEventArgs> ReceiveMessageCompleted;

        /// <summary>
        /// 开始接收消息
        /// </summary>
        private void StartReceive()
        {
            if (!_socket.Connected) return;
            var buffer = new byte[_decoderLengthFieldLength];
            _socket.BeginReceive(buffer, 0, _decoderLengthFieldLength, SocketFlags.None, OnReceiveFrameLengthComplete, buffer);
        }

        /// <summary>
        /// 实现帧长度解码.避免粘包等问题
        /// </summary>
        private void OnReceiveFrameLengthComplete(IAsyncResult ar)
        {
            var frameLength = (byte[]) ar.AsyncState;
            // 帧长度 
#if BIG_ENDIANESS
            var length = EndianBitConverter.Big.ToInt32(frameLength, 0);
#else
            var length = BitConverter.ToInt32(frameLength, 0);
#endif
            var data = new byte[length];
            _socket.BeginReceive(data, 0, length, SocketFlags.None, OnReceiveDataComplete, data);
        }

        /// <summary>
        /// 数据接收完成的回调函数
        /// </summary>
        private void OnReceiveDataComplete(IAsyncResult ar)
        {
            _socket.EndReceive(ar);
            var data = ar.AsyncState as byte[];
            // 触发接收消息事件
            if (ReceiveMessageCompleted != null)
            {
                ReceiveMessageCompleted(this, new SocketEventArgs(data));
            }
            StartReceive();
        }

        #endregion


        #region Close and Disconnect

        /// <summary>
        /// 关闭 <see cref="Socket"/> 连接并释放所有关联的资源
        /// </summary>
        public void Close()
        {
            _socket.Close();
        }

        /// <summary>
        /// 关闭套接字连接并允许重用套接字。
        /// </summary>
        /// <param name="reuseSocket">如果关闭当前连接后可以重用此套接字，则为 true；否则为 false。 </param>
        public void Disconnect(bool reuseSocket)
        {
            _socket.Disconnect(reuseSocket);
        }

        #endregion


        #region Protocol Buffers Utility [Request ： Google Protocol Buffers version 2.5]

#if GOOGLE_PROTOCOL_BUFFERS

        /// <summary>
        /// 发送消息
        /// </summary>
        /// <typeparam name="T">IMessageLite的子类</typeparam>
        /// <param name="generatedExtensionLite">消息的扩展信息</param>
        /// <param name="messageLite">消息</param>
        public void OnSendMessage<T>(GeneratedExtensionLite<ServerMessage, T> generatedExtensionLite, T messageLite)
            where T : IMessageLite
        {
            var data = ProtobufEncoder.ConvertMessageToByteArray(generatedExtensionLite, messageLite);
            OnSendMessage(data);
        }

#endif

        #endregion
    }

    #region Event

    /// <summary>
    /// Simple socket event args
    /// </summary>
    public class SocketEventArgs : EventArgs
    {

        public SocketEventArgs()
        {
        }

        public SocketEventArgs(byte[] data) : this()
        {
            Data = data;
        }

        /// <summary>
        /// 相关的数据
        /// </summary>
        public byte[] Data { get; private set; }

    }

    #endregion

}