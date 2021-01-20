using Messenger.Models;
using Messenger.Modules;
using Mikodev.Logger;
using Mikodev.Network;

namespace Messenger.Controllers
{
    /// <summary>
    /// 消息处理
    /// </summary>
    public class MessageController : LinkPacket
    {
        /// <summary>
        /// 文本消息
        /// </summary>
        [Route("msg.text")]
        public void Text()
        {
            var txt = Data.As<string>();
            _ = HistoryModule.Insert(Source, Target, "text", txt);
        }

        /// <summary>
        /// 图片消息
        /// </summary>
        [Route("msg.image")]
        public void Image()
        {
            var buf = Data.As<byte[]>();
            _ = HistoryModule.Insert(Source, Target, "image", buf);
        }

        /// <summary>
        /// 提示信息
        /// </summary>
        [Route("msg.notice")]
        public void Notice()
        {
            var typ = Data["type"].As<string>();
            var par = Data["parameter"].As<string>();
            var str = typ == "share.file"
                ? $"已成功接收文件 {par}"
                : typ == "share.dir"
                    ? $"已成功接收文件夹 {par}"
                    : null;
            if (str == null)
                Log.Info($"Unknown notice type: {typ}, parameter: {par}");
            else
                _ = HistoryModule.Insert(Source, Target, "notice", str);
            return;
        }
    }
}
