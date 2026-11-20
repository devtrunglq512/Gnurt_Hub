using UnityEngine;

namespace Gnurt
{
    /// <summary>
    /// Singleton cho MonoBehaviour, sống qua mọi scene.
    /// Gọi Instance lần đầu mà chưa có thì tự tạo GameObject; có bản trùng thì bản sau tự huỷ.
    /// Lớp con override Awake thì nhớ gọi base.Awake().
    /// </summary>
    public abstract class PersistentMonoSingleton<T> : MonoBehaviour where T : PersistentMonoSingleton<T>
    {
        static T _instance;
        static bool _quitting;

        static PersistentMonoSingleton()
        {
            // Tắt domain reload: static không tự reset nên dọn khi thoát Play Mode.
            PlayModeTracker.Exited += () =>
            {
                _instance = null;
                _quitting = false;
            };
        }

        /// <summary>Instance hiện tại. Trả null khi đang tắt game, hoặc ở Edit Mode mà scene chưa có sẵn.</summary>
        public static T Instance
        {
            get
            {
                if (_quitting) return null; // đang tắt game thì không tạo mới
                if (_instance != null) return _instance;

                _instance = FindAnyObjectByType<T>();
                if (_instance == null && Application.isPlaying)
                    new GameObject(typeof(T).Name).AddComponent<T>(); // Awake sẽ gán _instance
                return _instance;
            }
        }

        public static bool HasInstance => _instance != null;

        protected virtual void Awake()
        {
            if (_instance != null && _instance != this)
            {
                GLog.Tag("Singleton").Warning($"Có hơn một {typeof(T).Name}, huỷ bản thừa", this);
                Destroy(gameObject);
                return;
            }

            _instance = (T)this;
            transform.SetParent(null); // DontDestroyOnLoad chỉ chạy với object gốc
            DontDestroyOnLoad(gameObject);
        }

        protected virtual void OnApplicationQuit() => _quitting = true;

        protected virtual void OnDestroy()
        {
            if (_instance == this) _instance = null;
        }
    }
}
