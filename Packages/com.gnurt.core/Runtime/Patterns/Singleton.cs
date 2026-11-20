namespace Gnurt
{
    /// <summary>
    /// Singleton cho class C# thường: <c>class Config : Singleton&lt;Config&gt; { }</c>, dùng <c>Config.Instance</c>.
    /// Hạn chế dùng vì khó test và tạo phụ thuộc ngầm; ưu tiên <see cref="Services"/>.
    /// </summary>
    public abstract class Singleton<T> where T : Singleton<T>, new()
    {
        static readonly object Lock = new object();
        static T _instance;
        static bool _createdInPlayMode;

        static Singleton()
        {
            // Tắt domain reload: instance tạo trong Play Mode bị bỏ khi thoát, lần Play sau tạo mới.
            PlayModeTracker.Exited += () =>
            {
                lock (Lock)
                    if (_createdInPlayMode) _instance = null;
            };
        }

        /// <summary>Instance duy nhất, tạo ở lần gọi đầu tiên.</summary>
        public static T Instance
        {
            get
            {
                lock (Lock)
                {
                    if (_instance == null)
                    {
                        _instance = new T();
                        _createdInPlayMode = PlayModeTracker.IsPlaying;
                    }
                    return _instance;
                }
            }
        }

        public static bool HasInstance => _instance != null;
    }
}
