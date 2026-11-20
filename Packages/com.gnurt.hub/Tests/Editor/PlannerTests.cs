using System.Collections.Generic;
using System.Linq;
using NUnit.Framework;

namespace Gnurt.Hub.Tests
{
    public class PlannerTests
    {
        // Catalog giả: app cần lib, lib cần core; loop-a và loop-b cần nhau; soon chưa phát hành.
        static HubCatalog MakeCatalog()
        {
            CatalogEntry Entry(string name, bool planned = false, params string[] deps) => new CatalogEntry
            {
                name = name, displayName = name, repo = "https://github.com/x/" + name + ".git", path = "/Packages/" + name,
                tagPrefix = "", dependencies = deps, planned = planned,
            };

            return new HubCatalog
            {
                packages = new[]
                {
                    Entry("core"),
                    Entry("lib", false, "core"),
                    Entry("app", false, "lib"),
                    Entry("loop-a", false, "loop-b"),
                    Entry("loop-b", false, "loop-a"),
                    Entry("soon", true),
                    Entry("needs-soon", false, "soon"),
                    Entry("needs-ghost", false, "ghost"),
                },
            };
        }

        static List<string> Plan(string name, params string[] installed) =>
            HubPlanner.PlanInstall(MakeCatalog(), name, new HashSet<string>(installed), out _)?.Select(e => e.name).ToList();

        [Test]
        public void Dependencies_come_first()
        {
            CollectionAssert.AreEqual(new[] { "core", "lib", "app" }, Plan("app"));
        }

        [Test]
        public void Installed_dependencies_are_kept()
        {
            CollectionAssert.AreEqual(new[] { "lib", "app" }, Plan("app", "core"));
        }

        [TestCase("loop-a")]
        [TestCase("needs-soon")]
        [TestCase("needs-ghost")]
        [TestCase("soon")]
        public void Impossible_plans_return_an_error(string name)
        {
            var plan = HubPlanner.PlanInstall(MakeCatalog(), name, new HashSet<string>(), out string error);
            Assert.IsNull(plan);
            Assert.IsNotEmpty(error);
        }

        [Test]
        public void Dependents_block_removal()
        {
            var catalog = MakeCatalog();
            CollectionAssert.AreEqual(new[] { "lib" }, HubPlanner.InstalledDependents(catalog, "core", new HashSet<string> { "core", "lib" }));
            CollectionAssert.IsEmpty(HubPlanner.InstalledDependents(catalog, "core", new HashSet<string> { "core" }));
        }

        [TestCase("core/v1.2.0", "core/", "1.1.0", true)]
        [TestCase("core/v1.2.0", "core/", "1.2.0", false)]
        [TestCase("core/v1.2.0", "core/", "1.3.0", false)]
        [TestCase(null, "core/", "1.0.0", false)]
        [TestCase("v2.0.0", "", "1.9.9", true)]
        public void Detects_updates(string latestTag, string prefix, string installed, bool expected)
        {
            Assert.AreEqual(expected, HubPlanner.HasUpdate(latestTag, prefix, installed));
        }

        [TestCase("hub/v0.1.0", "hub/", true)]
        [TestCase("dev/0.1.0", "hub/", false)]
        [TestCase("main", "hub/", false)]
        [TestCase("8b7e80f", "hub/", false)]
        public void Tells_tags_from_branches(string revision, string prefix, bool expected)
        {
            Assert.AreEqual(expected, HubPlanner.IsVersionTag(revision, prefix));
        }

        [Test]
        public void Builds_git_identifier()
        {
            var entry = MakeCatalog().Find("lib");
            Assert.AreEqual("https://github.com/x/lib.git?path=/Packages/lib#dev/0.1.0", HubPlanner.Identifier(entry, "dev/0.1.0"));
        }

        [TestCase("https://github.com/devtrunglq512/Gnurt_Hub.git", "Gnurt_Hub")]
        [TestCase("https://github.com/devtrunglq512/Gnurt_Hub", "Gnurt_Hub")]
        [TestCase("git@github.com:devtrunglq512/Gnurt_LogicMap.git", "Gnurt_LogicMap")]
        public void Reads_repo_name(string url, string expected)
        {
            Assert.AreEqual(expected, HubPlanner.RepoName(url));
        }

        [Test]
        public void Local_folder_sits_inside_the_repos_root()
        {
            var entry = new CatalogEntry { repo = "https://github.com/devtrunglq512/Gnurt_Hub.git", path = "/Packages/com.gnurt.core" };
            Assert.AreEqual("D:/UnityProject/Gnurt_Hub/Packages/com.gnurt.core", HubPlanner.LocalFolder("D:/UnityProject", entry));
        }

        [Test]
        public void Local_identifier_is_relative_on_the_same_drive()
        {
            string id = HubPlanner.LocalIdentifier("D:/UnityProject/Gnurt_01", "D:/UnityProject/Gnurt_Hub/Packages/com.gnurt.core");
            Assert.AreEqual("file:../../Gnurt_Hub/Packages/com.gnurt.core", id);
        }

        [Test]
        public void Local_identifier_is_absolute_on_another_drive()
        {
            string id = HubPlanner.LocalIdentifier("C:/Temp/Project", "D:/UnityProject/Gnurt_Hub/Packages/com.gnurt.core");
            Assert.AreEqual("file:D:/UnityProject/Gnurt_Hub/Packages/com.gnurt.core", id);
        }
    }
}
