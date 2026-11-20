// Chỉ compile khi project chưa có com.gnurt.core (GNURT_CORE do versionDefines của asmdef đặt).
// Lúc đó Hub chưa chạy được, nên menu Gnurt > Hub sẽ cài core trước. Có core rồi thì HubWindow giữ menu này.
#if !GNURT_CORE
using UnityEditor;

namespace Gnurt.Hub.Bootstrap
{
    static class BootstrapMenu
    {
        [MenuItem("Gnurt/Hub", priority = 0)]
        static void InstallCore()
        {
            bool ok = EditorUtility.DisplayDialog("Gnurt Hub",
                "Hub cần package Gnurt Core để chạy. Cài ngay bây giờ?\n\nHub needs the Gnurt Core package. Install it now?",
                "Cài / Install", "Để sau / Later");
            if (ok) CoreInstaller.Install();
        }
    }
}
#endif
