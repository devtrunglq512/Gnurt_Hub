using Gnurt.Hub.Bootstrap;
using NUnit.Framework;

namespace Gnurt.Hub.Tests
{
    public class BootstrapTests
    {
        const string Repo = "https://github.com/devtrunglq512/Gnurt_Hub.git";

        [Test]
        public void Hub_from_a_branch_gets_core_from_the_same_branch()
        {
            string id = CoreInstaller.CoreIdentifier($"com.gnurt.hub@{Repo}?path=/Packages/com.gnurt.hub#dev/0.1.0", "com.gnurt.hub");
            Assert.AreEqual($"{Repo}?path=/Packages/com.gnurt.core#dev/0.1.0", id);
        }

        [Test]
        public void Hub_from_a_release_tag_gets_the_required_core_tag()
        {
            string id = CoreInstaller.CoreIdentifier($"com.gnurt.hub@{Repo}?path=/Packages/com.gnurt.hub#hub/v0.3.0", "com.gnurt.hub");
            Assert.AreEqual($"{Repo}?path=/Packages/com.gnurt.core#core/v{CoreInstaller.RequiredCoreVersion}", id);
        }

        [Test]
        public void Hub_without_revision_gets_core_without_revision()
        {
            string id = CoreInstaller.CoreIdentifier($"com.gnurt.hub@git+https://example.com/a/b?path=/Packages/com.gnurt.hub", "com.gnurt.hub");
            Assert.AreEqual("https://example.com/a/b?path=/Packages/com.gnurt.core", id);
        }

        [TestCase("com.gnurt.hub@file:../../Gnurt_Hub/Packages/com.gnurt.hub")]
        [TestCase("com.gnurt.hub@0.1.0")]
        [TestCase(null)]
        public void Hub_not_from_git_gets_nothing(string packageId)
        {
            Assert.IsNull(CoreInstaller.CoreIdentifier(packageId, "com.gnurt.hub"));
        }

        [TestCase("0.0.9", "0.1.0", true)]
        [TestCase("0.1.0", "0.1.0", false)]
        [TestCase("0.2.0-preview.1", "0.1.0", false)]
        [TestCase("1.0.0", "0.9.9", false)]
        public void Compares_versions(string version, string required, bool older)
        {
            Assert.AreEqual(older, CoreInstaller.IsOlder(version, required));
        }
    }
}
