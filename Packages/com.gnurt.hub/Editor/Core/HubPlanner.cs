using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using Gnurt.Editor;

namespace Gnurt.Hub
{
    /// <summary>Một bước cài: package nào, ở tag/nhánh nào, bằng chuỗi gì.</summary>
    public sealed class InstallStep
    {
        public CatalogEntry Entry;
        public string Revision;
        public string Identifier;

        public override string ToString() => $"{Entry.Title} ({Revision})";
    }

    /// <summary>Logic thuần của Hub, không gọi Unity, tách riêng để test được.</summary>
    public static class HubPlanner
    {
        /// <summary>Git URL để cài một package ở một tag hoặc nhánh.</summary>
        public static string Identifier(CatalogEntry entry, string revision) =>
            new GitSource(entry.repo, entry.path, revision).ToString();

        /// <summary>
        /// Các package phải cài khi cài <paramref name="name"/>: dependency còn thiếu đứng trước, chính nó đứng cuối.
        /// Dependency đã có trong project thì giữ nguyên. Trả null kèm lỗi khi thiếu trong catalog, chưa phát hành, hoặc phụ thuộc vòng.
        /// </summary>
        public static List<CatalogEntry> PlanInstall(HubCatalog catalog, string name, ICollection<string> installed, out string error)
        {
            var result = new List<CatalogEntry>();
            error = Visit(catalog, name, installed, result, new HashSet<string>(), new HashSet<string>());
            return error == null ? result : null;
        }

        static string Visit(HubCatalog catalog, string name, ICollection<string> installed,
            List<CatalogEntry> result, HashSet<string> visiting, HashSet<string> done)
        {
            if (done.Contains(name)) return null;
            if (!visiting.Add(name)) return $"phụ thuộc vòng tại {name}";

            var entry = catalog.Find(name);
            if (entry == null) return $"{name} không có trong catalog";
            if (entry.planned) return $"{entry.Title} chưa phát hành";

            foreach (string dependency in entry.dependencies)
            {
                if (installed.Contains(dependency)) continue;
                string error = Visit(catalog, dependency, installed, result, visiting, done);
                if (error != null) return error;
            }

            visiting.Remove(name);
            done.Add(name);
            result.Add(entry);
            return null;
        }

        /// <summary>Tên các package đang cài mà cần <paramref name="name"/>. Còn package nào thì không cho gỡ.</summary>
        public static List<string> InstalledDependents(HubCatalog catalog, string name, ICollection<string> installed) =>
            catalog.packages
                .Where(p => p.name != name && installed.Contains(p.name) && p.dependencies.Contains(name))
                .Select(p => p.Title)
                .ToList();

        /// <summary>Tag mới nhất có mới hơn version đang cài không.</summary>
        public static bool HasUpdate(string latestTag, string tagPrefix, string installedVersion)
        {
            if (latestTag == null || installedVersion == null) return false;
            return TryParseTag(latestTag, tagPrefix, out var latest)
                && PackageVersion.TryParse(installedVersion, out var current)
                && latest.CompareTo(current) > 0;
        }

        /// <summary>Revision có phải tag version của package này không (ví dụ "core/v1.2.0"), hay là nhánh/commit.</summary>
        public static bool IsVersionTag(string revision, string tagPrefix) => TryParseTag(revision, tagPrefix, out _);

        static bool TryParseTag(string tag, string tagPrefix, out PackageVersion version)
        {
            version = default;
            tagPrefix ??= "";
            return tag != null && tag.StartsWith(tagPrefix, StringComparison.Ordinal)
                && PackageVersion.TryParse(tag.Substring(tagPrefix.Length), out version);
        }

        /// <summary>Tên repo từ URL: ".../Gnurt_Hub.git" → "Gnurt_Hub".</summary>
        public static string RepoName(string repoUrl)
        {
            string url = repoUrl.TrimEnd('/');
            if (url.EndsWith(".git", StringComparison.OrdinalIgnoreCase)) url = url.Substring(0, url.Length - 4);
            int cut = Math.Max(url.LastIndexOf('/'), url.LastIndexOf(':'));
            return url.Substring(cut + 1);
        }

        /// <summary>Thư mục package trên máy, khi các repo nằm cạnh nhau trong <paramref name="reposRoot"/>.</summary>
        public static string LocalFolder(string reposRoot, CatalogEntry entry) =>
            Normalize(Path.GetFullPath(Path.Combine(reposRoot, RepoName(entry.repo), (entry.path ?? "").TrimStart('/'))));

        /// <summary>
        /// Chuỗi "file:" để cài package từ thư mục local. Cùng ổ đĩa với project thì dùng đường dẫn tương đối
        /// (tính từ thư mục Packages, đồng đội xếp thư mục giống nhau dùng chung được), khác ổ thì dùng đường dẫn tuyệt đối.
        /// </summary>
        public static string LocalIdentifier(string projectRoot, string folder)
        {
            string packagesDir = Normalize(Path.GetFullPath(Path.Combine(projectRoot, "Packages")));
            folder = Normalize(Path.GetFullPath(folder));

            var from = new Uri(packagesDir + "/");
            var to = new Uri(folder + "/");
            string relative = Uri.UnescapeDataString(from.MakeRelativeUri(to).ToString()).TrimEnd('/');

            // Khác ổ đĩa thì MakeRelativeUri trả về đường dẫn tuyệt đối dạng file:///...
            bool isRelative = !relative.StartsWith("file:", StringComparison.OrdinalIgnoreCase) && !Path.IsPathRooted(relative);
            return "file:" + (isRelative ? relative : folder);
        }

        static string Normalize(string path) => path.Replace('\\', '/').TrimEnd('/');
    }
}
