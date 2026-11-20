using System;

namespace Gnurt.Editor
{
    /// <summary>
    /// Nguồn git của một package, dạng <c>url?path=/Packages/x#rev</c>.
    /// </summary>
    public readonly struct GitSource
    {
        /// <summary>URL repo, không gồm ?path và #rev.</summary>
        public readonly string Url;

        /// <summary>Thư mục package trong repo. null nếu package nằm ở gốc repo.</summary>
        public readonly string Path;

        /// <summary>Tag, branch hoặc commit sau dấu #. null nếu không có.</summary>
        public readonly string Revision;

        public GitSource(string url, string path = null, string revision = null)
        {
            Url = url;
            Path = string.IsNullOrEmpty(path) ? null : path;
            Revision = string.IsNullOrEmpty(revision) ? null : revision;
        }

        /// <summary>Cùng repo và thư mục, đổi sang tag/branch khác.</summary>
        public GitSource WithRevision(string revision) => new GitSource(Url, Path, revision);

        /// <summary>Đọc một git URL. Trả false nếu chuỗi không giống git URL.</summary>
        public static bool TryParse(string text, out GitSource source)
        {
            source = default;
            if (string.IsNullOrWhiteSpace(text)) return false;

            string url = text.Trim();
            bool explicitGit = url.StartsWith("git+", StringComparison.Ordinal);
            if (explicitGit) url = url.Substring(4);

            string revision = null;
            int hash = url.IndexOf('#');
            if (hash >= 0)
            {
                revision = url.Substring(hash + 1);
                url = url.Substring(0, hash);
            }

            string path = null;
            int query = url.IndexOf('?');
            if (query >= 0)
            {
                foreach (var part in url.Substring(query + 1).Split('&'))
                    if (part.StartsWith("path=", StringComparison.Ordinal)) path = part.Substring(5);
                url = url.Substring(0, query);
            }

            if (!explicitGit && !LooksLikeGit(url)) return false;
            source = new GitSource(url, path, revision);
            return true;
        }

        // Giống cách Unity nhận diện: URL kết thúc bằng .git, hoặc dùng giao thức git/ssh.
        static bool LooksLikeGit(string url) =>
            url.EndsWith(".git", StringComparison.OrdinalIgnoreCase)
            || url.StartsWith("git@", StringComparison.Ordinal)
            || url.StartsWith("ssh://", StringComparison.Ordinal)
            || url.StartsWith("git://", StringComparison.Ordinal);

        public override string ToString()
        {
            string text = Url;
            if (Path != null) text += "?path=" + Path;
            if (Revision != null) text += "#" + Revision;
            return text;
        }
    }
}
