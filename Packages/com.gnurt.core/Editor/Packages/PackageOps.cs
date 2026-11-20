using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Threading.Tasks;
using UnityEditor;
using UnityEditor.PackageManager;
using UnityEditor.PackageManager.Requests;
using PackageInfo = UnityEditor.PackageManager.PackageInfo;

namespace Gnurt.Editor
{
    /// <summary>
    /// Cài, gỡ, liệt kê package qua Package Manager, dạng async.
    /// Gọi từ main thread. Cài/gỡ xong, Unity có thể reload domain.
    /// </summary>
    public static class PackageOps
    {
        static readonly GLogger Log = GLog.Tag("PackageOps");
        static int _running;

        /// <summary>Đang có thao tác cài/gỡ chạy. Package Manager chỉ nên làm từng việc một.</summary>
        public static bool IsBusy => _running > 0;

        /// <summary>Các package đang có trong project (chỉ đọc máy, không gọi mạng).</summary>
        public static async Task<IReadOnlyList<PackageInfo>> ListAsync()
        {
            var request = Client.List(true);
            await Wait(request);
            return request.Result.ToList();
        }

        /// <summary>Thêm package hoặc đổi version. identifier: "tên@1.0.0", git URL hoặc "file:...".</summary>
        public static async Task<PackageInfo> AddAsync(string identifier)
        {
            using (Busy())
            {
                var request = Client.Add(identifier);
                await Wait(request);
                Log.Info($"Đã cài {request.Result.name} {request.Result.version}");
                return request.Result;
            }
        }

        /// <summary>Gỡ package theo tên.</summary>
        public static async Task RemoveAsync(string packageName)
        {
            using (Busy())
            {
                await Wait(Client.Remove(packageName));
                Log.Info($"Đã gỡ {packageName}");
            }
        }

        /// <summary>Thêm và gỡ nhiều package trong một lần, Unity chỉ resolve và compile một lần.</summary>
        public static async Task AddAndRemoveAsync(IEnumerable<string> add, IEnumerable<string> remove)
        {
            using (Busy())
            {
                await Wait(Client.AddAndRemove(add?.ToArray(), remove?.ToArray()));
                Log.Info("Đã cập nhật danh sách package");
            }
        }

        /// <summary>Tiền tố tên của mọi package Gnurt.</summary>
        public const string GnurtPrefix = "com.gnurt.";

        /// <summary>Package có thuộc Gnurt không (tên bắt đầu bằng <c>com.gnurt.</c>).</summary>
        public static bool IsGnurtPackage(string packageName) =>
            packageName != null && packageName.StartsWith(GnurtPrefix, StringComparison.Ordinal);

        /// <summary>Thông tin package chứa một kiểu, ví dụ <c>InfoOf(typeof(MyWindow))</c>. null nếu kiểu đó không thuộc package nào.</summary>
        public static PackageInfo InfoOf(Type typeInPackage) => PackageInfo.FindForAssembly(typeInPackage.Assembly);

        /// <summary>Version của package chứa một kiểu. null nếu không thuộc package nào.</summary>
        public static string VersionOf(Type typeInPackage) => InfoOf(typeInPackage)?.version;

        /// <summary>
        /// Liệt kê tag version của repo git, mới nhất trước. Git chạy ở thread nền.
        /// Repo cần đăng nhập mà máy chưa có quyền thì báo lỗi ngay, không treo chờ nhập mật khẩu.
        /// </summary>
        public static async Task<IReadOnlyList<string>> ListVersionTagsAsync(string repoUrl, string tagPrefix = "")
        {
            if (string.IsNullOrWhiteSpace(repoUrl) || repoUrl.Contains("\""))
                throw new ArgumentException("URL repo không hợp lệ", nameof(repoUrl));

            string output = await Task.Run(() => RunGit($"ls-remote --tags --refs \"{repoUrl}\"", 30));
            var tags = output
                .Split('\n')
                .Select(line => line.Trim())
                .Where(line => line.Length > 0)
                .Select(line => line.Substring(line.LastIndexOf('\t') + 1).Replace("refs/tags/", ""));
            return VersionTags.FilterAndSort(tags, tagPrefix);
        }

        // Chạy lệnh git và trả về stdout. Lỗi hoặc quá thời gian thì ném exception.
        static string RunGit(string arguments, int timeoutSeconds)
        {
            var info = new ProcessStartInfo("git", arguments)
            {
                UseShellExecute = false,
                RedirectStandardOutput = true,
                RedirectStandardError = true,
                CreateNoWindow = true,
            };
            info.EnvironmentVariables["GIT_TERMINAL_PROMPT"] = "0"; // không hỏi mật khẩu trong terminal
            info.EnvironmentVariables["GCM_INTERACTIVE"] = "never"; // không bật cửa sổ đăng nhập của Git Credential Manager

            using var process = Process.Start(info);
            var error = process.StandardError.ReadToEndAsync();
            var output = process.StandardOutput.ReadToEndAsync();
            if (!process.WaitForExit(timeoutSeconds * 1000))
            {
                try { process.Kill(); } catch { /* process có thể đã tự thoát */ }
                throw new TimeoutException($"git {arguments}: quá {timeoutSeconds} giây");
            }
            if (process.ExitCode != 0)
                throw new InvalidOperationException(error.Result.Trim());
            return output.Result;
        }

        // Chờ request của Package Manager xong. Lỗi thì ném exception kèm thông báo của Unity.
        static Task Wait(Request request)
        {
            var done = new TaskCompletionSource<bool>();
            void Poll()
            {
                if (!request.IsCompleted) return;
                EditorApplication.update -= Poll;
                if (request.Status == StatusCode.Success) done.SetResult(true);
                else done.SetException(new InvalidOperationException(request.Error?.message ?? "Package Manager báo lỗi"));
            }
            EditorApplication.update += Poll;
            return done.Task;
        }

        static IDisposable Busy()
        {
            _running++;
            return new BusyScope();
        }

        sealed class BusyScope : IDisposable
        {
            bool _disposed;

            public void Dispose()
            {
                if (_disposed) return;
                _disposed = true;
                _running--;
            }
        }
    }
}
