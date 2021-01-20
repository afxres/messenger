using Mikodev.Binary;
using Mikodev.Logger;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Net.Sockets;
using System.Security.Cryptography;
using System.Threading;
using System.Threading.Tasks;

namespace Mikodev.Network
{
    public sealed class LinkClient : IDisposable
    {
        internal readonly int _id = 0;

        internal readonly object _locker = new object();

        internal readonly Socket _socket = null;

        internal readonly Socket _listen = null;

        internal readonly IPEndPoint _innerEndpoint = null;

        internal readonly IPEndPoint _outerEndpoint = null;

        internal readonly IPEndPoint _connected = null;

        internal readonly Queue<byte[]> _messageQueue = new Queue<byte[]>();

        internal readonly CancellationTokenSource _cancellationSource = new CancellationTokenSource();

        internal readonly Func<Socket, LinkPacket, Task> _requested;

        internal readonly AesManaged _aes = new AesManaged();

        internal bool _started = false;

        internal bool _disposed = false;

        internal long _messageLength = 0;

        public int Id => _id;

        public bool IsRunning { get { lock (_locker) { return _started == true && _disposed == false; } } }

        /// <summary>
        /// 本机端点 (不会返回 null)
        /// </summary>
        public IPEndPoint InnerEndPoint => _innerEndpoint;

        /// <summary>
        /// 服务器报告的相对于服务器的外部端点 (不会返回 null)
        /// </summary>
        public IPEndPoint OuterEndPoint => _outerEndpoint;

        public event EventHandler<LinkEventArgs<LinkPacket>> Received = null;

        public event EventHandler<LinkEventArgs<Exception>> Disposed = null;

        internal LinkClient(int id, Socket socket, IPEndPoint inner, IPEndPoint outer, byte[] key, byte[] block)
        {
            _id = id;
            _socket = socket;

            _innerEndpoint = inner;
            _outerEndpoint = outer;

            _aes.Key = key;
            _aes.IV = block;
        }

        internal LinkClient(int id, Socket socket, Socket listen, IPEndPoint connected, IPEndPoint inner, IPEndPoint outer, byte[] key, byte[] block, Func<Socket, LinkPacket, Task> request)
        {
            _id = id;
            _socket = socket;
            _listen = listen;

            _connected = connected;
            _innerEndpoint = inner;
            _outerEndpoint = outer;

            _aes.Key = key;
            _aes.IV = block;
            _requested = request;
        }

        public static async Task<LinkClient> Connect(int id, IPEndPoint target, Func<Socket, LinkPacket, Task> request)
        {
            if (target == null)
                throw new ArgumentNullException(nameof(target));
            if (request == null)
                throw new ArgumentNullException(nameof(request));

            var socket = new Socket(SocketType.Stream, ProtocolType.Tcp);
            var listen = new Socket(SocketType.Stream, ProtocolType.Tcp);
            var rsa = RSA.Create();
            var parameters = rsa.ExportParameters(false);

            try
            {
                await socket.ConnectAsyncEx(target).TimeoutAfter("Timeout, at connect to server.");
                _ = socket.SetKeepAlive();
                var inner = (IPEndPoint)socket.LocalEndPoint;

                listen.SetSocketOption(SocketOptionLevel.Socket, SocketOptionName.ReuseAddress, true);
                listen.Bind(inner);
                listen.Listen(Links.ClientSocketLimit);

                var packet = LinksHelper.Generator.Encode(new
                {
                    source = id,
                    endpoint = inner,
                    path = "link.connect",
                    protocol = Links.Protocol,
                    rsa = new
                    {
                        modulus = parameters.Modulus,
                        exponent = parameters.Exponent,
                    }
                });

                await socket.SendAsyncExt(packet).TimeoutAfter("Timeout, at client request.");
                var result = await socket.ReceiveAsyncExt().TimeoutAfter("Timeout, at client response.");

                var rea = new Token(LinksHelper.Generator, result);
                rea["result"].As<LinkError>().AssertError();
                var outer = rea["endpoint"].As<IPEndPoint>();
                var key = rsa.Decrypt(rea["aes"]["key"].As<byte[]>(), RSAEncryptionPadding.OaepSHA1);
                var any = rsa.Decrypt(rea["aes"]["iv"].As<byte[]>(), RSAEncryptionPadding.OaepSHA1);
                return new LinkClient(id, socket, listen, target, inner, outer, key, any, request);
            }
            catch (Exception)
            {
                socket.Dispose();
                listen.Dispose();
                throw;
            }
        }

        public async Task Start()
        {
            var tasks = default(Task[]);
            var error = default(Exception);

            lock (_locker)
            {
                if (_started || _disposed)
                    throw new InvalidOperationException("Client has benn marked as started or disposed!");
                _started = true;

                var value = new[]
                {
                    _listen == null ? null : Task.Run(_Listener),
                    Task.Run(_Sender),
                    Task.Run(_Receiver),
                };
                tasks = value.Where(r => r != null).ToArray();
            }

            try
            {
                await await Task.WhenAny(tasks);
            }
            catch (Exception ex)
            {
                if ((ex is SocketException || ex is ObjectDisposedException) == false)
                    Log.Error(ex);
                error = ex;
            }

            _Dispose(error);
            await Task.WhenAll(tasks);
        }

        public void Enqueue(byte[] buffer)
        {
            var len = buffer?.Length ?? throw new ArgumentNullException(nameof(buffer));
            if (len < 1 || len > Links.BufferLengthLimit)
                throw new ArgumentOutOfRangeException(nameof(buffer));
            lock (_locker)
            {
                if (_disposed)
                    return;
                _messageLength += len;
                _messageQueue.Enqueue(buffer);
            }
        }

        internal async Task _Sender()
        {
            bool _Dequeue(out byte[] buf)
            {
                lock (_locker)
                {
                    if (_messageLength > Links.BufferQueueLimit)
                        throw new LinkException(LinkError.QueueLimited);
                    if (_messageLength > 0)
                    {
                        buf = _messageQueue.Dequeue();
                        _messageLength -= buf.Length;
                        return true;
                    }
                }

                buf = null;
                return false;
            }

            while (_cancellationSource.IsCancellationRequested == false)
            {
                if (_Dequeue(out var buf))
                    await _socket.SendAsyncExt(_aes.Encrypt(buf));
                else
                    await Task.Delay(Links.Delay);
                continue;
            }
        }

        internal async Task _Receiver()
        {
            while (_cancellationSource.IsCancellationRequested == false)
            {
                var buf = await _socket.ReceiveAsyncExt();

                var rec = Received;
                if (rec == null)
                    continue;
                var res = _aes.Decrypt(buf);
                var pkt = new LinkPacket().LoadValue(res);
                var arg = new LinkEventArgs<LinkPacket>(pkt);
                rec.Invoke(this, arg);
            }
        }

        internal async Task _Listener()
        {
            while (_cancellationSource.IsCancellationRequested == false)
            {
                var soc = await _listen.AcceptAsyncEx();

                Task.Run(async () =>
                {
                    try
                    {
                        _ = soc.SetKeepAlive();
                        var buf = await soc.ReceiveAsyncExt().TimeoutAfter("Timeout, at receive header packet.");
                        var pkt = new LinkPacket().LoadValue(buf);
                        await _requested.Invoke(soc, pkt);
                    }
                    catch (Exception ex)
                    {
                        Log.Error(ex);
                    }
                    finally
                    {
                        soc.Dispose();
                    }
                }).Ignore();
            }
        }

        internal void _Dispose(Exception err = null)
        {
            lock (_locker)
            {
                if (_disposed)
                    return;
                _disposed = true;
                _messageQueue.Clear();
                _messageLength = 0;
            }
            _cancellationSource.Cancel();
            _cancellationSource.Dispose();
            _socket.Dispose();
            _listen?.Dispose();
            _aes.Dispose();

            var dis = Disposed;
            if (dis == null)
                return;
            _ = Task.Run(() =>
            {
                if (err == null)
                    err = new OperationCanceledException("Client disposed.");
                var arg = new LinkEventArgs<Exception>(err);
                dis.Invoke(this, arg);
            });
        }

        public void Dispose() => _Dispose();
    }
}
