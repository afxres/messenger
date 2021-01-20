using System;

namespace Messenger.Models
{
    /// <summary>
    /// 标注有此属性的静态函数将根据指定条件自动执行
    /// </summary>
    [AttributeUsage(AttributeTargets.Method)]
    public class LoaderAttribute : Attribute
    {
        private readonly int _lev;

        private readonly LoaderFlags _tag;

        public int Level => _lev;

        public LoaderFlags Flag => _tag;

        public LoaderAttribute(int level, LoaderFlags flag)
        {
            _lev = level;
            _tag = flag;
        }
    }
}
