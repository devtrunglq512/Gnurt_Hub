using Gnurt.Editor;
using NUnit.Framework;

namespace Gnurt.Tests
{
    public class GitSourceTests
    {
        [TestCase("https://github.com/a/b.git?path=/Packages/com.x#hub/v1.0.0", "https://github.com/a/b.git", "/Packages/com.x", "hub/v1.0.0")]
        [TestCase("git+https://example.com/a/b?path=/P#main", "https://example.com/a/b", "/P", "main")]
        [TestCase("git@github.com:a/b.git", "git@github.com:a/b.git", null, null)]
        public void Parses_git_urls(string text, string url, string path, string revision)
        {
            Assert.IsTrue(GitSource.TryParse(text, out var source));
            Assert.AreEqual(url, source.Url);
            Assert.AreEqual(path, source.Path);
            Assert.AreEqual(revision, source.Revision);
            Assert.AreEqual(text.Replace("git+", ""), source.ToString());
        }

        [TestCase("1.2.0")]
        [TestCase("file:../com.x")]
        [TestCase("https://example.com/page")]
        public void Rejects_non_git_text(string text)
        {
            Assert.IsFalse(GitSource.TryParse(text, out _));
        }

        [Test]
        public void WithRevision_keeps_url_and_path()
        {
            GitSource.TryParse("https://github.com/a/b.git?path=/P#v1.0.0", out var source);
            Assert.AreEqual("https://github.com/a/b.git?path=/P#v2.0.0", source.WithRevision("v2.0.0").ToString());
        }
    }

    public class GnurtPackageTests
    {
        [TestCase("com.gnurt.core", true)]
        [TestCase("com.gnurt.hub", true)]
        [TestCase("com.unity.ugui", false)]
        [TestCase("com.gnurtx.fake", false)]
        [TestCase(null, false)]
        public void Recognises_gnurt_packages_by_name(string name, bool expected)
        {
            Assert.AreEqual(expected, PackageOps.IsGnurtPackage(name));
        }
    }

    public class VersionTagsTests
    {
        [Test]
        public void Keeps_only_prefixed_versions_newest_first()
        {
            var tags = new[] { "hub/v1.0.0", "core/v2.0.0", "hub/v1.10.0", "hub/v1.2.0", "hub/v1.2.0-preview.1", "main", "hub/latest" };

            var result = VersionTags.FilterAndSort(tags, "hub/");

            CollectionAssert.AreEqual(new[] { "hub/v1.10.0", "hub/v1.2.0", "hub/v1.2.0-preview.1", "hub/v1.0.0" }, result);
        }

        [Test]
        public void Empty_prefix_ignores_prefixed_tags()
        {
            var result = VersionTags.FilterAndSort(new[] { "v1.0.0", "hub/v2.0.0", "0.9.0" });
            CollectionAssert.AreEqual(new[] { "v1.0.0", "0.9.0" }, result);
        }

        [Test]
        public void Parses_and_compares_versions()
        {
            Assert.IsTrue(PackageVersion.TryParse("v1.2.3-beta", out var beta));
            Assert.AreEqual("1.2.3-beta", beta.ToString());
            Assert.IsTrue(PackageVersion.TryParse("1.2.3", out var release));
            Assert.Less(beta.CompareTo(release), 0); // bản beta cũ hơn bản chính
            Assert.IsFalse(PackageVersion.TryParse("1.2", out _));
        }
    }

    public class LocTests
    {
        [Test]
        public void Text_follows_current_language()
        {
            var previous = Loc.Current;
            try
            {
                Loc.Current = Language.English;
                Assert.AreEqual("Install", Loc.T("Install", "Cài đặt"));

                Loc.Current = Language.Vietnamese;
                Assert.AreEqual("Cài đặt", new LocText("Install", "Cài đặt").ToString());
            }
            finally
            {
                Loc.Current = previous; // trả lại ngôn ngữ người dùng đang chọn
            }
        }
    }
}
