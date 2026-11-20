using System.IO;
using UnityEditor;
using UnityEngine;

// Không để test treo mãi: quá thời gian thì ghi TIMEOUT rồi thoát Unity.
// Assembly này không phụ thuộc gì nên luôn compile được, kể cả khi Hub hay core lỗi.
[InitializeOnLoad]
static class GnurtE2EWatchdog
{
    const double LimitSeconds = 900;

    static GnurtE2EWatchdog()
    {
        if (!Application.isBatchMode) return;
        EditorApplication.update += () =>
        {
            if (EditorApplication.timeSinceStartup < LimitSeconds) return;
            File.AppendAllText(Path.Combine(Application.dataPath, "..", "e2e-result.txt"), "FAIL timeout: quá 15 phút\n");
            EditorApplication.Exit(3);
        };
    }
}
