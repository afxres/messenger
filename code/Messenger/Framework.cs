using Messenger.Extensions;
using Messenger.Models;
using Mikodev.Logger;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace Messenger
{
    public sealed class Framework : IDisposable
    {
        private Framework() { }

        private static readonly Framework s_ins = new Framework();

        private readonly object _locker = new object();

        private readonly CancellationTokenSource _cancel = new CancellationTokenSource();

        private bool _started = false;

        private bool _closed = false;

        private List<Action> _exit;

        private void _Start()
        {
            // 利用反射运行模块
            var lst = Extension.FindAttribute(
                typeof(LoaderAttribute).Assembly,
                typeof(LoaderAttribute), null,
                (a, m, t) => new { attribute = (LoaderAttribute)a, method = m, path = $"{t.FullName}.{m.Name}" }
            ).ToList();

            var loa = from r in lst
                      where r.attribute.Flag == LoaderFlags.OnLoad
                      orderby r.attribute.Level
                      select (Action)Delegate.CreateDelegate(typeof(Action), r.method);

            var ext = from r in lst
                      where r.attribute.Flag == LoaderFlags.OnExit
                      orderby r.attribute.Level
                      select (Action)Delegate.CreateDelegate(typeof(Action), r.method);

            var bak = from r in lst
                      where r.attribute.Flag == LoaderFlags.AsTask
                      orderby r.attribute.Level
                      select new { r.path, func = (Func<CancellationToken, Task>)Delegate.CreateDelegate(typeof(Func<CancellationToken, Task>), r.method) };

            var run = loa.ToList();
            var sav = ext.ToList();
            var tsk = bak.ToList();

            lock (_locker)
            {
                if (_started || _closed)
                    throw new InvalidOperationException("Framework started or closed!");
                _started = true;
                _exit = sav;
            }

            run.ForEach(r => r.Invoke());
            tsk.ForEach(r => Task.Run(() => r.func.Invoke(_cancel.Token)).ContinueWith(t => Log.Info($"Framework task completed, status: {t.Status}, path: {r.path}")));
        }

        private void _Close()
        {
            lock (_locker)
            {
                if (_closed || _started == false)
                    return;
                _closed = true;
            }

            _cancel.Cancel();
            _cancel.Dispose();
            _exit.ForEach(r => r.Invoke());
        }

        public static void Start() => s_ins._Start();

        public static void Close() => s_ins._Close();

        public void Dispose()
        {
            _cancel.Dispose();
        }
    }
}
