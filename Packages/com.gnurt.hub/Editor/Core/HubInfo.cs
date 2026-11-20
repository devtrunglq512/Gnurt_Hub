using Gnurt.Editor;
using PackageInfo = UnityEditor.PackageManager.PackageInfo;

namespace Gnurt.Hub
{
    /// <summary>Thông tin về chính Hub: version, cài từ đâu, lấy catalog ở đâu.</summary>
    public static class HubInfo
    {
        public const string HubName = "com.gnurt.hub";
        public const string CoreName = "com.gnurt.core";
        public const string RepoWebUrl = "https://github.com/devtrunglq512/Gnurt_Hub";
        const string RawRoot = "https://raw.githubusercontent.com/devtrunglq512/Gnurt_Hub/";

        public static PackageInfo Package => PackageOps.InfoOf(typeof(HubInfo));
        public static string Version => Package?.version ?? "dev";

        /// <summary>Tag, nhánh hoặc commit mà Hub được cài từ đó. null nếu Hub không cài từ git.</summary>
        public static string Revision => PackageOps.TryGetGitSource(Package, out var source) ? source.Revision : null;

        /// <summary>Hub đang cài từ một nhánh/commit (bản đang phát triển), không phải tag phát hành.</summary>
        public static bool InstalledFromBranch => Revision != null && !HubPlanner.IsVersionTag(Revision, "hub/");

        /// <summary>
        /// URL catalog mặc định. Hub bản dev đọc catalog trên đúng nhánh của nó;
        /// Hub bản phát hành đọc catalog mới nhất trên main.
        /// </summary>
        public static string DefaultCatalogUrl =>
            RawRoot + (InstalledFromBranch ? Revision : "main") + "/" + CatalogLoader.BuiltInPath;

        /// <summary>URL catalog đang dùng: URL người dùng tự đặt, không có thì URL mặc định.</summary>
        public static string CatalogUrl => HubSettings.CatalogUrlOverride ?? DefaultCatalogUrl;
    }
}
