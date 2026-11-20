using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace Gnurt
{
    public enum ServiceState
    {
        Idle,
        Initializing,
        Ready,
        Failed,
    }

    /// <summary>
    /// Service có vòng đời: Idle → Initializing → Ready hoặc Failed.
    /// Lớp con viết logic trong <see cref="OnInitialize"/>, xong thì gọi <see cref="Complete"/> hoặc <see cref="Fail"/>.
    /// </summary>
    public abstract class GnurtService
    {
        static readonly GLogger Log = GLog.Tag("Service");

        readonly List<(Action onReady, Action<string> onFailed)> _waiters = new List<(Action, Action<string>)>();
        bool _timedOut;

        public ServiceState State { get; private set; } = ServiceState.Idle;
        public bool IsReady => State == ServiceState.Ready;

        /// <summary>Lý do lỗi khi Failed, null nếu không lỗi.</summary>
        public string Error { get; private set; }

        /// <summary>Tên hiện trong log.</summary>
        public virtual string Name => GetType().Name;

        /// <summary>Quá thời gian này mà chưa xong thì coi là Failed. 0 = không giới hạn.</summary>
        public virtual float TimeoutSeconds => 10f;

        /// <summary>Bắn mỗi khi State đổi.</summary>
        public event Action<GnurtService> StateChanged;

        /// <summary>Logic khởi tạo. Xong ngay hay xong sau đều được, miễn là cuối cùng gọi Complete() hoặc Fail().</summary>
        protected abstract void OnInitialize();

        /// <summary>Bắt đầu khởi tạo. Gọi nhiều lần cũng chỉ chạy một lần.</summary>
        public void Initialize()
        {
            if (State != ServiceState.Idle) return;

            SetState(ServiceState.Initializing);
            StartTimeout();
            try
            {
                OnInitialize();
            }
            catch (Exception e)
            {
                Log.Error($"{Name} ném exception khi khởi tạo", e);
                Fail(e.Message);
            }
        }

        /// <summary>Báo khởi tạo xong. Gọi được từ thread khác.</summary>
        protected void Complete() => MainThread.Run(() =>
        {
            // Đã Failed vì timeout mà sau đó vẫn xong thì chuyển sang Ready,
            // nhưng không gọi lại callback (mỗi callback chỉ được gọi một lần).
            bool lateAfterTimeout = State == ServiceState.Failed && _timedOut;
            if (State != ServiceState.Initializing && !lateAfterTimeout) return;

            Error = null;
            SetState(ServiceState.Ready);
            Flush(ready: true);
        });

        /// <summary>Báo khởi tạo lỗi. Gọi được từ thread khác.</summary>
        protected void Fail(string error) => MainThread.Run(() =>
        {
            if (State != ServiceState.Initializing) return;

            Error = error;
            Log.Error($"{Name} lỗi: {error}");
            SetState(ServiceState.Failed);
            Flush(ready: false);
        });

        /// <summary>
        /// Gọi onReady khi Ready, hoặc onFailed khi Failed. Đã xong rồi thì gọi ngay.
        /// Mỗi cặp callback chỉ được gọi đúng một lần. Gọi từ main thread.
        /// </summary>
        public void WhenReady(Action onReady, Action<string> onFailed = null)
        {
            switch (State)
            {
                case ServiceState.Ready: Safe(onReady); break;
                case ServiceState.Failed: Safe(() => onFailed?.Invoke(Error)); break;
                default: _waiters.Add((onReady, onFailed)); break;
            }
        }

        void StartTimeout()
        {
            if (TimeoutSeconds <= 0) return;

            // Đợi ở thread nền, rồi quay về main thread để kiểm tra.
            Task.Delay(TimeSpan.FromSeconds(TimeoutSeconds)).ContinueWith(_ => MainThread.Run(() =>
            {
                if (State != ServiceState.Initializing) return;
                _timedOut = true;
                Fail($"quá {TimeoutSeconds} giây chưa khởi tạo xong");
            }));
        }

        void Flush(bool ready)
        {
            var waiters = _waiters.ToArray();
            _waiters.Clear();
            foreach (var (onReady, onFailed) in waiters)
                Safe(ready ? onReady : () => onFailed?.Invoke(Error));
        }

        void SetState(ServiceState state)
        {
            State = state;
            Safe(() => StateChanged?.Invoke(this));
        }

        void Safe(Action action)
        {
            try
            {
                action?.Invoke();
            }
            catch (Exception e)
            {
                Log.Error($"Callback của {Name} lỗi", e);
            }
        }
    }
}
