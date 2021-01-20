using System.Net;

namespace Messenger.Models
{
    /// <summary>
    /// 服务器信息
    /// </summary>
    public class Host
    {
        /// <summary>
        /// 协议字符串
        /// </summary>
        public string Protocol { get; set; }

        /// <summary>
        /// 名称
        /// </summary>
        public string Name { get; set; }

        /// <summary>
        /// 端口
        /// </summary>
        public int Port { get; set; }

        /// <summary>
        /// 服务器当前连接的客户端数
        /// </summary>
        public int Count { get; set; }

        /// <summary>
        /// 服务器最大客户端数
        /// </summary>
        public int CountLimit { get; set; }

        /// <summary>
        /// 访问延迟 (单次往返耗时 误差较大)
        /// </summary>
        public long Delay { get; set; } = 0;

        /// <summary>
        /// IP 地址
        /// </summary>
        public IPAddress Address { get; set; } = null;

        /// <summary>
        /// 依据 IP 地址和端口比较两个对象
        /// </summary>
        public override bool Equals(object obj)
        {
            if (obj == this)
                return true;
            var info = obj as Host;
            if (info == null)
                return false;
            if (Port != info.Port)
                return false;
            if (Address == info.Address)
                return true;
            if (Address == null || info.Address == null)
                return false;
            return Address.Equals(info.Address);
        }

        /// <summary>
        /// 调用 <see cref="IPEndPoint.GetHashCode"/>
        /// </summary>
        public override int GetHashCode()
        {
            var add = Address;
            return add != null
                ? new IPEndPoint(add, Port).GetHashCode()
                : 0;
        }
    }
}
