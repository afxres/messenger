using Messenger.Models;
using Mikodev.Binary;
using Mikodev.Network;
using System.IO;
using System.Windows;

namespace Messenger.Modules
{
    internal class PostModule
    {
        public static void Text(int dst, string val)
        {
            var buf = LinksHelper.Generator.Encode(new
            {
                source = LinkModule.Id,
                target = dst,
                path = "msg.text",
                data = val,
            });
            LinkModule.Enqueue(buf);
            _ = HistoryModule.Insert(dst, "text", val);
        }

        public static void Image(int dst, byte[] val)
        {
            var buf = LinksHelper.Generator.Encode(new
            {
                source = LinkModule.Id,
                target = dst,
                path = "msg.image",
                data = val,
            });
            LinkModule.Enqueue(buf);
            _ = HistoryModule.Insert(dst, "image", val);
        }

        /// <summary>
        /// Post feedback message
        /// </summary>
        public static void Notice(int dst, string genre, string arg)
        {
            var buf = LinksHelper.Generator.Encode(new
            {
                source = LinkModule.Id,
                target = dst,
                path = "msg.notice",
                data = new
                {
                    type = genre,
                    parameter = arg,
                },
            });
            LinkModule.Enqueue(buf);
            // you don't have to notice yourself in history module
        }

        /// <summary>
        /// 向指定用户发送本机用户信息
        /// </summary>
        public static void UserProfile(int dst)
        {
            var pro = ProfileModule.Current;
            var buf = LinksHelper.Generator.Encode(new
            {
                source = LinkModule.Id,
                target = dst,
                path = "user.profile",
                data = new
                {
                    id = ProfileModule.Id,
                    name = pro.Name,
                    text = pro.Text,
                    image = ProfileModule.ImageBuffer,
                },
            });
            LinkModule.Enqueue(buf);
        }

        public static void UserRequest()
        {
            var buf = LinksHelper.Generator.Encode(new
            {
                source = LinkModule.Id,
                target = Links.Id,
                path = "user.request",
            });
            LinkModule.Enqueue(buf);
        }

        /// <summary>
        /// 发送请求监听的用户组
        /// </summary>
        public static void UserGroups()
        {
            var buf = LinksHelper.Generator.Encode(new
            {
                source = LinkModule.Id,
                target = Links.Id,
                path = "user.group",
                data = ProfileModule.GroupIds,
            });
            LinkModule.Enqueue(buf);
        }

        /// <summary>
        /// 发送文件信息
        /// </summary>
        public static void File(int dst, string filepath)
        {
            var sha = new Share(new FileInfo(filepath));
            Application.Current.Dispatcher.Invoke(() => ShareModule.ShareList.Add(sha));
            var buf = LinksHelper.Generator.Encode(new
            {
                source = LinkModule.Id,
                target = dst,
                path = "share.info",
                data = new
                {
                    key = sha._key,
                    type = "file",
                    name = sha.Name,
                    length = sha.Length,
                    endpoints = LinkModule.GetEndPoints(),
                }
            });
            LinkModule.Enqueue(buf);
            _ = HistoryModule.Insert(dst, "share", sha);
        }

        public static void Directory(int dst, string directory)
        {
            var sha = new Share(new DirectoryInfo(directory));
            Application.Current.Dispatcher.Invoke(() => ShareModule.ShareList.Add(sha));
            var buf = LinksHelper.Generator.Encode(new
            {
                source = LinkModule.Id,
                target = dst,
                path = "share.info",
                data = new
                {
                    key = sha._key,
                    type = "dir",
                    name = sha.Name,
                    endpoints = LinkModule.GetEndPoints(),
                }
            });
            LinkModule.Enqueue(buf);
            _ = HistoryModule.Insert(dst, "share", sha);
        }
    }
}
