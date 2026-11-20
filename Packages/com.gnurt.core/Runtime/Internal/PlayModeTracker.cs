using System;

namespace Gnurt
{
    /// <summary>
    /// Báo khi thoát Play Mode trong Editor để dọn dữ liệu static.
    /// Cần khi tắt domain reload: lúc đó static không tự reset giữa các lần Play.
    /// </summary>
    internal static class PlayModeTracker
    {
#if UNITY_EDITOR
        /// <summary>Đang ở Play Mode (hoặc sắp vào).</summary>
        public static bool IsPlaying { get; private set; }

        /// <summary>Vừa quay về Edit Mode.</summary>
        public static event Action Exited;

        [UnityEditor.InitializeOnLoadMethod]
        static void Init()
        {
            IsPlaying = UnityEditor.EditorApplication.isPlayingOrWillChangePlaymode;
            UnityEditor.EditorApplication.playModeStateChanged += state =>
            {
                if (state == UnityEditor.PlayModeStateChange.ExitingEditMode)
                {
                    IsPlaying = true;
                }
                else if (state == UnityEditor.PlayModeStateChange.EnteredEditMode)
                {
                    IsPlaying = false;
                    Exited?.Invoke();
                }
            };
        }
#else
        // Bản build: luôn đang chạy và không bao giờ thoát Play Mode.
        public static bool IsPlaying => true;
        public static event Action Exited { add { } remove { } }
#endif
    }
}
