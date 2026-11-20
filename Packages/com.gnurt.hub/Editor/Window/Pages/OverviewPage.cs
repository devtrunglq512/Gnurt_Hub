using System.Linq;
using Gnurt.Editor;
using UnityEngine.UIElements;

namespace Gnurt.Hub
{
    sealed class OverviewPage : GnurtPage
    {
        public override string Id => "overview";
        public override string Title => Loc.T("Overview", "Tổng quan");
        public override string Subtitle => Loc.T("Gnurt packages in this project.", "Các package Gnurt trong project này.");
        public override string Group => Loc.T("General", "Chung");
        public override GnurtIconKind Icon => GnurtIconKind.Home;

        static HubService Hub => HubService.Instance;

        public override void Build(VisualElement root)
        {
            HubUI.AddStatusNotes(root);
            if (Hub.Catalog == null) return;

            var available = Hub.States.Where(s => !s.Entry.planned).ToList();
            var installed = Hub.States.Where(s => s.IsInstalled).ToList();
            int updates = installed.Count(s => s.HasUpdate);

            root.Add(GnurtUI.Stats(
                GnurtUI.Stat(Loc.T("INSTALLED", "ĐÃ CÀI"), $"{installed.Count}/{available.Count}", Loc.T("Gnurt packages", "package Gnurt")),
                GnurtUI.Stat(Loc.T("UPDATES", "CÓ BẢN MỚI"), updates.ToString(), Loc.T("ready to update", "có thể cập nhật")),
                GnurtUI.Stat(Loc.T("CHANNEL", "KÊNH"), HubSettings.UseDevChannel ? "Dev" : Loc.T("Release", "Phát hành"),
                    HubSettings.UseDevChannel ? HubSettings.DevBranch : Loc.T("release tags", "tag phát hành")),
                GnurtUI.Stat("CATALOG", HubUI.SourceName(Hub.CatalogSource), Loc.T($"{Hub.Catalog.packages.Length} packages", $"{Hub.Catalog.packages.Length} package"))));

            var list = GnurtUI.Card(Loc.T("In this project", "Trong project này"), Loc.T("Gnurt packages currently installed.", "Các package Gnurt đang cài."));
            foreach (var state in installed)
            {
                var badge = state.HasUpdate
                    ? GnurtUI.Badge(Loc.T($"Update {state.LatestTag}", $"Có bản mới {state.LatestTag}"), BadgeKind.Success)
                    : GnurtUI.Badge(state.Installed.source.ToString());
                list.Add(GnurtUI.ListRow(HubUI.Avatar(state.Entry), state.Entry.Title, state.Entry.name,
                    GnurtUI.Badge("v" + state.Installed.version, BadgeKind.Accent), badge));
            }
            if (installed.Count == 0) list.Add(GnurtUI.Hint(Loc.T("Nothing installed yet.", "Chưa cài package nào.")));

            var actions = GnurtUI.Row(
                GnurtUI.Button(Loc.T("Browse packages", "Xem các package"), () => Window.SelectPage(PackagesPage.PageId), primary: true));
            bool canUpdate = updates > 0 || (HubSettings.UseDevChannel && installed.Any(s => s.IsGit));
            if (canUpdate)
            {
                var updateAll = GnurtUI.Button(Loc.T("Update all", "Cập nhật hết"), HubActions.UpdateAll);
                updateAll.SetEnabled(!Hub.IsBusy && !Hub.IsLoading);
                actions.Add(updateAll);
            }
            list.Add(actions);

            var guide = GnurtUI.Card(Loc.T("How it works", "Cách dùng"), Loc.T("Three steps, all inside this window.", "Ba bước, đều làm trong window này."));
            guide.Add(GnurtUI.Bullet(Loc.T("Open Packages and press Install on the package you need.", "Mở trang Package, bấm Cài ở package cần dùng.")));
            guide.Add(GnurtUI.Bullet(Loc.T("Hub also installs the packages it depends on, in one go.", "Hub tự cài luôn các package nó cần, trong một lần.")));
            guide.Add(GnurtUI.Bullet(Loc.T("When a newer version is out, press Update. Settings switches between release and dev versions.",
                "Có bản mới thì bấm Cập nhật. Trang Cài đặt cho chọn bản phát hành hay bản đang phát triển.")));

            root.Add(GnurtUI.Columns(list, guide));
        }
    }
}
