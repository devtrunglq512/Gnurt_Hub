using System;
using System.IO;
using System.Threading.Tasks;
using UnityEditor;
using UnityEngine;
using UnityEngine.Networking;

namespace Gnurt.Hub
{
    /// <summary>Catalog đang dùng lấy từ đâu.</summary>
    public enum CatalogSource
    {
        Remote,  // vừa tải trên GitHub
        Cache,   // bản tải lần trước, lưu trong Library
        BuiltIn, // bản nằm sẵn trong package Hub
    }

    /// <summary>Lấy catalog: tải trên mạng trước, không được thì bản đã lưu, cuối cùng là bản có sẵn trong Hub.</summary>
    public static class CatalogLoader
    {
        public const string BuiltInPath = "Packages/com.gnurt.hub/Editor/Catalog/catalog.json";
        const int TimeoutSeconds = 10;
        static readonly GLogger Log = GLog.Tag("Hub");

        static string CachePath => Path.GetFullPath(Path.Combine(Application.dataPath, "..", "Library", "Gnurt", "HubCatalog.json"));

        /// <summary>Trả về catalog, nguồn của nó, và lỗi khi tải trên mạng (null nếu tải được).</summary>
        public static async Task<(HubCatalog catalog, CatalogSource source, string remoteError)> LoadAsync(string url)
        {
            string remoteError;
            try
            {
                string json = await DownloadAsync(url);
                if (HubCatalog.TryParse(json, out var remote, out string parseError))
                {
                    SaveCache(json);
                    return (remote, CatalogSource.Remote, null);
                }
                remoteError = "catalog trên mạng sai định dạng: " + parseError;
            }
            catch (Exception e)
            {
                remoteError = e.Message;
            }

            Log.Info($"Không tải được catalog ({remoteError}), dùng bản dự phòng");
            if (File.Exists(CachePath) && HubCatalog.TryParse(File.ReadAllText(CachePath), out var cached, out _))
                return (cached, CatalogSource.Cache, remoteError);

            var asset = AssetDatabase.LoadAssetAtPath<TextAsset>(BuiltInPath);
            if (asset != null && HubCatalog.TryParse(asset.text, out var builtIn, out _))
                return (builtIn, CatalogSource.BuiltIn, remoteError);

            throw new InvalidOperationException("Không đọc được catalog nào. Lỗi khi tải: " + remoteError);
        }

        static Task<string> DownloadAsync(string url)
        {
            var done = new TaskCompletionSource<string>();
            var request = UnityWebRequest.Get(url);
            request.timeout = TimeoutSeconds;
            request.SendWebRequest().completed += _ =>
            {
                if (request.result == UnityWebRequest.Result.Success) done.SetResult(request.downloadHandler.text);
                else done.SetException(new InvalidOperationException($"HTTP {request.responseCode}: {request.error}"));
                request.Dispose();
            };
            return done.Task;
        }

        static void SaveCache(string json)
        {
            try
            {
                Directory.CreateDirectory(Path.GetDirectoryName(CachePath));
                File.WriteAllText(CachePath, json);
            }
            catch (Exception e)
            {
                // Không lưu được bản dự phòng thì vẫn dùng bản vừa tải, chỉ ghi log.
                Log.Warning("Không lưu được catalog vào Library: " + e.Message);
            }
        }
    }
}
