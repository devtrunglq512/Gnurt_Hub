using System;
using System.Linq;
using System.Threading.Tasks;
using Gnurt.Editor;
using UnityEditor;

namespace Gnurt.Hub
{
    /// <summary>Các thao tác người dùng bấm trong Hub: hỏi xác nhận, chạy, báo lỗi bằng hộp thoại.</summary>
    static class HubActions
    {
        const string Title = "Gnurt Hub";

        static HubService Hub => HubService.Instance;

        public static void Install(string name, string revision = null)
        {
            var steps = Hub.PlanInstall(name, revision, out string error);
            if (steps == null)
            {
                Show(error);
                return;
            }

            string list = string.Join("\n", steps.Select(s => "•  " + s));
            if (!Confirm(Loc.T($"Unity will install:\n{list}\n\nThen it recompiles scripts.", $"Unity sẽ cài:\n{list}\n\nSau đó Unity compile lại script."),
                    Loc.T("Install", "Cài"))) return;

            Run(() => Hub.InstallAsync(steps));
        }

        public static void Update(PackageState state)
        {
            string revision = Hub.RevisionFor(state);
            if (!Confirm(Loc.T($"Update {state.Entry.Title} to {revision}?", $"Cập nhật {state.Entry.Title} lên {revision}?"), Loc.T("Update", "Cập nhật"))) return;
            Run(() => Hub.InstallAsync(state.Entry.name, revision));
        }

        public static void SwitchVersion(PackageState state, string revision)
        {
            if (revision == state.Revision) return;
            if (!Confirm(Loc.T($"Switch {state.Entry.Title} to {revision}?", $"Chuyển {state.Entry.Title} sang {revision}?"), Loc.T("Switch", "Chuyển"))) return;
            Run(() => Hub.InstallAsync(state.Entry.name, revision));
        }

        public static void UpdateAll()
        {
            if (!Confirm(Loc.T("Update every Gnurt package that has a newer version?", "Cập nhật mọi package Gnurt đang có bản mới?"), Loc.T("Update all", "Cập nhật hết"))) return;
            Run(async () =>
            {
                int count = await Hub.UpdateAllAsync();
                if (count == 0) Show(Loc.T("Everything is up to date.", "Mọi package đều đã mới nhất."));
            });
        }

        public static void Remove(PackageState state)
        {
            var blockers = Hub.RemoveBlockers(state.Entry.name);
            if (blockers.Count > 0)
            {
                Show(Loc.T($"Can't remove {state.Entry.Title}: {string.Join(", ", blockers)} needs it.",
                    $"Không gỡ được {state.Entry.Title} vì {string.Join(", ", blockers)} đang cần nó."));
                return;
            }
            if (!Confirm(Loc.T($"Remove {state.Entry.Title} from this project?", $"Gỡ {state.Entry.Title} khỏi project này?"), Loc.T("Remove", "Gỡ"))) return;
            Run(() => Hub.RemoveAsync(state.Entry.name));
        }

        public static void UseLocal(PackageState state)
        {
            if (!Confirm(Loc.T($"Use the local copy of {state.Entry.Title}?\n{Hub.LocalFolderOf(state)}\n\nEdits there show up in this project right away.",
                    $"Dùng bản local của {state.Entry.Title}?\n{Hub.LocalFolderOf(state)}\n\nSửa code ở đó là project này thấy ngay."),
                    Loc.T("Use local", "Dùng bản local"))) return;
            Run(() => Hub.UseLocalAsync(state.Entry.name));
        }

        public static void UseGit(PackageState state)
        {
            if (!Confirm(Loc.T($"Switch {state.Entry.Title} back to the git version ({Hub.RevisionFor(state)})?",
                    $"Chuyển {state.Entry.Title} về bản git ({Hub.RevisionFor(state)})?"), Loc.T("Switch", "Chuyển"))) return;
            Run(() => Hub.UseGitAsync(state.Entry.name));
        }

        static bool Confirm(string message, string ok) =>
            EditorUtility.DisplayDialog(Title, message, ok, Loc.T("Cancel", "Huỷ"));

        static void Show(string message) => EditorUtility.DisplayDialog(Title, message, "OK");

        // Chạy một thao tác async; lỗi thì hiện hộp thoại thay vì chỉ ghi log.
        static async void Run(Func<Task> action)
        {
            try
            {
                await action();
            }
            catch (Exception e)
            {
                Show(Loc.T("Something went wrong:\n", "Có lỗi:\n") + e.Message);
            }
        }
    }
}
