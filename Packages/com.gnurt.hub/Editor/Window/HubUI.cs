using System.Collections.Generic;
using System.Linq;
using Gnurt.Editor;
using UnityEditor.UIElements;
using UnityEngine;
using UnityEngine.UIElements;

namespace Gnurt.Hub
{
    /// <summary>Các khối giao diện riêng của Hub, dùng chung giữa các trang.</summary>
    static class HubUI
    {
        static HubService Hub => HubService.Instance;

        /// <summary>Thông báo đang tải hoặc lỗi, đặt ở đầu mỗi trang.</summary>
        public static void AddStatusNotes(VisualElement root)
        {
            if (Hub.IsLoading && Hub.Catalog == null)
                root.Add(GnurtUI.Note(Loc.T("Loading the catalog and package versions…", "Đang tải catalog và version của các package…"), BadgeKind.Accent));
            if (Hub.LastError != null)
                root.Add(GnurtUI.Note(Hub.LastError, BadgeKind.Danger));
            if (Hub.Catalog != null && Hub.CatalogError != null)
                root.Add(GnurtUI.Note(Loc.T($"Could not download the catalog ({Hub.CatalogError}). Using the {SourceName(Hub.CatalogSource)} copy.",
                    $"Không tải được catalog ({Hub.CatalogError}). Đang dùng bản {SourceName(Hub.CatalogSource)}."), BadgeKind.Warning));
            if (Hub.IsBusy)
                root.Add(GnurtUI.Note(Loc.T("Package Manager is working… Unity may recompile when it finishes.",
                    "Package Manager đang làm việc… Xong có thể Unity sẽ compile lại."), BadgeKind.Accent));
        }

        public static string SourceName(CatalogSource source)
        {
            switch (source)
            {
                case CatalogSource.Remote: return "GitHub";
                case CatalogSource.Cache: return Loc.T("saved", "đã lưu");
                default: return Loc.T("built-in", "có sẵn");
            }
        }

        /// <summary>Ô vuông nhỏ bên trái thẻ: logo cho package Gnurt.</summary>
        public static VisualElement Avatar(CatalogEntry entry) =>
            PackageOps.IsGnurtPackage(entry.name)
                ? GnurtUI.Avatar(GnurtEditorAssets.Logo)
                : GnurtUI.Avatar(entry.Title.Substring(0, 1), new Color32(0x6E, 0x6D, 0x68, 0xFF));

        /// <summary>Thẻ đầy đủ của một package: tên, badge, mô tả, trạng thái và các nút.</summary>
        public static VisualElement PackageCard(PackageState state)
        {
            var entry = state.Entry;
            var title = GnurtUI.Inline(GnurtUI.Text(entry.Title, "gnurt-card__title"));
            foreach (var badge in Badges(state)) title.Add(badge);

            var body = GnurtUI.Media(Avatar(entry), title, GnurtUI.Text(entry.Description, "gnurt-card__subtitle"));
            var card = GnurtUI.Element("gnurt-card", body);

            var details = Details(state);
            if (details != null) card.Add(details);

            var actions = Actions(state);
            if (actions.childCount > 0) card.Add(actions);
            return card;
        }

        static IEnumerable<VisualElement> Badges(PackageState state)
        {
            if (state.Entry.planned) yield return GnurtUI.Badge(Loc.T("Coming soon", "Sắp có"));
            if (state.Entry.licensed) yield return GnurtUI.Badge(Loc.T("Needs key", "Cần key"), BadgeKind.Warning);
            if (state.IsInstalled) yield return GnurtUI.Badge("v" + state.Installed.version, BadgeKind.Accent);
            if (state.IsEmbedded) yield return GnurtUI.Badge(Loc.T("Developed here", "Đang phát triển tại chỗ"));
            if (state.IsLocal) yield return GnurtUI.Badge("Local", BadgeKind.Warning);
            if (state.HasUpdate) yield return GnurtUI.Badge(Loc.T($"Update {state.LatestTag}", $"Có bản mới {state.LatestTag}"), BadgeKind.Success);
        }

        // Dòng chữ nhỏ: cài từ đâu, cần gì, lỗi tag.
        static VisualElement Details(PackageState state)
        {
            var box = new VisualElement();
            var entry = state.Entry;

            if (state.IsGit) box.Add(GnurtUI.Hint(Loc.T($"Installed from git · {state.Revision ?? "default branch"}", $"Cài từ git · {state.Revision ?? "nhánh mặc định"}")));
            if (state.IsLocal) box.Add(GnurtUI.Hint(Loc.T($"Using local folder · {state.Installed.resolvedPath}", $"Đang dùng thư mục local · {state.Installed.resolvedPath}")));
            if (entry.dependencies.Length > 0)
            {
                var names = entry.dependencies.Select(d => Hub.Catalog.Find(d)?.Title ?? d);
                box.Add(GnurtUI.Hint(Loc.T("Needs: ", "Cần: ") + string.Join(", ", names)));
            }
            if (!entry.planned && state.TagsError != null)
                box.Add(GnurtUI.Error(Loc.T("Could not read versions: ", "Không đọc được danh sách version: ") + state.TagsError));
            else if (!entry.planned && !state.IsInstalled && state.Tags.Count == 0 && !HubSettings.UseDevChannel)
                box.Add(GnurtUI.Hint(Loc.T("No release yet. Turn on the Dev channel in Settings to install the version in development.",
                    "Chưa có bản phát hành. Bật kênh Dev trong Cài đặt để cài bản đang phát triển.")));

            if (box.childCount == 0) return null;
            box.style.marginTop = 8;
            return box;
        }

        static VisualElement Actions(PackageState state)
        {
            var row = GnurtUI.Row();
            if (state.Entry.planned || state.IsEmbedded) return row; // chưa phát hành hoặc đang sửa tại chỗ: không có nút

            bool idle = !Hub.IsBusy && !Hub.IsLoading;
            string revision = Hub.RevisionFor(state);

            if (!state.IsInstalled)
            {
                var install = GnurtUI.Button(Loc.T("Install", "Cài"), () => HubActions.Install(state.Entry.name), primary: true);
                install.SetEnabled(idle && revision != null);
                row.Add(install);
            }
            else if (state.IsLocal)
            {
                row.Add(Enabled(GnurtUI.Button(Loc.T("Back to git", "Về bản git"), () => HubActions.UseGit(state), primary: true), idle && revision != null));
            }
            else
            {
                if (state.HasUpdate)
                    row.Add(Enabled(GnurtUI.Button(Loc.T($"Update to {state.LatestTag}", $"Cập nhật lên {state.LatestTag}"), () => HubActions.Update(state), primary: true), idle));
                else if (state.IsGit && HubSettings.UseDevChannel)
                    row.Add(Enabled(GnurtUI.Button(Loc.T("Get latest commit", "Lấy bản mới nhất"), () => HubActions.Update(state)), idle));

                if (state.IsGit) row.Add(Enabled(VersionMenu(state), idle));
                row.Add(Enabled(GnurtUI.Button(Loc.T("Remove", "Gỡ"), () => HubActions.Remove(state)), idle));
            }

            if (state.IsInstalled && !state.IsLocal && Hub.HasLocalFolder(state))
                row.Add(Enabled(GnurtUI.Button(Loc.T("Use local copy", "Dùng bản local"), () => HubActions.UseLocal(state)), idle));
            return row;
        }

        // Menu chọn version: các tag phát hành, và nhánh dev.
        static VisualElement VersionMenu(PackageState state)
        {
            var menu = new ToolbarMenu { text = Loc.T("Version", "Version") };
            menu.AddToClassList("gnurt-menu-button");

            foreach (string tag in state.Tags)
                menu.menu.AppendAction(MenuText(tag), _ => HubActions.SwitchVersion(state, tag), Status(state, tag));
            if (state.Tags.Count == 0)
                menu.menu.AppendAction(Loc.T("No release tags yet", "Chưa có tag phát hành"), null, DropdownMenuAction.Status.Disabled);

            menu.menu.AppendSeparator();
            string branch = HubSettings.DevBranch;
            menu.menu.AppendAction(MenuText(Loc.T($"Branch {branch} (dev)", $"Nhánh {branch} (dev)")),
                _ => HubActions.SwitchVersion(state, branch), Status(state, branch));
            return menu;
        }

        // Dấu "/" trong menu Unity tạo menu con, nên đổi sang ký tự gạch chéo trông giống hệt.
        static string MenuText(string text) => text.Replace("/", "∕");

        static DropdownMenuAction.Status Status(PackageState state, string revision) =>
            state.Revision == revision ? DropdownMenuAction.Status.Checked : DropdownMenuAction.Status.Normal;

        static T Enabled<T>(T element, bool enabled) where T : VisualElement
        {
            element.SetEnabled(enabled);
            return element;
        }
    }
}
