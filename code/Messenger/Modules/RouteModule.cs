using Messenger.Extensions;
using Messenger.Models;
using Mikodev.Logger;
using Mikodev.Network;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;

namespace Messenger.Modules
{
    /// <summary>
    /// 处理消息, 并分发给各个消息处理函数
    /// </summary>
    internal class RouteModule
    {
        private static readonly RouteModule s_ins = new RouteModule();

        private readonly Dictionary<string, Action<byte[]>> _dic = new Dictionary<string, Action<byte[]>>();

        private RouteModule() { }

        private void _Load()
        {
            /* 利用反射识别所有控制器
             * 同时构建表达式以便提升运行速度 */
            var fun = typeof(LinkExtension).GetMethods().First(r => r.Name == nameof(LinkExtension.LoadValue));
            var lst = Extension.FindAttribute(
                typeof(RouteAttribute).Assembly, typeof(RouteAttribute),
                typeof(LinkPacket),
                (a, m, t) => new { Attribute = (RouteAttribute)a, MethodInfo = m, Type = t }
            ).ToList();

            var res = lst.Select(i =>
            {
                var buf = Expression.Parameter(typeof(byte[]), "buffer");
                var val = Expression.Call(fun, Expression.New(i.Type), buf);
                var cvt = Expression.Convert(val, i.Type);
                var act = Expression.Lambda<Action<byte[]>>(Expression.Call(cvt, i.MethodInfo), buf);
                return new { Path = i.Attribute.Path, Action = act.Compile() };
            });

            foreach (var i in res)
                _dic.Add(i.Path, i.Action);
            return;
        }

        public static void Invoke(LinkPacket arg)
        {
            var dic = s_ins._dic;
            if (dic.TryGetValue(arg.Path, out var act))
                act.Invoke(arg.Buffer);
            else
                Log.Info($"Path \"{arg.Path}\" not supported.");
            return;
        }

        [Loader(1, LoaderFlags.OnLoad)]
        public static void Load()
        {
            s_ins._Load();
        }
    }
}
