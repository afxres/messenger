using System;

namespace Messenger.Models
{
    /// <summary>
    /// 标注有此属性的函数将在收到消息时自动匹配路径执行
    /// </summary>
    [AttributeUsage(AttributeTargets.Method)]
    public class RouteAttribute : Attribute
    {
        private readonly string _pth = null;

        public string Path => _pth;

        public RouteAttribute(string path) => _pth = path;
    }
}
