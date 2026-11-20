using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using UnityEditor;
using UnityEditor.Compilation;
using UnityEditor.PackageManager;
using UnityEngine;
using PackageInfo = UnityEditor.PackageManager.PackageInfo;

namespace Gnurt.Hub.Bootstrap
{
    /// <summary>
    /// Hub cần com.gnurt.core mới chạy được. Khi Unity mở mà project chưa có core (hoặc core quá cũ),
    /// lớp này tự cài core từ cùng repo với Hub, để người dùng chỉ phải dán một URL.
    /// Assembly này không phụ thuộc package nào nên luôn compile được, kể cả khi chưa có core.
    /// </summary>
    [InitializeOnLoad]
    static class CoreInstaller
    {
        internal const string CoreName = "com.gnurt.core";

        /// <summary>Version core tối thiểu mà bản Hub này cần. Đổi cùng lúc với versionDefines của Gnurt.Hub.Editor.</summary>
        internal const string RequiredCoreVersion = "0.1.0";

        const string HubTagPrefix = "hub/v";
        const string CoreTagPrefix = "core/v";
        const string CorePath = "/Packages/com.gnurt.core";
        const string TriedKey = "Gnurt.Hub.CoreInstallTried";
        const string RestartAskedKey = "Gnurt.Hub.RestartAsked";

        // Tên các assembly vừa compile trong lượt hiện tại.
        static readonly HashSet<string> CompiledThisRound = new HashSet<string>();

        static CoreInstaller()
        {
            EditorApplication.delayCall += Check;
            CompilationPipeline.compilationStarted += _ => CompiledThisRound.Clear();
            CompilationPipeline.assemblyCompilationFinished += (path, _) => CompiledThisRound.Add(Path.GetFileNameWithoutExtension(path));
            CompilationPipeline.compilationFinished += _ => CheckCoreCompiled();
        }

        /// <summary>
        /// Unity 6000.3 (đã gặp khi chạy batchmode): package git thêm vào giữa phiên có thể không được compile cho tới khi
        /// mở lại Unity. Dấu hiệu: lượt compile có Hub mà không có core, dù core đã nằm trong project. Khi đó hỏi mở lại project.
        /// Unity bình thường compile được core thì điều kiện này không bao giờ đúng.
        /// </summary>
        static void CheckCoreCompiled()
        {
            bool stuck = CompiledThisRound.Contains("Gnurt.Hub.Editor")
                && !CompiledThisRound.Contains("Gnurt.Core")
                && !AppDomain.CurrentDomain.GetAssemblies().Any(a => a.GetName().Name == "Gnurt.Core")
                && PackageInfo.FindForAssetPath("Packages/" + CoreName) != null;
            if (!stuck || SessionState.GetBool(RestartAskedKey, false)) return;
            SessionState.SetBool(RestartAskedKey, true);

            const string message = "Gnurt Core đã được cài nhưng Unity chưa nạp nó trong phiên này. Mở lại project là xong.\n\n"
                + "Gnurt Core is installed but Unity has not loaded it in this session. Reopening the project fixes it.";
            if (Application.isBatchMode)
            {
                Debug.LogWarning("[Gnurt][Hub] " + message.Replace("\n", " "));
                return;
            }
            if (EditorUtility.DisplayDialog("Gnurt Hub", message, "Mở lại / Reopen", "Để sau / Later"))
                EditorApplication.OpenProject(Directory.GetCurrentDirectory());
        }

        static void Check()
        {
            var core = PackageInfo.FindForAssetPath("Packages/" + CoreName);
            if (core != null && !IsOlder(core.version, RequiredCoreVersion)) return; // đã có core đủ mới

            // Mỗi phiên Unity chỉ tự thử một lần, để lỗi mạng không làm nó cài đi cài lại.
            if (SessionState.GetBool(TriedKey, false)) return;
            SessionState.SetBool(TriedKey, true);
            Install();
        }

        /// <summary>Cài core từ cùng repo với Hub. Menu Gnurt > Hub gọi lại hàm này khi lần tự cài bị lỗi.</summary>
        internal static void Install()
        {
            var hub = PackageInfo.FindForAssembly(typeof(CoreInstaller).Assembly);
            string identifier = hub != null ? CoreIdentifier(hub.packageId, hub.name) : null;
            if (identifier == null)
            {
                Debug.LogWarning($"[Gnurt][Hub] Hub không được cài từ git nên không tự cài được {CoreName}. Hãy cài {CoreName} bằng tay.");
                return;
            }

            Debug.Log($"[Gnurt][Hub] Đang cài {CoreName}: {identifier}");
            var request = Client.Add(identifier);
            EditorApplication.CallbackFunction poll = null;
            poll = () =>
            {
                if (!request.IsCompleted) return;
                EditorApplication.update -= poll;
                if (request.Status == StatusCode.Success)
                    Debug.Log($"[Gnurt][Hub] Đã cài {CoreName} {request.Result.version}. Mở Hub bằng menu Gnurt > Hub.");
                else
                    Debug.LogError($"[Gnurt][Hub] Cài {CoreName} lỗi: {request.Error?.message}. Thử lại bằng menu Gnurt > Hub.");
            };
            EditorApplication.update += poll;
        }

        /// <summary>
        /// Git URL của core, cùng repo với Hub. Hub cài bằng tag "hub/vX" thì core dùng tag "core/v{RequiredCoreVersion}";
        /// Hub cài bằng nhánh hay commit thì core dùng đúng nhánh/commit đó. Trả null nếu Hub không cài từ git.
        /// </summary>
        internal static string CoreIdentifier(string hubPackageId, string hubName)
        {
            string prefix = hubName + "@";
            if (string.IsNullOrEmpty(hubPackageId) || !hubPackageId.StartsWith(prefix, StringComparison.Ordinal)) return null;

            string source = hubPackageId.Substring(prefix.Length);
            bool explicitGit = source.StartsWith("git+", StringComparison.Ordinal);
            if (explicitGit) source = source.Substring(4);

            string revision = null;
            int hash = source.IndexOf('#');
            if (hash >= 0)
            {
                revision = source.Substring(hash + 1);
                source = source.Substring(0, hash);
            }
            int query = source.IndexOf('?');
            string url = query >= 0 ? source.Substring(0, query) : source;
            if (!explicitGit && !LooksLikeGit(url)) return null; // cài từ file:, tgz hay registry

            if (revision != null && revision.StartsWith(HubTagPrefix, StringComparison.Ordinal))
                revision = CoreTagPrefix + RequiredCoreVersion;
            return url + "?path=" + CorePath + (string.IsNullOrEmpty(revision) ? "" : "#" + revision);
        }

        static bool LooksLikeGit(string url) =>
            url.EndsWith(".git", StringComparison.OrdinalIgnoreCase)
            || url.StartsWith("git@", StringComparison.Ordinal)
            || url.StartsWith("ssh://", StringComparison.Ordinal)
            || url.StartsWith("git://", StringComparison.Ordinal);

        /// <summary>version cũ hơn required không (so 3 số đầu, bỏ qua phần preview).</summary>
        internal static bool IsOlder(string version, string required)
        {
            int[] a = Numbers(version), b = Numbers(required);
            for (int i = 0; i < 3; i++)
                if (a[i] != b[i]) return a[i] < b[i];
            return false;
        }

        static int[] Numbers(string version)
        {
            var result = new int[3];
            var parts = (version ?? "").Split('-')[0].Split('.');
            for (int i = 0; i < 3 && i < parts.Length; i++)
                int.TryParse(parts[i], out result[i]);
            return result;
        }
    }
}
