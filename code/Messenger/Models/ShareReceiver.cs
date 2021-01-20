using Messenger.Extensions;
using Messenger.Modules;
using Mikodev.Binary;
using Mikodev.Logger;
using Mikodev.Network;
using System;
using System.IO;
using System.Net;
using System.Net.Sockets;
using System.Threading;
using System.Threading.Tasks;

namespace Messenger.Models
{
    public sealed class ShareReceiver : ShareBasic, IDisposable
    {
        private readonly object _locker = new object();

        private readonly CancellationTokenSource _cancel = new CancellationTokenSource();

        internal readonly Guid _key;

        internal readonly long _length;

        internal readonly bool _batch = false;

        /// <summary>
        /// 原始文件名
        /// </summary>
        internal readonly string _origin;

        internal bool _started = false;

        internal bool _disposed = false;

        internal long _position = 0;

        internal string _name = null;

        internal string _path = null;

        private readonly IPEndPoint[] _endpoints = null;

        public bool IsStarted => _started;

        public bool IsDisposed => _disposed;

        public override long Length => _length;

        public override bool IsBatch => _batch;

        public override string Name => _name;

        public override string Path => _path;

        public override long Position => _position;

        public ShareReceiver(int id, Token reader) : base(id)
        {
            if (reader == null)
                throw new ArgumentNullException(nameof(reader));

            var typ = reader["type"].As<string>();
            if (typ == "file")
                _length = reader["length"].As<long>();
            else if (typ == "dir")
                _batch = true;
            else
                throw new ApplicationException("Invalid share type!");

            _key = reader["key"].As<Guid>();
            _origin = reader["name"].As<string>();
            _name = _origin;
            _endpoints = reader["endpoints"].As<IPEndPoint[]>();
        }

        public Task Start()
        {
            lock (_locker)
            {
                if (_started || _disposed)
                    throw new InvalidOperationException();
                _started = true;
            }

            Status = ShareStatus.连接;
            Register();
            OnPropertyChanged(nameof(IsStarted));
            return Task.Run(_Start);
        }

        private async Task _Start()
        {
            var soc = default(Socket);
            var iep = default(IPEndPoint);

            for (var i = 0; i < _endpoints.Length; i++)
            {
                if (soc != null)
                    break;
                soc = new Socket(SocketType.Stream, ProtocolType.Tcp);
                iep = _endpoints[i];

                try
                {
                    await soc.ConnectAsyncEx(iep).TimeoutAfter("Share receiver timeout.");
                }
                catch (Exception err)
                {
                    Log.Error(err);
                    soc.Dispose();
                    soc = null;
                }
            }

            if (soc == null)
            {
                Status = ShareStatus.失败;
                Dispose();
                return;
            }

            var buf = LinksHelper.Generator.Encode(new
            {
                path = "share." + (_batch ? "directory" : "file"),
                data = _key,
                source = LinkModule.Id,
                target = Id,
            });

            try
            {
                _ = soc.SetKeepAlive();
                await soc.SendAsyncExt(buf);
                Status = ShareStatus.运行;
                await _Receive(soc, _cancel.Token);
                Status = ShareStatus.成功;
                PostModule.Notice(Id, _batch ? "share.dir" : "share.file", _origin);
            }
            catch (OperationCanceledException)
            {
                Status = ShareStatus.取消;
            }
            catch (Exception ex)
            {
                Log.Error(ex);
                Status = ShareStatus.中断;
            }
            finally
            {
                soc.Dispose();
                Dispose();
            }
        }

        internal Task _Receive(Socket socket, CancellationToken token)
        {
            void _UpdateInfo(FileSystemInfo info)
            {
                _name = info.Name;
                _path = info.FullName;
                OnPropertyChanged(nameof(Name));
                OnPropertyChanged(nameof(Path));
            }

            if (_batch)
            {
                var dir = ShareModule.AvailableDirectory(_name);
                _UpdateInfo(dir);
                return socket.ReceiveDirectoryAsyncEx(dir.FullName, r => _position += r, token);
            }
            else
            {
                var inf = ShareModule.AvailableFile(_name);
                _UpdateInfo(inf);
                return socket.ReceiveFileEx(inf.FullName, _length, r => _position += r, token);
            }
        }

        public void Dispose()
        {
            lock (_locker)
            {
                if (_disposed)
                    return;
                _disposed = true;
            }

            _cancel.Cancel();
            _cancel.Dispose();
            OnPropertyChanged(nameof(IsDisposed));
        }
    }
}
