using System;
using System.IO;
using UnityEditor;
using UnityEngine;
using PackageInfo = UnityEditor.PackageManager.PackageInfo;

// Hai việc không cần Hub hay core, nên để trong assembly không phụ thuộc gì (luôn compile được):
// 1. Lần chạy "install": chờ bootstrap của Hub thêm com.gnurt.core vào project rồi thoát,
//    để lần chạy sau Unity mở lại project với đủ cả hai package.
// 2. Không để test treo mãi: quá thời gian thì ghi lỗi rồi thoát Unity.
[InitializeOnLoad]
static class GnurtE2EWatchdog
{
    const double LimitSeconds = 600;

    static string ResultPath => Path.Combine(Application.dataPath, "..", "e2e-result.txt");
    static string Phase => Environment.GetEnvironmentVariable("GNURT_E2E_PHASE") ?? "check";

    static GnurtE2EWatchdog()
    {
        if (!Application.isBatchMode) return;
        double nextPoll = 0;
        EditorApplication.update += () =>
        {
            double now = EditorApplication.timeSinceStartup;
            if (now > LimitSeconds)
            {
                File.AppendAllText(ResultPath, $"FAIL {Phase}: quá {LimitSeconds / 60} phút\n");
                EditorApplication.Exit(3);
                return;
            }
            if (Phase != "install" || now < nextPoll) return;
            nextPoll = now + 2;

            var core = PackageInfo.FindForAssetPath("Packages/com.gnurt.core");
            if (core == null) return;
            File.AppendAllText(ResultPath, "OK   Bootstrap tự thêm core vào project: " + core.packageId + "\n");
            EditorApplication.Exit(0);
        };
    }
}
