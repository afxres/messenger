using Messenger.Modules;
using System;

namespace Messenger.Models
{
    /// <summary>
    /// 消息记录
    /// </summary>
    public class Packet
    {
        private readonly string _key;

        private readonly DateTime _timestamp;

        private readonly int _source;

        private readonly int _target;

        private readonly int _index;

        private readonly string _path;

        private readonly object _value;

        private string _image = null;

        private Profile _profile = null;

        public Packet(string key, DateTime datetime, int index, int source, int target, string path, object value)
        {
            _key = key ?? throw new ArgumentNullException(nameof(key));
            _timestamp = datetime;

            _path = path ?? throw new ArgumentNullException(nameof(path));
            _value = value;

            _source = source;
            _target = target;
            _index = index;
        }

        public Packet(int index, int source, int target, string path, object value)
        {
            _path = path ?? throw new ArgumentNullException(nameof(path));
            _value = value;

            _source = source;
            _target = target;
            _index = index;

            _key = Guid.NewGuid().ToString();
            _timestamp = DateTime.Now;
        }

        public string Key => _key;

        /// <summary>
        /// 分组索引
        /// </summary>
        public int Index => _index;

        /// <summary>
        /// 消息时间
        /// </summary>
        public DateTime DateTime => _timestamp;

        /// <summary>
        /// 收信人编号
        /// </summary>
        public int Target => _target;

        /// <summary>
        /// 发信人编号
        /// </summary>
        public int Source => _source;

        /// <summary>
        /// 消息类型
        /// </summary>
        public string Path => _path;

        /// <summary>
        /// 底层数据 (怎么解读取决于 <see cref="Path"/>)
        /// </summary>
        public object Object => _value;

        /// <summary>
        /// 发送者信息
        /// </summary>
        public Profile Profile
        {
            get
            {
                if (_profile == null)
                    _profile = ProfileModule.Query(_source, true);
                return _profile;
            }
        }

        /// <summary>
        /// 消息文本
        /// </summary>
        public string MessageText
        {
            get
            {
                if (_value is string str && _path == "text")
                    return str;
                return null;
            }
        }

        /// <summary>
        /// 图像路径
        /// </summary>
        public string MessageImage
        {
            get
            {
                if (_image == null && _value is string str && _path == "image")
                    _image = CacheModule.GetPath(str);
                return _image;
            }
        }

        /// <summary>
        /// 提醒
        /// </summary>
        public string MessageNotice
        {
            get
            {
                if (_value is string str && _path == "notice")
                    return str;
                return null;
            }
        }

        public override string ToString()
        {
            return $"{nameof(Packet)} at {_timestamp:u}, form {_source} to {_target}, path: {_path}, value: {_value}";
        }
    }
}
