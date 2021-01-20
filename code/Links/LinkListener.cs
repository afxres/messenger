using Mikodev.Binary;
using Mikodev.Logger;
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Net.Sockets;
using System.Security.Cryptography;
using System.Threading.Tasks;

namespace Mikodev.Network
{
    public sealed partial class LinkListener
    {
        internal const int _NoticeDelay = 100;

        internal static readonly TimeSpan _NoticeInterval = TimeSpan.FromMilliseconds(1000);

        internal readonly object _locker = new object();

        internal readonly string _sname = null;

        internal readonly int _climit = Links.ServerSocketLimit;

        internal readonly int _port = Links.Port;

        internal readonly Socket _broadcast = null;

        internal readonly Socket _socket = null;

        internal readonly LinkNoticeSource _notice = new LinkNoticeSource(_NoticeInterval);

        internal readonly ConcurrentDictionary<int, LinkClient> _clients = new ConcurrentDictionary<int, LinkClient>();

        internal readonly ConcurrentDictionary<int, HashSet<int>> _joined = new ConcurrentDictionary<int, HashSet<int>>();

        internal readonly ConcurrentDictionary<int, ConcurrentDictionary<int, LinkClient>> _set = new ConcurrentDictionary<int, ConcurrentDictionary<int, LinkClient>>();

        private LinkListener(Socket socket, Socket broadcast, int port, int count, string name)
        {
            _socket = socket;
            _broadcast = broadcast;
            _port = port;
            _climit = count;
            _sname = name;
        }

        public static async Task Run(IPAddress address, int port = Links.Port, int broadcast = Links.BroadcastPort, int count = Links.ServerSocketLimit, string name = null)
        {
            if (address == null)
                throw new ArgumentNullException(nameof(address));
            if (count < 1 || count > Links.ServerSocketLimit)
                throw new ArgumentOutOfRangeException(nameof(count), "Count limit out of range!");
            var iep = new IPEndPoint(address, port);
            var bep = new IPEndPoint(address, broadcast);
            var soc = new Socket(address.AddressFamily, SocketType.Stream, ProtocolType.Tcp);
            var bro = new Socket(address.AddressFamily, SocketType.Dgram, ProtocolType.Udp);

            try
            {
                if (string.IsNullOrEmpty(name))
                    name = Dns.GetHostName();
                soc.Bind(iep);
                bro.Bind(bep);
                soc.Listen(count);

                var lis = new LinkListener(soc, bro, port, count, name);
                await Task.WhenAll(new Task[]
                {
                    Task.Run(lis._Notice),
                    Task.Run(lis._Listen),
                    Task.Run(lis._Broadcast),
                });
            }
            catch (Exception)
            {
                soc.Dispose();
                bro.Dispose();
                throw;
            }
        }

        private async Task _Listen()
        {
            void _Invoke(Socket soc) => Task.Run(async () =>
            {
                try
                {
                    _ = soc.SetKeepAlive();
                    await await _AcceptClient(soc);
                }
                finally
                {
                    soc.Dispose();
                }
            });

            while (true)
            {
                _Invoke(await _socket.AcceptAsyncEx());
            }
        }

        private async Task<Task> _AcceptClient(Socket socket)
        {
            LinkError _Check(int id)
            {
                if ((Links.Id < id && id < Links.DefaultId) == false)
                    return LinkError.IdInvalid;
                if (_clients.Count >= _climit)
                    return LinkError.CountLimited;
                return _clients.TryAdd(id, null) ? LinkError.Success : LinkError.IdConflict;
            }

            var key = LinkCrypto.GetKey();
            var blk = LinkCrypto.GetBlock();
            var err = LinkError.None;
            var cid = 0;
            var iep = default(IPEndPoint);
            var oep = default(IPEndPoint);

            byte[] _Response(byte[] buf)
            {
                var rea = new Token(LinksHelper.Generator, buf);
                if (string.Equals(rea["protocol"].As<string>(), Links.Protocol, StringComparison.InvariantCultureIgnoreCase) == false)
                    throw new LinkException(LinkError.ProtocolMismatch);
                cid = rea["source"].As<int>();
                var mod = rea["rsa"]["modulus"].As<byte[]>();
                var exp = rea["rsa"]["exponent"].As<byte[]>();
                iep = rea["endpoint"].As<IPEndPoint>();
                oep = (IPEndPoint)socket.RemoteEndPoint;
                err = _Check(cid);
                var rsa = RSA.Create();
                var par = new RSAParameters() { Exponent = exp, Modulus = mod };
                rsa.ImportParameters(par);
                var packet = LinksHelper.Generator.Encode(new
                {
                    result = err,
                    endpoint = oep,
                    aes = new
                    {
                        key = rsa.Encrypt(key, RSAEncryptionPadding.OaepSHA1),
                        iv = rsa.Encrypt(blk, RSAEncryptionPadding.OaepSHA1),
                    }
                });
                return packet;
            }

            try
            {
                var buf = await socket.ReceiveAsyncExt().TimeoutAfter("Listener request timeout.");
                var res = _Response(buf);
                await socket.SendAsyncExt(res).TimeoutAfter("Listener response timeout.");
                err.AssertError();
            }
            catch (Exception)
            {
                if (err == LinkError.Success)
                    _clients.TryRemove(cid, out var val).AssertFatal(val == null, "Failed to remove placeholder!");
                throw;
            }

            var clt = new LinkClient(cid, socket, iep, oep, key, blk);
            _clients.TryUpdate(cid, clt, null).AssertFatal("Failed to update client!");

            clt.Received += _ClientReceived;
            clt.Disposed += _ClientDisposed;
            return clt.Start();
        }

        private void _Refresh(LinkClient client, IEnumerable<int> groups = null)
        {
            /* Require lock */
            var cid = client._id;
            var set = _joined.GetOrAdd(cid, _ => new HashSet<int>());
            foreach (var i in set)
            {
                _set.TryGetValue(i, out var gro).AssertFatal("Failed to get group collection!");
                gro.TryRemove(cid, out var val).AssertFatal(val == client, "Failed to remove client from group collection!");
                if (gro.Count > 0)
                    continue;
                _set.TryRemove(i, out var res).AssertFatal(res == gro, "Failed to remove empty group!");
            }

            if (groups == null)
            {
                /* Client shutdown */
                _joined.TryRemove(cid, out var res).AssertFatal(res == set, "Failed to remove group set!");
                _clients.TryRemove(cid, out var val).AssertFatal(val == client, "Failed to remove client!");
                return;
            }

            // Union with -> add range
            set.Clear();
            set.UnionWith(groups);
            foreach (var i in groups)
            {
                var gro = _set.GetOrAdd(i, _ => new ConcurrentDictionary<int, LinkClient>());
                gro.TryAdd(cid, client).AssertFatal("Failed to add client to group collection!");
            }
        }

        private async Task _Notice()
        {
            while (true)
            {
                await Task.Delay(_NoticeDelay);
                var res = _notice.Notice();
                if (res.IsAny == false)
                    continue;

                var lst = _clients.Where(r => r.Value != null).Select(r => r.Key).ToList();
                var buf = LinksHelper.Generator.Encode(new
                {
                    source = Links.Id,
                    target = Links.Id,
                    path = "user.list",
                    data = lst,
                });

                foreach (var clt in _clients.Values)
                    clt?.Enqueue(buf);
                res.Handled();
            }
        }

        private void _ClientDisposed(object sender, EventArgs e)
        {
            var clt = (LinkClient)sender;

            lock (_locker)
                _Refresh(clt);
            _notice.Update();

            clt.Received -= _ClientReceived;
            clt.Disposed -= _ClientDisposed;
        }

        private void _ClientReceived(object sender, LinkEventArgs<LinkPacket> arg)
        {
            var obj = arg.Object;
            var src = obj.Source;
            var tar = obj.Target;
            var buf = obj.Buffer;

            if (tar == Links.Id)
            {
                if (obj.Path == "user.group")
                {
                    var lst = obj.Data.As<List<int>>().Where(r => r < Links.Id);
                    var set = new HashSet<int>(lst);
                    if (set.Count > Links.GroupLabelLimit)
                        throw new LinkException(LinkError.GroupLimited);
                    var clt = (LinkClient)sender;
                    lock (_locker)
                        _Refresh(clt, set);
                    return;
                }

                foreach (var val in _clients.Values)
                    if (val != null && val._id != src)
                        val.Enqueue(buf);
                return;
            }
            else if (tar > Links.Id)
            {
                // Thread safe operation
                if (_clients.TryGetValue(tar, out var val))
                    val?.Enqueue(buf);
                return;
            }
            else
            {
                // Thread safe operation
                if (_set.TryGetValue(tar, out var grp))
                    foreach (var val in grp.Values)
                        if (val != null && val._id != src)
                            val.Enqueue(buf);
                return;
            }
        }

        private async Task _Broadcast()
        {
            while (true)
            {
                var ava = _broadcast.Available;
                if (ava < 1)
                {
                    await Task.Delay(Links.Delay);
                    continue;
                }

                try
                {
                    var buf = new byte[Math.Min(ava, Links.BufferLength)];
                    var iep = (EndPoint)new IPEndPoint(IPAddress.Any, IPEndPoint.MinPort);
                    var len = _broadcast.ReceiveFrom(buf, ref iep);

                    var rea = new Token(LinksHelper.Generator, new ReadOnlyMemory<byte>(buf, 0, len));
                    if (string.Equals(Links.Protocol, rea["protocol", nothrow: true]?.As<string>()) == false)
                        continue;
                    var res = LinksHelper.Generator.Encode(new
                    {
                        protocol = Links.Protocol,
                        port = _port,
                        name = _sname,
                        limit = _climit,
                        count = _clients.Count,
                    });
                    var sub = _broadcast.SendTo(res, iep);
                }
                catch (SocketException ex)
                {
                    Log.Error(ex);
                }
                catch (ObjectDisposedException)
                {
                    break;
                }
            }
        }
    }
}
