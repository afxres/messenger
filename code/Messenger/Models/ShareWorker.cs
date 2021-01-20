using Messenger.Extensions;
using Mikodev.Logger;
using System;
using System.IO;
using System.Net.Sockets;
using System.Threading;
using System.Threading.Tasks;

namespace Messenger.Models
{
    public sealed class ShareWorker : ShareBasic, IDisposable
    {
        internal readonly object _locker = new object();

        internal readonly Share _source;

        internal readonly Socket _socket;

        internal readonly CancellationTokenSource _cancel = new CancellationTokenSource();

        internal long _position = 0;

        internal bool _started = false;

        internal bool _disposed = false;

        public override long Length => _source.Length;

        public override bool IsBatch => _source.IsBatch;

        public override string Name => _source._name;

        public override string Path => _source._path;

        public override long Position => _position;

        public ShareWorker(Share share, int id, Socket socket) : base(id)
        {
            _source = share;
            _socket = socket;
        }

        public async Task Start()
        {
            lock (_locker)
            {
                if (_started || _disposed)
                    throw new InvalidOperationException();
                _started = true;
            }

            Status = ShareStatus.运行;
            Register();

            try
            {
                if (_source._info is FileInfo inf)
                    await _socket.SendFileEx(_source._path, _source._length, r => _position += r, _cancel.Token);
                else
                    await _socket.SendDirectoryAsyncEx(_source._path, r => _position += r, _cancel.Token);
                Status = ShareStatus.成功;
            }
            catch (OperationCanceledException)
            {
                Status = ShareStatus.取消;
            }
            catch (Exception ex)
            {
                Status = ShareStatus.中断;
                Log.Error(ex);
            }
            finally
            {
                Dispose();
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
        }
    }
}
