using System;
using System.Collections.Generic;
using System.Text.RegularExpressions;

namespace Gnurt.Editor
{
    /// <summary>
    /// Version dạng semver: <c>1.2.3</c> hoặc <c>1.2.3-preview.1</c> (chữ "v" ở đầu được bỏ qua).
    /// Bản có đuôi (preview) xếp trước bản chính cùng số.
    /// </summary>
    public readonly struct PackageVersion : IComparable<PackageVersion>
    {
        static readonly Regex Pattern = new Regex(@"^v?(\d+)\.(\d+)\.(\d+)(?:-([0-9A-Za-z.\-]+))?$");

        public readonly int Major;
        public readonly int Minor;
        public readonly int Patch;

        /// <summary>Phần sau dấu "-", null nếu là bản chính.</summary>
        public readonly string Prerelease;

        public PackageVersion(int major, int minor, int patch, string prerelease = null)
        {
            Major = major;
            Minor = minor;
            Patch = patch;
            Prerelease = string.IsNullOrEmpty(prerelease) ? null : prerelease;
        }

        public static bool TryParse(string text, out PackageVersion version)
        {
            version = default;
            var match = Pattern.Match(text ?? "");
            if (!match.Success) return false;
            if (!int.TryParse(match.Groups[1].Value, out int major)
                || !int.TryParse(match.Groups[2].Value, out int minor)
                || !int.TryParse(match.Groups[3].Value, out int patch))
                return false;

            version = new PackageVersion(major, minor, patch, match.Groups[4].Success ? match.Groups[4].Value : null);
            return true;
        }

        public int CompareTo(PackageVersion other)
        {
            if (Major != other.Major) return Major.CompareTo(other.Major);
            if (Minor != other.Minor) return Minor.CompareTo(other.Minor);
            if (Patch != other.Patch) return Patch.CompareTo(other.Patch);
            if (Prerelease == other.Prerelease) return 0;
            if (Prerelease == null) return 1;        // bản chính mới hơn bản preview
            if (other.Prerelease == null) return -1;
            return string.CompareOrdinal(Prerelease, other.Prerelease); // so sánh chuỗi đơn giản
        }

        public override string ToString() =>
            Prerelease == null ? $"{Major}.{Minor}.{Patch}" : $"{Major}.{Minor}.{Patch}-{Prerelease}";
    }

    /// <summary>Lọc tag version theo tiền tố và xếp mới nhất lên trước.</summary>
    public static class VersionTags
    {
        /// <summary>
        /// Giữ các tag dạng <c>{prefix}v1.2.3</c>, mới nhất trước.
        /// Ví dụ prefix "hub/" giữ "hub/v1.0.0", bỏ "core/v1.0.0" và "main".
        /// </summary>
        public static List<string> FilterAndSort(IEnumerable<string> tags, string prefix = "")
        {
            prefix ??= "";
            var found = new List<(string tag, PackageVersion version)>();
            foreach (var tag in tags)
            {
                if (tag == null || !tag.StartsWith(prefix, StringComparison.Ordinal)) continue;
                if (PackageVersion.TryParse(tag.Substring(prefix.Length), out var version))
                    found.Add((tag, version));
            }

            found.Sort((a, b) => b.version.CompareTo(a.version));
            return found.ConvertAll(item => item.tag);
        }
    }
}
