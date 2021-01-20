using Messenger.Models;
using Mikodev.Logger;
using Mikodev.Network;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.Xml;
using static Messenger.Extensions.Extension;

namespace Messenger.Modules
{
    /// <summary>
    /// 管理用户设置
    /// </summary>
    internal class EnvironmentModule
    {
        private const int _NoticeDelay = 1000;

        private const int _NoticeErrorDelay = 15 * 1000 - _NoticeDelay;

        private static readonly TimeSpan _NoticeInterval = TimeSpan.FromSeconds(10);

        private const string _Path = nameof(Messenger) + ".settings.xml";

        private const string _Root = "settings";

        private const string _Header = "setting";

        private const string _Key = "key";

        private const string _Value = "value";

        private readonly object _locker = new object();

        private readonly Dictionary<string, string> _settings = new Dictionary<string, string>();

        private readonly LinkNoticeSource _source = new LinkNoticeSource(_NoticeInterval);

        private static readonly EnvironmentModule s_ins = new EnvironmentModule();

        private EnvironmentModule() { }

        private void _Load(XmlDocument document)
        {
            var itm = document.SelectNodes($"/{_Root}/{_Header}[@{_Key}]");
            foreach (var i in itm)
            {
                var ele = (XmlElement)i;
                var key = (XmlAttribute)ele.SelectSingleNode($"@{_Key}");
                // Maybe null
                var val = (XmlAttribute)ele.SelectSingleNode($"@{_Value}");
                _Update(key.Value, val?.Value);
            }
        }

        [Loader(0, LoaderFlags.OnLoad)]
        public static void Load()
        {
            var fst = default(FileStream);
            var doc = default(XmlDocument);

            try
            {
                var inf = new FileInfo(_Path);
                if (inf.Exists == false)
                    return;
                fst = new FileStream(_Path, FileMode.Open, FileAccess.Read, FileShare.ReadWrite);
                if (fst.Length > Links.BufferLengthLimit)
                    return;
                doc = new XmlDocument();
                doc.Load(fst);
            }
            catch (Exception ex)
            {
                Log.Error(ex);
            }
            finally
            {
                fst?.Dispose();
            }

            // Do not call this method if xml file not disposed!!!
            s_ins._Load(doc);
        }

        /// <summary>
        /// 保存配置并忽略异常
        /// </summary>
        private bool _Save(string path)
        {
            var lst = Lock(_locker, () => _settings.ToList());
            var doc = new XmlDocument();
            var top = doc.CreateElement(_Root);
            _ = doc.AppendChild(top);
            lst.Sort((a, b) => a.Key.CompareTo(b.Key));
            foreach (var i in lst)
            {
                var key = i.Key;
                var val = i.Value;
                var ele = doc.CreateElement(_Header);
                ele.SetAttribute(_Key, key);
                if (val != null)
                    ele.SetAttribute(_Value, val);
                _ = top.AppendChild(ele);
            }

            var fst = default(FileStream);
            var wtr = default(StreamWriter);
            var res = false;

            try
            {
                fst = new FileStream(path, FileMode.Create);
                wtr = new StreamWriter(fst, Encoding.UTF8);
                doc.Save(wtr);
                res = true;
            }
            catch (Exception ex)
            {
                Log.Error(ex);
            }
            finally
            {
                wtr?.Dispose();
                fst?.Dispose();
            }
            return res;
        }

        private void _Exit()
        {
            var res = _source.GetNotice(true);
            if (res.IsAny == false)
                return;
            _ = _Save(_Path);
            res.Handled();
        }

        private string _Query(string key, string value)
        {
            lock (_locker)
            {
                if (_settings.TryGetValue(key, out var val))
                    return val;
                _settings.Add(key, value);
            }
            _source.Update();
            return value;
        }

        private void _Update(string key, string value)
        {
            lock (_locker)
            {
                if (_settings.TryGetValue(key, out var val) && Equals(val, value))
                    return;
                _settings[key] = value;
            }
            _source.Update();
        }

        public static string Query(string key, string defaultValue = null) => s_ins._Query(key, defaultValue);

        public static void Update(string key, string value) => s_ins._Update(key, value);

        [Loader(int.MaxValue, LoaderFlags.OnExit)]
        public static void Exit() => s_ins._Exit();

        [Loader(0, LoaderFlags.AsTask)]
        public static async Task Scan(CancellationToken token)
        {
            while (true)
            {
                token.ThrowIfCancellationRequested();
                await Task.Delay(_NoticeDelay);
                token.ThrowIfCancellationRequested();

                var res = s_ins._source.Notice();
                if (res.IsAny == false)
                    continue;

                if (s_ins._Save(_Path) == false)
                    await Task.Delay(_NoticeErrorDelay);
                else
                    res.Handled();
                continue;
            }
        }
    }
}
