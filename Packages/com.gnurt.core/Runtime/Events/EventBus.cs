using System;
using System.Collections.Generic;

namespace Gnurt
{
    /// <summary>
    /// Kênh sự kiện toàn cục (pattern Observer): module bắn sự kiện mà không cần biết ai nghe.
    /// Sự kiện nên là <c>readonly struct</c> để không sinh rác.
    /// <code>
    /// using var sub = EventBus.Subscribe&lt;LevelStarted&gt;(e =&gt; ...);
    /// EventBus.Publish(new LevelStarted(3));
    /// </code>
    /// </summary>
    public static class EventBus
    {
        static readonly GLogger Log = GLog.Tag("EventBus");

        // Hàm dọn của từng kênh. Tham số: true = chỉ dọn subscriber tạo trong Play Mode.
        static readonly List<Action<bool>> Cleaners = new List<Action<bool>>();

        static EventBus()
        {
            PlayModeTracker.Exited += () => ForEachCleaner(true);
        }

        /// <summary>Nghe sự kiện T. Gọi Dispose() trên kết quả để thôi nghe.</summary>
        public static IDisposable Subscribe<T>(Action<T> handler) where T : struct
        {
            if (handler == null) throw new ArgumentNullException(nameof(handler));
            return Channel<T>.Add(handler);
        }

        /// <summary>Bắn sự kiện: gọi các handler ngay trên thread hiện tại. Nên gọi từ main thread.</summary>
        public static void Publish<T>(T evt) where T : struct => Channel<T>.Publish(evt);

        /// <summary>Bắn sự kiện trên main thread. Dùng khi đang ở thread khác, ví dụ trong callback của SDK.</summary>
        public static void PublishOnMainThread<T>(T evt) where T : struct => MainThread.Run(() => Channel<T>.Publish(evt));

        /// <summary>Số handler đang nghe T.</summary>
        public static int CountSubscribers<T>() where T : struct => Channel<T>.Count;

        /// <summary>Bỏ mọi subscriber. Chỉ dùng trong test.</summary>
        internal static void ClearAll() => ForEachCleaner(false);

        static void ForEachCleaner(bool playModeOnly)
        {
            Action<bool>[] cleaners;
            lock (Cleaners) cleaners = Cleaners.ToArray();
            foreach (var clean in cleaners) clean(playModeOnly);
        }

        sealed class Subscription<T> : IDisposable where T : struct
        {
            public readonly Action<T> Handler;
            public readonly bool FromPlayMode;
            public volatile bool Disposed;

            public Subscription(Action<T> handler)
            {
                Handler = handler;
                FromPlayMode = PlayModeTracker.IsPlaying;
            }

            public void Dispose()
            {
                if (Disposed) return;
                Disposed = true;
                Channel<T>.Remove(this);
            }
        }

        // Mỗi loại sự kiện một kênh riêng (generic static), tra cứu nhanh, không cần Dictionary.
        static class Channel<T> where T : struct
        {
            static readonly object Lock = new object();

            // Thêm/bớt thì tạo mảng mới, nên vòng Publish đang chạy không bị ảnh hưởng.
            static Subscription<T>[] _subs = Array.Empty<Subscription<T>>();

            static Channel()
            {
                lock (Cleaners) Cleaners.Add(Clear);
            }

            public static int Count => _subs.Length;

            public static Subscription<T> Add(Action<T> handler)
            {
                var sub = new Subscription<T>(handler);
                lock (Lock)
                {
                    var next = new Subscription<T>[_subs.Length + 1];
                    _subs.CopyTo(next, 0);
                    next[_subs.Length] = sub;
                    _subs = next;
                }
                return sub;
            }

            public static void Remove(Subscription<T> sub)
            {
                lock (Lock) _subs = Array.FindAll(_subs, s => s != sub);
            }

            public static void Publish(T evt)
            {
                var subs = _subs;
                for (int i = 0; i < subs.Length; i++)
                {
                    var sub = subs[i];
                    if (sub.Disposed) continue; // bị huỷ giữa vòng này thì bỏ qua

                    try
                    {
                        sub.Handler(evt);
                    }
                    catch (Exception e)
                    {
                        // Một handler lỗi không được làm hỏng các handler còn lại.
                        Log.Error($"Handler lỗi khi xử lý {typeof(T).Name}", e);
                    }
                }
            }

            static void Clear(bool playModeOnly)
            {
                lock (Lock)
                {
                    foreach (var sub in _subs)
                        if (!playModeOnly || sub.FromPlayMode) sub.Disposed = true;
                    _subs = Array.FindAll(_subs, s => !s.Disposed);
                }
            }
        }
    }
}
