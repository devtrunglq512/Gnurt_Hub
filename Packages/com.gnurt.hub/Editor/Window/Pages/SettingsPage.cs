using Gnurt.Editor;
using UnityEditor;
using UnityEngine.UIElements;

namespace Gnurt.Hub
{
    sealed class SettingsPage : GnurtPage
    {
        public override string Id => "settings";
        public override string Title => Loc.T("Settings", "Cài đặt");
        public override string Subtitle => Loc.T("Saved for you in this project only (not committed to git).",
            "Chỉ lưu cho bạn trong project này (không commit lên git).");
        public override string Group => Loc.T("Other", "Khác");
        public override GnurtIconKind Icon => GnurtIconKind.Settings;

        static HubService Hub => HubService.Instance;

        public override void Build(VisualElement root)
        {
            root.Add(GnurtUI.Columns(ChannelCard(), CatalogCard()));
            root.Add(LocalCard());
        }

        VisualElement ChannelCard()
        {
            var card = GnurtUI.Card(Loc.T("Update channel", "Kênh cập nhật"), Loc.T(
                "Release installs tagged versions (core/v1.2.0). Dev installs the latest commit of a branch, for testing work in progress.",
                "Phát hành cài các bản đã gắn tag (core/v1.2.0). Dev cài commit mới nhất của một nhánh, để thử bản đang làm."));

            bool dev = HubSettings.UseDevChannel;
            card.Add(GnurtUI.Row(
                GnurtUI.Button(Loc.T("Release", "Phát hành"), () => SetChannel(false), primary: !dev),
                GnurtUI.Button("Dev", () => SetChannel(true), primary: dev)));

            var branch = new TextField(Loc.T("Dev branch", "Nhánh dev")) { value = HubSettings.DevBranch };
            branch.AddToClassList("gnurt-field");
            branch.style.marginTop = 10;
            card.Add(branch);
            card.Add(GnurtUI.Row(GnurtUI.Button(Loc.T("Save branch", "Lưu nhánh"), () =>
            {
                HubSettings.DevBranch = branch.value;
                Window.RefreshAll();
            })));
            return card;
        }

        void SetChannel(bool dev)
        {
            HubSettings.UseDevChannel = dev;
            Window.RefreshAll(); // header và nút của các package đổi theo kênh
        }

        VisualElement CatalogCard()
        {
            var card = GnurtUI.Card("Catalog", Loc.T("The list of Gnurt packages Hub knows about.", "Danh sách package Gnurt mà Hub biết."));
            if (Hub.Catalog != null)
                card.Add(GnurtUI.KeyValue(Loc.T("Source", "Nguồn"), HubUI.SourceName(Hub.CatalogSource)));
            card.Add(GnurtUI.KeyValue("URL", HubInfo.CatalogUrl));

            var url = new TextField(Loc.T("Custom URL", "URL tự đặt")) { value = HubSettings.CatalogUrlOverride ?? "" };
            url.AddToClassList("gnurt-field");
            url.style.marginTop = 10;
            card.Add(url);
            card.Add(GnurtUI.Row(
                GnurtUI.Button(Loc.T("Save and reload", "Lưu và tải lại"), () =>
                {
                    HubSettings.CatalogUrlOverride = url.value.Trim();
                    _ = Hub.RefreshAsync();
                }, primary: true),
                GnurtUI.Button(Loc.T("Use default", "Dùng mặc định"), () =>
                {
                    HubSettings.CatalogUrlOverride = null;
                    _ = Hub.RefreshAsync();
                })));
            return card;
        }

        VisualElement LocalCard()
        {
            var card = GnurtUI.Card(Loc.T("Local copies", "Bản local"), Loc.T(
                "Folder that holds your Gnurt repos side by side (Gnurt_Hub, Gnurt_LogicMap…). When a package's repo is found here, its card shows \"Use local copy\".",
                "Thư mục chứa các repo Gnurt nằm cạnh nhau (Gnurt_Hub, Gnurt_LogicMap…). Repo của package nào có ở đây thì thẻ của nó có nút \"Dùng bản local\"."));

            var folder = new TextField(Loc.T("Repos folder", "Thư mục repo")) { value = HubSettings.ReposRoot };
            folder.AddToClassList("gnurt-field");
            card.Add(folder);
            card.Add(GnurtUI.Row(
                GnurtUI.Button(Loc.T("Save", "Lưu"), () => SaveRoot(folder.value), primary: true),
                GnurtUI.Button(Loc.T("Choose folder…", "Chọn thư mục…"), () =>
                {
                    string picked = EditorUtility.OpenFolderPanel(Loc.T("Repos folder", "Thư mục repo"), HubSettings.ReposRoot, "");
                    if (!string.IsNullOrEmpty(picked)) SaveRoot(picked);
                }),
                GnurtUI.Button(Loc.T("Use default", "Dùng mặc định"), () => SaveRoot(null))));
            card.Add(GnurtUI.Hint(Loc.T($"Default: {HubSettings.DefaultReposRoot}", $"Mặc định: {HubSettings.DefaultReposRoot}")));
            return card;
        }

        void SaveRoot(string path)
        {
            HubSettings.ReposRoot = path;
            Window.RefreshAll();
        }
    }
}
