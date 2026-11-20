using System.IO;
using UnityEditor;
using UnityEngine;

namespace Gnurt.Hub
{
    /// <summary>
    /// Cài đặt của Hub, lưu riêng cho từng người trong từng project
    /// (UserSettings/EditorUserSettings.asset, không commit lên git).
    /// </summary>
    public static class HubSettings
    {
        const string Prefix = "Gnurt.Hub.";
        const string FallbackDevBranch = "dev/0.1.0";

        /// <summary>URL catalog tự đặt. null = dùng URL mặc định.</summary>
        public static string CatalogUrlOverride
        {
            get => Get("CatalogUrl");
            set => Set("CatalogUrl", value);
        }

        /// <summary>
        /// true = cài bản đang phát triển từ nhánh dev; false = chỉ cài bản phát hành (tag).
        /// Mặc định theo cách Hub được cài: Hub cài từ nhánh thì bật.
        /// </summary>
        public static bool UseDevChannel
        {
            get => Get("DevChannel") is string value ? value == "1" : HubInfo.InstalledFromBranch;
            set => Set("DevChannel", value ? "1" : "0");
        }

        /// <summary>Nhánh dùng cho kênh dev. Mặc định là nhánh Hub đang cài.</summary>
        public static string DevBranch
        {
            get => Get("DevBranch") ?? (HubInfo.InstalledFromBranch ? HubInfo.Revision : FallbackDevBranch);
            set => Set("DevBranch", value?.Trim());
        }

        /// <summary>Thư mục chứa các repo Gnurt trên máy (dùng cho bản local). Mặc định là thư mục cha của project.</summary>
        public static string ReposRoot
        {
            get => Get("ReposRoot") ?? DefaultReposRoot;
            set => Set("ReposRoot", value?.Trim());
        }

        public static string DefaultReposRoot =>
            Path.GetFullPath(Path.Combine(Application.dataPath, "..", "..")).Replace('\\', '/');

        static string Get(string key)
        {
            string value = EditorUserSettings.GetConfigValue(Prefix + key);
            return string.IsNullOrEmpty(value) ? null : value;
        }

        static void Set(string key, string value) => EditorUserSettings.SetConfigValue(Prefix + key, value ?? "");
    }
}
