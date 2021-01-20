using Messenger.Extensions;
using Messenger.Models;
using Mikodev.Binary;
using Mikodev.Logger;
using Mikodev.Network;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Net;
using System.Net.Sockets;
using System.Threading.Tasks;

namespace Messenger.Modules
{
    /// <summary>
    /// 搜索和管理服务器信息
    /// </summary>
    internal class HostModule
    {
        private const int _Timeout = 1000;

        private const string _KeyLast = "server-last";

        private const string _KeyList = "server-broadcast-list";

        private string _host = null;

        private int _port = 0;

        private readonly List<IPEndPoint> _points = new List<IPEndPoint>();

        private static readonly HostModule s_ins = new HostModule();

        public static string Name
        {
            get => s_ins._host;
            set
            {
                s_ins._host = value;
                EnvironmentModule.Update(_KeyLast, $"{value}:{s_ins._port}");
            }
        }

        public static int Port
        {
            get => s_ins._port;
            set
            {
                s_ins._port = value;
                EnvironmentModule.Update(_KeyLast, $"{s_ins._host}:{value}");
            }
        }

        internal static Host GetHostInfo(byte[] buffer, int offset, int length)
        {
            try
            {
                var rea = new Token(LinksHelper.Generator, new ReadOnlyMemory<byte>(buffer, offset, length));
                var inf = new Host()
                {
                    Protocol = rea["protocol"].As<string>(),
                    Port = rea["port"].As<int>(),
                    Name = rea["name"].As<string>(),
                    Count = rea["count"].As<int>(),
                    CountLimit = rea["limit"].As<int>(),
                };
                return inf;
            }
            catch (Exception ex)
            {
                Log.Error(ex);
                return null;
            }
        }

        /// <summary>
        /// 通过 UDP 广播从搜索列表搜索服务器
        /// </summary>
        /// <returns></returns>
        public static async Task<Host[]> Refresh()
        {
            var lst = new List<Host>();
            var soc = new Socket(AddressFamily.InterNetwork, SocketType.Dgram, ProtocolType.Udp);
            var txt = LinksHelper.Generator.Encode(new { protocol = Links.Protocol });
            var mis = new List<Task>();

            async Task _RefreshAsync()
            {
                var buf = new byte[Links.BufferLength];
                var stw = Stopwatch.StartNew();
                while (stw.ElapsedMilliseconds < _Timeout)
                {
                    var len = soc.Available;
                    if (len < 1)
                    {
                        if (stw.ElapsedMilliseconds > _Timeout)
                            break;
                        await Task.Delay(4);
                        continue;
                    }

                    var iep = new IPEndPoint(IPAddress.Any, IPEndPoint.MinPort) as EndPoint;
                    len = Math.Min(len, buf.Length);
                    len = soc.ReceiveFrom(buf, 0, len, SocketFlags.None, ref iep);
                    var inf = GetHostInfo(buf, 0, len);
                    if (inf == null || inf.Protocol != Links.Protocol)
                        continue;
                    inf.Address = ((IPEndPoint)iep).Address;
                    inf.Delay = stw.ElapsedMilliseconds;

                    if (lst.Find(r => r.Equals(inf)) != null)
                        continue;
                    lst.Add(inf);
                }
            }

            try
            {
                soc.SetSocketOption(SocketOptionLevel.Socket, SocketOptionName.Broadcast, 1);
                soc.Bind(new IPEndPoint(IPAddress.Any, 0));

                var run = Task.Run(_RefreshAsync);
                foreach (var a in s_ins._points)
                    _ = soc.SendTo(txt, a);
                await run;
            }
            catch (Exception ex) when (ex is SocketException || ex is AggregateException)
            {
                Log.Error(ex);
            }
            finally
            {
                soc.Dispose();
            }

            return lst.ToArray();
        }

        /// <summary>
        /// 读取服务器搜索列表
        /// </summary>
        [Loader(4, LoaderFlags.OnLoad)]
        public static void Load()
        {
            var lst = new List<IPEndPoint>();
            var hos = default(string);
            var pot = Links.BroadcastPort;
            var iep = new IPEndPoint(IPAddress.Broadcast, Links.BroadcastPort);

            try
            {
                var sts = EnvironmentModule.Query(_KeyList, iep.ToString());
                foreach (var s in sts.Split(new char[] { ' ' }, StringSplitOptions.RemoveEmptyEntries))
                    lst.Add(s.ToEndPointEx());

                var str = EnvironmentModule.Query(_KeyLast, $"{IPAddress.Loopback}:{Links.Port}");
                _ = Extension.ToHostEx(str, out hos, out pot);
            }
            catch (Exception ex)
            {
                Log.Error(ex);
            }

            if (lst.Count < 1)
                lst.Add(iep);

            var res = s_ins._points;
            res.Clear();
            foreach (var i in lst.Distinct())
                res.Add(i);

            s_ins._host = hos;
            s_ins._port = pot;
            return;
        }
    }
}
