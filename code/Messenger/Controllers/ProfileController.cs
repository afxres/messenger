using Messenger.Models;
using Messenger.Modules;
using Mikodev.Network;

namespace Messenger.Controllers
{
    /// <summary>
    /// 处理用户信息
    /// </summary>
    public class ProfileController : LinkPacket
    {
        /// <summary>
        /// 向发送者返回本机的用户信息
        /// </summary>
        [Route("user.request")]
        public void Request()
        {
            PostModule.UserProfile(Source);
        }

        /// <summary>
        /// 处理传入的用户信息
        /// </summary>
        [Route("user.profile")]
        public void Profile()
        {
            var cid = Data["id"].As<int>();
            var pro = new Profile(cid)
            {
                Name = Data["name"].As<string>(),
                Text = Data["text"].As<string>(),
            };

            var buf = Data["image"].As<byte[]>();
            if (buf.Length > 0)
                pro.Image = CacheModule.SetBuffer(buf, true);
            ProfileModule.Insert(pro);
        }

        /// <summary>
        /// 处理服务器返回的用户 Id 列表
        /// </summary>
        [Route("user.list")]
        public void List()
        {
            var lst = Data.As<int[]>();
            _ = ProfileModule.Remove(lst);
        }
    }
}
