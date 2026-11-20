using System;
using System.IO;
using System.Threading.Tasks;
using Gnurt.Hub;
using UnityEditor;
using UnityEditor.PackageManager;
using UnityEngine;
using PackageInfo = UnityEditor.PackageManager.PackageInfo;

// Kiểm tra Hub sau khi được cài thật từ git, trong project trống. Chỉ chạy ở lần "check" của Tools/e2e_hub.sh
// (lần chạy trước đó đã để bootstrap thêm core). Giữa chừng Unity reload domain khi đổi nguồn core,
// nên tiến độ được lưu trong SessionState. Assembly này chỉ compile khi project đã có cả Hub và core.
[InitializeOnLoad]
static class GnurtE2EChecks
{
    const string StageKey = "GnurtE2E.Stage";
    const string CoreName = "com.gnurt.core";

    static string Revision => Environment.GetEnvironmentVariable("GNURT_E2E_REVISION") ?? "dev/0.1.0";
    static string ReposRoot => Environment.GetEnvironmentVariable("GNURT_E2E_REPOS") ?? "D:/UnityProject";
    static string Phase => Environment.GetEnvironmentVariable("GNURT_E2E_PHASE") ?? "check";
    static string ResultPath => Path.Combine(Application.dataPath, "..", "e2e-result.txt");

    static GnurtE2EChecks()
    {
        if (Application.isBatchMode && Phase == "check") EditorApplication.delayCall += Run;
    }

    static async void Run()
    {
        int stage = SessionState.GetInt(StageKey, 0);
        try
        {
            switch (stage)
            {
                case 0: await CheckFreshInstall(); break;
                case 1: await CheckCoreSource(PackageSource.Local, nextStage: 2); break;
                case 2: await CheckCoreSource(PackageSource.Git, nextStage: 3); break;
            }
        }
        catch (Exception e)
        {
            Finish(false, $"bước {stage}: {e.Message}\n{e.StackTrace}");
        }
    }

    // Bước 0: Hub vừa được cài từ git, bootstrap đã tự cài core.
    static async Task CheckFreshInstall()
    {
        var core = Core();
        Expect(core != null, "bootstrap chưa cài com.gnurt.core");
        Expect(core.source == PackageSource.Git, $"core phải cài từ git, đang là {core.source}");
        Expect(core.packageId.EndsWith("#" + Revision), $"core phải cùng nhánh với Hub: {core.packageId}");
        Pass("Mở lại project: Hub và core từ git đều compile được");

        var hub = HubService.Instance;
        await hub.RefreshAsync();
        Expect(hub.LastError == null, "làm mới Hub lỗi: " + hub.LastError);
        Expect(hub.CatalogSource == CatalogSource.Remote, $"catalog phải tải từ GitHub, đang là {hub.CatalogSource} ({hub.CatalogError})");
        Pass($"Catalog tải từ GitHub: {hub.Catalog.packages.Length} package");

        var coreState = hub.StateOf(CoreName);
        Expect(coreState.TagsError == null, "không đọc được tag: " + coreState.TagsError);
        Pass($"Đọc tag qua git: core có {coreState.Tags.Count} tag phát hành");

        Expect(HubSettings.UseDevChannel, "Hub cài từ nhánh thì kênh Dev phải tự bật");
        Expect(HubSettings.DevBranch == Revision, $"nhánh dev phải là {Revision}, đang là {HubSettings.DevBranch}");
        Pass($"Kênh Dev tự bật theo nhánh của Hub ({HubSettings.DevBranch})");

        Expect(hub.RemoveBlockers(CoreName).Contains("Gnurt Hub"), "gỡ core phải bị chặn vì Hub cần nó");
        Pass("Không cho gỡ core khi Hub còn cần");

        var plan = hub.PlanInstall("com.gnurt.hub", null, out string error);
        Expect(plan != null && plan.Count == 1 && plan[0].Identifier.EndsWith("#" + Revision), "kế hoạch cài lại Hub sai: " + error);
        Pass("Kế hoạch cài lại Hub: " + plan[0].Identifier);

        HubSettings.ReposRoot = ReposRoot;
        Expect(hub.HasLocalFolder(coreState), "không thấy bản local của core trong " + ReposRoot);
        SessionState.SetInt(StageKey, 1);
        Pass("Đang chuyển core sang bản local…");
        await hub.UseLocalAsync(CoreName);
        Later(Run); // nếu Unity không reload domain thì tự chạy bước tiếp
    }

    // Bước 1 và 2: core đã đổi nguồn chưa. Bước 1 xong thì chuyển core về git; bước 2 xong là hết.
    static async Task CheckCoreSource(PackageSource expected, int nextStage)
    {
        var core = Core();
        if (core == null || core.source != expected)
        {
            // Unity cần chút thời gian để nhận nguồn mới; chờ tối đa 2 phút.
            string waitKey = "GnurtE2E.WaitStart" + nextStage;
            float start = SessionState.GetFloat(waitKey, -1);
            if (start < 0) SessionState.SetFloat(waitKey, start = (float)EditorApplication.timeSinceStartup);
            if (EditorApplication.timeSinceStartup - start > 120)
                throw new Exception($"core không chuyển sang {expected} (đang là {core?.source})");
            Later(Run);
            return;
        }

        if (expected == PackageSource.Local)
        {
            string path = core.resolvedPath.Replace('\\', '/');
            Expect(path.EndsWith("Gnurt_Hub/Packages/com.gnurt.core"), "core local trỏ sai chỗ: " + path);
            Pass("Core đang dùng bản local: " + path);

            var hub = HubService.Instance;
            await hub.RefreshAsync(); // sau reload, service là bản mới nên phải tải lại
            SessionState.SetInt(StageKey, nextStage);
            Pass("Đang chuyển core về bản git…");
            await hub.UseGitAsync(CoreName);
            Later(Run);
            return;
        }

        Expect(core.packageId.EndsWith("#" + Revision), "core về git sai nhánh: " + core.packageId);
        Pass("Core đã về bản git: " + core.packageId);
        Finish(true, "mọi bước đều đạt");
    }

    static PackageInfo Core() => PackageInfo.FindForAssetPath("Packages/" + CoreName);

    static void Expect(bool condition, string failure)
    {
        if (!condition) throw new Exception(failure);
    }

    static void Pass(string message)
    {
        Debug.Log("[GnurtE2E] OK  " + message);
        File.AppendAllText(ResultPath, "OK   " + message + "\n");
    }

    static void Finish(bool ok, string message)
    {
        Debug.Log($"[GnurtE2E] {(ok ? "PASS" : "FAIL")} {message}");
        File.AppendAllText(ResultPath, (ok ? "PASS " : "FAIL ") + message + "\n");
        EditorApplication.Exit(ok ? 0 : 1);
    }

    // Chạy lại sau khoảng 1 giây, để không quay vòng liên tục khi đang chờ.
    static void Later(Action action)
    {
        double at = EditorApplication.timeSinceStartup + 1;
        EditorApplication.CallbackFunction tick = null;
        tick = () =>
        {
            if (EditorApplication.timeSinceStartup < at) return;
            EditorApplication.update -= tick;
            action();
        };
        EditorApplication.update += tick;
    }
}
