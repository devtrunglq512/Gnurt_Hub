using NUnit.Framework;
using UnityEditor;
using UnityEngine;

namespace Gnurt.Hub.Tests
{
    public class CatalogTests
    {
        [Test]
        public void Built_in_catalog_is_valid()
        {
            var asset = AssetDatabase.LoadAssetAtPath<TextAsset>(CatalogLoader.BuiltInPath);
            Assert.IsNotNull(asset, "thiếu file catalog.json trong package");

            Assert.IsTrue(HubCatalog.TryParse(asset.text, out var catalog, out string error), error);
            var core = catalog.Find("com.gnurt.core");
            var hub = catalog.Find("com.gnurt.hub");
            Assert.IsNotNull(core);
            Assert.IsNotNull(hub);
            Assert.AreEqual("core/", core.tagPrefix);
            CollectionAssert.Contains(hub.dependencies, "com.gnurt.core");
        }

        [Test]
        public void Rejects_bad_json_and_missing_fields()
        {
            Assert.IsFalse(HubCatalog.TryParse("{ not json", out _, out _));
            Assert.IsFalse(HubCatalog.TryParse("{\"packages\":[]}", out _, out _));
            Assert.IsFalse(HubCatalog.TryParse("{\"packages\":[{\"repo\":\"x.git\"}]}", out _, out _), "thiếu tên");
            Assert.IsFalse(HubCatalog.TryParse("{\"packages\":[{\"name\":\"a\"}]}", out _, out _), "đã phát hành mà thiếu repo");
        }

        [Test]
        public void Planned_package_may_have_no_repo_and_gets_defaults()
        {
            Assert.IsTrue(HubCatalog.TryParse("{\"packages\":[{\"name\":\"a\",\"planned\":true}]}", out var catalog, out string error), error);
            var entry = catalog.Find("a");
            Assert.IsNotNull(entry.dependencies);
            Assert.AreEqual("", entry.tagPrefix);
            Assert.AreEqual("a", entry.Title); // không có displayName thì dùng tên
        }
    }
}
