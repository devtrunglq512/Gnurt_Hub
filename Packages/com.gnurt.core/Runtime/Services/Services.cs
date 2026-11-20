using System;
using System.Collections.Generic;

namespace Gnurt
{
    /// <summary>
    /// Nơi đăng ký và lấy service dùng chung (pattern Service Locator). Gọi từ main thread.
    /// <code>
    /// Services.Register&lt;IAds&gt;(new MaxAds());
    /// Services.WhenReady&lt;IAds&gt;(ads =&gt; ads.ShowBanner());
    /// </code>
    /// </summary>
    public static class Services
    {
        sealed class Entry
        {
            public object Instance;
            public bool FromPlayMode;
        }

        static readonly GLogger Log = GLog.Tag("Services");
        static readonly Dictionary<Type, Entry> Entries = new Dictionary<Type, Entry>();
        static readonly List<Type> Order = new List<Type>();
        static readonly Dictionary<Type, List<Action<object>>> Waiting = new Dictionary<Type, List<Action<object>>>();

        static Services()
        {
            // Tắt domain reload: bỏ service đăng ký trong Play Mode, lần Play sau đăng ký lại từ đầu.
            PlayModeTracker.Exited += () =>
            {
                foreach (var type in Order.ToArray())
                    if (Entries[type].FromPlayMode) Unregister(type);
                Waiting.Clear();
            };
        }

        /// <summary>Đăng ký service theo kiểu T. Kiểu T đã có rồi thì giữ bản cũ và báo warning.</summary>
        public static T Register<T>(T instance) where T : class
        {
            if (instance == null) throw new ArgumentNullException(nameof(instance));

            var type = typeof(T);
            if (Entries.TryGetValue(type, out var existing))
            {
                Log.Warning($"{type.Name} đã được đăng ký, bỏ qua bản mới");
                return (T)existing.Instance;
            }

            Entries[type] = new Entry { Instance = instance, FromPlayMode = PlayModeTracker.IsPlaying };
            Order.Add(type);

            // Báo cho những ai đã chờ service này từ trước khi nó được đăng ký.
            if (Waiting.TryGetValue(type, out var waiters))
            {
                Waiting.Remove(type);
                foreach (var attach in waiters) attach(instance);
            }
            return instance;
        }

        /// <summary>Bỏ đăng ký service kiểu T.</summary>
        public static void Unregister<T>() where T : class => Unregister(typeof(T));

        /// <summary>Lấy service kiểu T, null nếu chưa đăng ký.</summary>
        public static T Get<T>() where T : class =>
            Entries.TryGetValue(typeof(T), out var entry) ? (T)entry.Instance : null;

        public static bool TryGet<T>(out T service) where T : class
        {
            service = Get<T>();
            return service != null;
        }

        /// <summary>
        /// Chờ service T sẵn sàng; chưa đăng ký thì chờ tới lúc được đăng ký.
        /// Service không phải GnurtService thì coi như sẵn sàng ngay khi đăng ký.
        /// </summary>
        public static void WhenReady<T>(Action<T> onReady, Action<string> onFailed = null) where T : class
        {
            void Attach(object instance)
            {
                if (instance is GnurtService service) service.WhenReady(() => onReady((T)instance), onFailed);
                else onReady((T)instance);
            }

            var type = typeof(T);
            if (Entries.TryGetValue(type, out var entry))
            {
                Attach(entry.Instance);
                return;
            }

            if (!Waiting.TryGetValue(type, out var list))
                Waiting[type] = list = new List<Action<object>>();
            list.Add(Attach);
        }

        /// <summary>Gọi Initialize() cho mọi GnurtService, theo thứ tự đăng ký.</summary>
        public static void InitializeAll()
        {
            foreach (var type in Order.ToArray())
                if (Entries[type].Instance is GnurtService service) service.Initialize();
        }

        /// <summary>Mọi service đã đăng ký, theo thứ tự đăng ký.</summary>
        public static IReadOnlyList<object> All => Order.ConvertAll(type => Entries[type].Instance);

        /// <summary>Bỏ hết. Chỉ dùng trong test.</summary>
        internal static void Clear()
        {
            Entries.Clear();
            Order.Clear();
            Waiting.Clear();
        }

        static void Unregister(Type type)
        {
            if (Entries.Remove(type)) Order.Remove(type);
        }
    }
}
