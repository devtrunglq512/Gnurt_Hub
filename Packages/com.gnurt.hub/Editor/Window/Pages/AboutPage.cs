using Gnurt.Editor;
using UnityEngine;
using UnityEngine.UIElements;

namespace Gnurt.Hub
{
    sealed class AboutPage : GnurtPage
    {
        public override string Id => "about";
        public override string Title => Loc.T("About", "Giới thiệu");
        public override string Subtitle => Loc.T("Gnurt Hub and the packages it manages.", "Gnurt Hub và các package nó quản lý.");
        public override string Group => Loc.T("Other", "Khác");
        public override GnurtIconKind Icon => GnurtIconKind.Info;

        public override void Build(VisualElement root)
        {
            var logo = new VisualElement();
            logo.style.width = 96;
            logo.style.height = 96;
            logo.style.backgroundImage = GnurtEditorAssets.Logo;
            logo.style.marginBottom = 12;

            var info = GnurtUI.Card();
            info.Add(logo);
            info.Add(GnurtUI.KeyValue("Gnurt Hub", "v" + HubInfo.Version + (HubInfo.Revision != null ? $"  ({HubInfo.Revision})" : "")));
            info.Add(GnurtUI.KeyValue("Gnurt Core", "v" + (PackageOps.VersionOf(typeof(GnurtWindow)) ?? "dev")));
            info.Add(GnurtUI.KeyValue("Unity", Application.unityVersion));
            info.Add(GnurtUI.KeyValue("Repo", HubInfo.RepoWebUrl));
            info.Add(GnurtUI.Row(GnurtUI.Button(Loc.T("Open on GitHub", "Mở trên GitHub"), () => Application.OpenURL(HubInfo.RepoWebUrl), primary: true)));

            var meaning = GnurtUI.Card(Loc.T("The logo", "Logo"), Loc.T(
                "The G opens toward the star, the goal of doing things perfectly. The colors brighten toward the top right: always moving up.",
                "Chữ G mở về phía ngôi sao, đích đến là làm mọi thứ thật hoàn hảo. Màu sáng dần lên góc trên phải: luôn đi lên."));

            root.Add(GnurtUI.Columns(info, meaning));
        }
    }
}
