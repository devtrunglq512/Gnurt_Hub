using System.Collections.Generic;
using Gnurt.Editor;
using UnityEditor;
using UnityEngine.UIElements;

namespace Gnurt.Hub
{
    /// <summary>Window chính của Hub. Mở bằng menu Gnurt > Hub.</summary>
    public sealed class HubWindow : GnurtWindow
    {
        bool _refreshQueued;

        [MenuItem("Gnurt/Hub", priority = 0)]
        public static void OpenHub() => Open<HubWindow>();

        protected override string HeaderTitle => "Gnurt Hub";
        protected override string HeaderSubtitle => Loc.T("Install and update Gnurt packages", "Cài và cập nhật các package Gnurt");
        protected override string Version => HubInfo.Version;

        protected override IEnumerable<GnurtPage> CreatePages()
        {
            yield return new OverviewPage();
            yield return new PackagesPage();
            yield return new SettingsPage();
            yield return new AboutPage();
        }

        protected override void AddHeaderControls(VisualElement controls)
        {
            var hub = HubInfo.Package;
            if (hub != null) controls.Add(GnurtUI.Badge(hub.source.ToString()));

            controls.Add(HubSettings.UseDevChannel
                ? GnurtUI.Badge("Dev · " + HubSettings.DevBranch, BadgeKind.Warning)
                : GnurtUI.Badge(Loc.T("Release", "Phát hành"), BadgeKind.Success));

            var refresh = GnurtUI.Button(Loc.T("Refresh", "Làm mới"), () => _ = HubService.Instance.RefreshAsync());
            refresh.SetEnabled(!HubService.Instance.IsLoading);
            controls.Add(refresh);
        }

        protected override void OnEnable()
        {
            base.OnEnable();
            HubService.Instance.Changed += QueueRefresh;
            HubService.Instance.EnsureLoaded();
        }

        protected override void OnDisable()
        {
            HubService.Instance.Changed -= QueueRefresh;
            base.OnDisable();
        }

        // Gom nhiều lần thay đổi liên tiếp thành một lần vẽ lại.
        void QueueRefresh()
        {
            if (_refreshQueued) return;
            _refreshQueued = true;
            EditorApplication.delayCall += () =>
            {
                _refreshQueued = false;
                if (this != null) RefreshAll(); // header cũng đổi (nút Làm mới, kênh), nên vẽ lại cả window
            };
        }
    }
}
