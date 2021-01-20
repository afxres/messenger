using Messenger.Extensions;
using Messenger.Models;
using Mikodev.Logger;
using Mikodev.Network;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Windows;

namespace Messenger.Modules
{
    /// <summary>
    /// 管理用户信息
    /// </summary>
    internal class ProfileModule : INotifyPropertyChanging, INotifyPropertyChanged
    {
        private const string _KeyId = "profile-id";

        private const string _KeyName = "profile-name";

        private const string _KeyText = "profile-text";

        private const string _KeyImage = "profile-image";

        private const string _KeyLabel = "profile-group-labels";

        private readonly Profile _local = new Profile(Links.DefaultId);

        private int _id;

        private bool _hasgroup = false;

        private bool _hasclient = false;

        private bool _hasrecent = false;

        private string _grouptags = null;

        private byte[] _imagebuffer = null;

        private List<int> _groupids = null;

        private readonly BindingList<Profile> _group = new BindingList<Profile>();

        private readonly BindingList<Profile> _recent = new BindingList<Profile>();

        private readonly BindingList<Profile> _client = new BindingList<Profile>();

        private readonly LinkedList<WeakReference> _spaces = new LinkedList<WeakReference>();

        private Profile _inscope = null;

        private EventHandler _inscopechanged = null;

        public bool HasGroup
        {
            get => _hasgroup;
            private set => _EmitChange(ref _hasgroup, value);
        }

        public bool HasRecent
        {
            get => _hasrecent;
            private set => _EmitChange(ref _hasrecent, value);
        }

        public bool HasClient
        {
            get => _hasclient;
            private set => _EmitChange(ref _hasclient, value);
        }

        public event PropertyChangedEventHandler PropertyChanged;

        public event PropertyChangingEventHandler PropertyChanging;

        private void _EmitChange<T>(ref T source, T target, [CallerMemberName] string name = null)
        {
            PropertyChanging?.Invoke(this, new PropertyChangingEventArgs(name));
            if (Equals(source, target))
                return;
            source = target;
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(name));
        }

        private ProfileModule()
        {
            Profile.InstancePropertyChanged += (s, e) =>
            {
                if (e.PropertyName.Equals(nameof(Profile.Hint)))
                    _Changed();
            };
            _client.ListChanged += (s, e) => _Changed();
            _group.ListChanged += (s, e) => _Changed();
            _recent.ListChanged += (s, e) => _Changed();
        }

        /// <summary>
        /// 重新计算未读消息数量
        /// </summary>
        private void _Changed()
        {
            var cli = _client.Sum(r => r.Hint);
            var gro = _group.Sum(r => r.Hint);
            var rec = _recent.Sum(r => (r.Hint < 1 || _client.FirstOrDefault(t => t.Id == r.Id) != null || _group.FirstOrDefault(t => t.Id == r.Id) != null) ? 0 : r.Hint);
            HasClient = cli > 0;
            HasGroup = gro > 0;
            HasRecent = rec > 0;
        }

        // ---------- ---------- ---------- ---------- ---------- ---------- ---------- ---------- ---------- ----------

        private static readonly ProfileModule s_ins = new ProfileModule();

        public static int Id => s_ins._id;
        public static ProfileModule Instance => s_ins;
        public static Profile Current => s_ins._local;
        public static Profile Inscope => s_ins._inscope;
        public static string GroupLabels => s_ins._grouptags;
        public static byte[] ImageBuffer => s_ins._imagebuffer;
        public static List<int> GroupIds => s_ins._groupids;
        public static BindingList<Profile> GroupList => s_ins._group;
        public static BindingList<Profile> RecentList => s_ins._recent;
        public static BindingList<Profile> ClientList => s_ins._client;

        public static event EventHandler InscopeChanged { add => s_ins._inscopechanged += value; remove => s_ins._inscopechanged -= value; }

        public static void Clear()
        {
            var clt = s_ins._client;
            Application.Current.Dispatcher.Invoke(() => clt.Clear());
        }

        /// <summary>
        /// 添加或更新用户信息 (添加返回真, 更新返回假)
        /// </summary>
        public static void Insert(Profile profile)
        {
            var clt = s_ins._client;
            Application.Current.Dispatcher.Invoke(() =>
            {
                var res = Query(profile.Id, true);
                _ = res.CopyFrom(profile);
                var tmp = clt.FirstOrDefault(r => r.Id == profile.Id);
                if (tmp == null)
                    clt.Add(res);
            });
        }

        /// <summary>
        /// 根据编号查找用户信息
        /// </summary>
        /// <param name="id">编号</param>
        /// <param name="create">指定编号不存在时创建对象</param>
        public static Profile Query(int id, bool create = false)
        {
            var ins = s_ins;
            if (id == ins._id || id == Links.DefaultId)
                return ins._local;
            var spa = ins._spaces;

            var pro = default(Profile);
            var ele = spa.First;
            while (ele != null)
            {
                var cur = ele;
                ele = ele.Next;
                if (!(cur.Value.Target is Profile val))
                    spa.Remove(cur);
                else if (pro == null && val.Id == id)
                    pro = val;
            }

            if (pro != null)
                return pro;
            pro = ins._client.Concat(ins._group).Concat(ins._recent).FirstOrDefault(t => t.Id == id);
            if (pro != null)
                return pro;
            if (create == false)
                return null;
            pro = new Profile(id) { Name = $"佚名 [{id}]" };
            _ = spa.AddLast(new WeakReference(pro));
            return pro;
        }

        /// <summary>
        /// 移除所有 Id 不在给定集合的项目 并把含有未读消息的项目添加到最近列表
        /// </summary>
        /// <param name="ids">Id 集合</param>
        public static List<Profile> Remove(IEnumerable<int> ids)
        {
            var clt = s_ins._client;
            var lst = default(List<Profile>);
            Application.Current.Dispatcher.Invoke(() =>
            {
                lst = clt.RemoveEx(r => ids.Contains(r.Id) == false);
                foreach (var i in lst)
                    if (i.Hint > 0)
                        SetRecent(i);
            });
            return lst;
        }

        /// <summary>
        /// 设置组标签 不区分大小写 以空格分开 超出个数限制返回 false
        /// </summary>
        public static bool SetGroupLabels(string args)
        {
            var kvp = from k in
                          from i in (args ?? string.Empty).Split(new[] { ' ' }, StringSplitOptions.RemoveEmptyEntries)
                          select new { Name = i, Hash = Extension.GetInvariantHashCode(i.ToLower()) | 1 << 31 }
                      group k by k.Hash into a
                      select a.First();
            var kvs = kvp.ToList();

            if (kvs.Count > Links.GroupLabelLimit)
                return false;

            var gro = s_ins._group;
            var ids = (from i in kvs select i.Hash).ToList();
            s_ins._groupids = ids;
            s_ins._grouptags = args;

            Application.Current.Dispatcher.Invoke(() =>
            {
                var lst = gro.RemoveEx(r => ids.Contains(r.Id) == false);
                foreach (var i in lst)
                    if (i.Hint > 0)
                        SetRecent(i);
                var add = from r in kvs
                          where gro.FirstOrDefault(t => t.Id == r.Hash) == null
                          select r;
                foreach (var i in add)
                {
                    var pro = Query(i.Hash, true);
                    pro.Name = i.Name;
                    pro.Text = i.Hash.ToString("X8");
                    gro.Add(pro);
                }
            });

            PostModule.UserGroups();
            EnvironmentModule.Update(_KeyLabel, args);
            return true;
        }

        /// <summary>
        /// 更新头像 (在主线程上操作, 需要捕捉异常)
        /// </summary>
        public static void SetImage(string path)
        {
            var buf = CacheModule.ImageSquare(path);
            var str = CacheModule.SetBuffer(buf, true);
            s_ins._imagebuffer = buf;
            s_ins._local.Image = str;

            PostModule.UserProfile(Links.Id);
            EnvironmentModule.Update(_KeyImage, path);
        }

        public static void SetProfile(string name, string text)
        {
            var pro = s_ins._local;
            pro.Name = name;
            pro.Text = text;

            EnvironmentModule.Update(_KeyName, name);
            EnvironmentModule.Update(_KeyText, text);
            PostModule.UserProfile(Links.Id);
        }

        public static void SetId(int id)
        {
            s_ins._id = id;
            EnvironmentModule.Update(_KeyId, id.ToString());
        }

        /// <summary>
        /// 设置当前联系人
        /// </summary>
        public static void SetInscope(Profile profile)
        {
            if (profile == null)
            {
                s_ins._inscope = null;
                return;
            }

            profile.Hint = 0;
            if (ReferenceEquals(profile, s_ins._inscope))
                return;

            s_ins._inscope = profile;
            s_ins._inscopechanged?.Invoke(s_ins, new EventArgs());
        }

        public static void Shutdown()
        {
            s_ins._inscope = null;
        }

        /// <summary>
        /// 添加联系人到最近列表
        /// </summary>
        public static void SetRecent(Profile profile)
        {
            var rec = s_ins._recent;
            for (var i = 0; i < rec.Count; i++)
            {
                if (rec[i].Id == profile.Id)
                {
                    if (ReferenceEquals(rec[i], profile))
                        return;
                    // 移除值相同但引用不同的项目
                    rec.RemoveAt(i);
                    break;
                }
            }
            rec.Add(profile);
        }

        [Loader(16, LoaderFlags.OnLoad)]
        public static void Load()
        {
            try
            {
                var pro = s_ins._local;
                s_ins._id = int.Parse(EnvironmentModule.Query(_KeyId, new Random().Next(1, 1 << 16 + 1).ToString()));
                pro.Name = EnvironmentModule.Query(_KeyName);
                pro.Text = EnvironmentModule.Query(_KeyText);

                var lbs = EnvironmentModule.Query(_KeyLabel);
                _ = SetGroupLabels(lbs);
                var pth = EnvironmentModule.Query(_KeyImage);
                if (pth == null)
                    return;
                var buf = CacheModule.ImageSquare(pth);
                var sha = CacheModule.SetBuffer(buf, false);
                s_ins._local.Image = CacheModule.GetPath(sha);
                s_ins._imagebuffer = buf;
            }
            catch (Exception ex)
            {
                Log.Error(ex);
                return;
            }
        }
    }
}
