using System;
using System.Collections.Concurrent;
using System.Threading;

namespace Gnurt
{
    /// <summary>
    /// Đưa code về main thread. Callback của SDK bên thứ ba hay chạy ở thread khác,
    /// mà API của Unity chỉ gọi được ở main thread.
    /// </summary>
    public static class MainThread
    {
        static readonly ConcurrentQueue<Action> Queue = new ConcurrentQueue<Action>();
        static readonly GLogger Log = GLog.Tag("MainThread");
        static int _mainThreadId = -1;

        /// <summary>Code hiện tại có đang chạy ở main thread không.</summary>
        public static bool IsMainThread => Thread.CurrentThread.ManagedThreadId == _mainThreadId;

        /// <summary>Đang ở main thread thì chạy ngay, không thì xếp hàng chờ lượt sau.</summary>
        public static void Run(Action action)
        {
            if (IsMainThread) Invoke(action);
            else Queue.Enqueue(action);
        }

        /// <summary>Luôn xếp hàng và chạy ở lượt sau, kể cả khi đang ở main thread.</summary>
        public static void Post(Action action) => Queue.Enqueue(action);

        /// <summary>Chạy các việc đang chờ. Tự được gọi mỗi frame; test gọi tay.</summary>
        internal static void Pump()
        {
            // Chỉ chạy số việc có sẵn lúc bắt đầu, để việc mới thêm vào không làm vòng lặp chạy mãi.
            int count = Queue.Count;
            while (count-- > 0 && Queue.TryDequeue(out var action))
                Invoke(action);
        }

        static void Invoke(Action action)
        {
            try
            {
                action();
            }
            catch (Exception e)
            {
                Log.Error("Lỗi khi chạy việc trên main thread", e);
            }
        }

#if UNITY_EDITOR
        // Trong Editor, EditorApplication.update chạy cả ở Edit Mode lẫn Play Mode.
        [UnityEditor.InitializeOnLoadMethod]
        static void InitEditor()
        {
            _mainThreadId = Thread.CurrentThread.ManagedThreadId;
            UnityEditor.EditorApplication.update += Pump;
        }
#else
        // Bản build: gắn Pump vào đầu Update của PlayerLoop, không cần GameObject nên không ai xoá nhầm được.
        [UnityEngine.RuntimeInitializeOnLoadMethod(UnityEngine.RuntimeInitializeLoadType.SubsystemRegistration)]
        static void InitPlayer()
        {
            _mainThreadId = Thread.CurrentThread.ManagedThreadId;

            var loop = UnityEngine.LowLevel.PlayerLoop.GetCurrentPlayerLoop();
            for (int i = 0; i < loop.subSystemList.Length; i++)
            {
                var update = loop.subSystemList[i];
                if (update.type != typeof(UnityEngine.PlayerLoop.Update))
                    continue;

                var systems = new System.Collections.Generic.List<UnityEngine.LowLevel.PlayerLoopSystem>(update.subSystemList);
                systems.Insert(0, new UnityEngine.LowLevel.PlayerLoopSystem { type = typeof(MainThread), updateDelegate = Pump });
                update.subSystemList = systems.ToArray();
                loop.subSystemList[i] = update;
                break;
            }
            UnityEngine.LowLevel.PlayerLoop.SetPlayerLoop(loop);
        }
#endif
    }
}
