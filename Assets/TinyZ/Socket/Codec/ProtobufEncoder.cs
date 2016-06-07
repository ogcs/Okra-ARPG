//     +------------------------+
//     |    Author : TinyZ      |
//     |   Data : 2014-08-20    |
//     |Ma-il : zou90512@126.com|
//     +------------------------+
//      引用资料: 
//      [推荐]protobuf-csharp-port：https://code.google.com/p/protobuf-csharp-port/ . PB最好,最完整的C#实现.使用.net 20版本即可以完美支持Unity3D 4.3x以上版本
//      protobuf-net: https://code.google.com/p/protobuf-net/ 

using Google.ProtocolBuffers;

namespace Assets.TinyZ.Socket.Codec
{
    /// <summary>
    /// Protocol Buffers 编码器
    /// </summary>
    public class ProtobufEncoder
    {
        /// <summary>
        /// [自用]Message转换为byte[]
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="generatedExtensionLite"></param>
        /// <param name="messageLite"></param>
        /// <returns></returns>
        public static byte[] ConvertMessageToByteArray<T>(GeneratedExtensionLite<ServerMessage, T> generatedExtensionLite, T messageLite) where T : IMessageLite
        {
            ServerMessage.Builder builder = ServerMessage.CreateBuilder();
            builder.SetMsgId("" + generatedExtensionLite.Number);
            builder.SetExtension(generatedExtensionLite, messageLite);
            ServerMessage serverMessage = builder.Build();
            return serverMessage.ToByteArray();
        }

        public static byte[] Encode(IMessageLite messageLite)
        {
            return messageLite.ToByteArray();
        }

        public static byte[] Encode(IBuilder builder)
        {
            return builder.WeakBuild().ToByteArray();
        }
    }
}