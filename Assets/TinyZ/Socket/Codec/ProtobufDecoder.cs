//     +------------------------+
//     |    Author : TinyZ      |
//     |   Data : 2014-08-20    |
//     |Ma-il : zou90512@126.com|
//     +------------------------+
//      引用资料: 
//      [推荐]protobuf-csharp-port：https://code.google.com/p/protobuf-csharp-port/ . PB最好,最完整的C#实现.使用.net 20版本即可以完美支持Unity3D 4.3x以上版本
//      protobuf-net: https://code.google.com/p/protobuf-net/ 


using System;
using Google.ProtocolBuffers;

namespace Assets.TinyZ.Socket.Codec
{
    /// <summary>
    /// Protocol Buffers 解码器
    /// </summary>
    public class ProtobufDecoder
    {
        private readonly IMessageLite _prototype;

        /// <summary>
        /// 扩展消息注册
        /// </summary>
        private readonly ExtensionRegistry _extensionRegistry;

        public ProtobufDecoder(IMessageLite prototype)
        {
            _prototype = prototype.WeakDefaultInstanceForType;
        }

        public ProtobufDecoder(IMessageLite prototype, ExtensionRegistry extensionRegistry)
            : this(prototype)
        {
            _extensionRegistry = extensionRegistry;
        }

        /// <summary>
        /// 注册扩展
        /// </summary>
        /// <param name="extension">protobuf扩展消息</param>
        public void RegisterExtension(IGeneratedExtensionLite extension)
        {
            if (_extensionRegistry == null)
            {
                throw new Exception("ExtensionRegistry must using InitProtobufDecoder method to initialize. ");
            }
            _extensionRegistry.Add(extension);
        }

        /// <summary>
        /// 解码
        /// </summary>
        /// <param name="data">protobuf编码字节数组</param>
        /// <returns>返回解码之后的消息</returns>
        public IMessageLite Decode(byte[] data)
        {
            if (_prototype == null)
            {
                throw new Exception("_prototype must using InitProtobufDecoder method to initialize.");
            }
            IMessageLite message;
            if (_extensionRegistry == null)
            {
                message = (_prototype.WeakCreateBuilderForType().WeakMergeFrom(ByteString.CopyFrom(data))).WeakBuild();
            }
            else
            {
                message =
                    (_prototype.WeakCreateBuilderForType().WeakMergeFrom(ByteString.CopyFrom(data), _extensionRegistry))
                        .WeakBuild();
            }
            if (message == null)
            {
                throw new Exception("Decode message failed");
            }
            return message;
        }
    }
}