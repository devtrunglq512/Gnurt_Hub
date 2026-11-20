using System.Linq;
using Gnurt.Editor;
using UnityEngine.UIElements;

namespace Gnurt.Hub
{
    sealed class PackagesPage : GnurtPage
    {
        public const string PageId = "packages";

        public override string Id => PageId;
        public override string Title => Loc.T("Packages", "Package");
        public override string Subtitle => Loc.T("Every package in the Gnurt catalog. Installed ones come first.",
            "Mọi package trong catalog của Gnurt. Package đã cài xếp trước.");
        public override string Group => Loc.T("General", "Chung");
        public override GnurtIconKind Icon => GnurtIconKind.Package;

        public override void Build(VisualElement root)
        {
            HubUI.AddStatusNotes(root);
            var hub = HubService.Instance;
            if (hub.Catalog == null) return;

            // Đã cài trước, rồi tới cài được, cuối cùng là sắp có. Trong mỗi nhóm giữ thứ tự của catalog.
            var ordered = hub.States
                .Select((state, index) => (state, index))
                .OrderBy(x => x.state.IsInstalled ? 0 : x.state.Entry.planned ? 2 : 1)
                .ThenBy(x => x.index)
                .Select(x => x.state);

            foreach (var state in ordered)
                root.Add(HubUI.PackageCard(state));
        }
    }
}
