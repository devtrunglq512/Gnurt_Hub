using System;
using System.Collections.Generic;
using System.Linq;
using Gnurt;
using Gnurt.Editor;
using UnityEditor;
using UnityEditor.PackageManager;
using UnityEngine;
using UnityEngine.UIElements;
using PackageInfo = UnityEditor.PackageManager.PackageInfo;

// Window thử nghiệm của project dev, không phát hành. Mở bằng menu: Gnurt > Core Demo (dev).
sealed class CoreDemoWindow : GnurtWindow
{
    [MenuItem("Gnurt/Core Demo (dev)", priority = 1000)]
    static void OpenWindow() => Open<CoreDemoWindow>();

    protected override string HeaderTitle => "Gnurt Core";
    protected override string HeaderSubtitle => Loc.T("Shared core  ·  demo window", "Phần core dùng chung  ·  window thử nghiệm");
    protected override string Version => PackageOps.VersionOf(typeof(GnurtWindow));

    protected override void AddHeaderControls(VisualElement controls)
    {
        var core = PackageOps.InfoOf(typeof(GnurtWindow));
        if (core != null) controls.Add(GnurtUI.Badge(core.source.ToString()));
    }

    protected override IEnumerable<GnurtPage> CreatePages()
    {
        yield return new OverviewPage();
        yield return new LogPage();
        yield return new InstalledPage();
        yield return new TagsPage();
    }

    static string GeneralGroup => Loc.T("General", "Chung");
    static string TryGroup => Loc.T("Try it", "Thử nghiệm");

    // Màu avatar theo nguồn cài của package.
    static Color SourceColor(PackageSource source)
    {
        switch (source)
        {
            case PackageSource.Registry: return new Color32(0x37, 0x8A, 0xDD, 0xFF);
            case PackageSource.Git: return new Color32(0xD8, 0x5A, 0x30, 0xFF);
            case PackageSource.Embedded:
            case PackageSource.Local:
            case PackageSource.LocalTarball: return new Color32(0x1D, 0x9E, 0x75, 0xFF);
            default: return new Color32(0x6E, 0x6D, 0x68, 0xFF);
        }
    }

    // "Visual Studio Editor" → "VS"
    static string Initials(string name)
    {
        var words = name.Split(new[] { ' ', '-', '.' }, StringSplitOptions.RemoveEmptyEntries);
        return string.Concat(words.Take(2).Select(w => char.ToUpperInvariant(w[0])));
    }

    sealed class OverviewPage : GnurtPage
    {
        public override string Id => "overview";
        public override string Title => Loc.T("Overview", "Tổng quan");
        public override string Subtitle => Loc.T("What the shared core provides.", "Những gì phần core dùng chung cung cấp.");
        public override string Group => GeneralGroup;
        public override GnurtIconKind Icon => GnurtIconKind.Home;

        public override void Build(VisualElement root)
        {
            root.Add(GnurtUI.Stats(
                GnurtUI.Stat(Loc.T("CORE VERSION", "VERSION CORE"), PackageOps.VersionOf(typeof(GnurtWindow)) ?? "dev", "com.gnurt.core"),
                GnurtUI.Stat("UNITY", Application.unityVersion, Loc.T("supports 2022.3+", "hỗ trợ 2022.3 trở lên")),
                GnurtUI.Stat(Loc.T("PLATFORM", "NỀN TẢNG"), GnurtUI.PlatformName(EditorUserBuildSettings.activeBuildTarget), Loc.T("active target", "đang chọn"))));

            var runtime = GnurtUI.Card("Runtime", Loc.T("Usable in game code.", "Dùng được trong code game."));
            runtime.Add(GnurtUI.Bullet(Loc.T("GLog: logs with tag and level", "GLog: log có tag và level")));
            runtime.Add(GnurtUI.Bullet(Loc.T("EventBus, Observable: the Observer pattern", "EventBus, Observable: pattern Observer")));
            runtime.Add(GnurtUI.Bullet(Loc.T("Services, GnurtService: service registry and lifecycle", "Services, GnurtService: đăng ký service và vòng đời")));
            runtime.Add(GnurtUI.Bullet(Loc.T("Singleton, PersistentMonoSingleton", "Singleton, PersistentMonoSingleton")));
            runtime.Add(GnurtUI.Bullet(Loc.T("MainThread: back to the main thread", "MainThread: đưa code về main thread")));

            var editor = GnurtUI.Card("Editor", Loc.T("For Gnurt tool windows.", "Cho các window tool của Gnurt."));
            editor.Add(GnurtUI.Bullet(Loc.T("GnurtWindow, GnurtPage, GnurtUI: this window's layout", "GnurtWindow, GnurtPage, GnurtUI: khung của window này")));
            editor.Add(GnurtUI.Bullet(Loc.T("Loc: English / Vietnamese text", "Loc: chữ tiếng Anh / tiếng Việt")));
            editor.Add(GnurtUI.Bullet(Loc.T("PackageOps: install, remove, list versions", "PackageOps: cài, gỡ, liệt kê version")));
            editor.Add(GnurtUI.Row(
                GnurtUI.Button(Loc.T("Installed packages", "Package đã cài"), () => Window.SelectPage("installed"), primary: true),
                GnurtUI.Button(Loc.T("Version tags", "Tag version"), () => Window.SelectPage("tags"))));

            root.Add(GnurtUI.Columns(runtime, editor));
        }
    }

    // Thử GLog và EventBus.
    sealed class LogPage : GnurtPage
    {
        static readonly GLogger Log = GLog.Tag("Demo");
        IDisposable _subscription;

        public override string Id => "log";
        public override string Title => Loc.T("Log & events", "Log và sự kiện");
        public override string Subtitle => Loc.T("Write logs and publish events to see them work.", "Ghi log và bắn sự kiện để xem chúng chạy.");
        public override string Group => TryGroup;
        public override GnurtIconKind Icon => GnurtIconKind.Log;

        public override void Build(VisualElement root)
        {
            var log = GnurtUI.Card("GLog", Loc.T("Each button writes one line to the Console, tagged [Gnurt][Demo].",
                "Mỗi nút ghi một dòng ra Console, có tag [Gnurt][Demo]."));
            log.Add(GnurtUI.Row(
                GnurtUI.Button("Info", () => Log.Info("Xin chào từ GLog")),
                GnurtUI.Button("Warning", () => Log.Warning("Cảnh báo thử")),
                GnurtUI.Button("Error", () => Log.Error("Lỗi thử"))));

            var counter = GnurtUI.Badge(Loc.T("0 received", "Đã nhận 0"), BadgeKind.Accent);
            int received = 0;
            _subscription = EventBus.Subscribe<DemoClicked>(_ =>
            {
                received++;
                counter.text = Loc.T($"{received} received", $"Đã nhận {received}");
            });

            var events = GnurtUI.Card("EventBus", Loc.T("The button publishes an event; this page listens and counts.",
                "Nút bắn một sự kiện; trang này nghe và đếm."));
            events.Add(GnurtUI.Row(GnurtUI.Button(Loc.T("Publish event", "Bắn sự kiện"), () => EventBus.Publish(new DemoClicked()), primary: true), counter));

            root.Add(GnurtUI.Columns(log, events));
        }

        public override void OnHide() => _subscription?.Dispose(); // rời trang thì thôi nghe
    }

    readonly struct DemoClicked { }

    // Package đang cài, chia thành của Gnurt và không thuộc Gnurt.
    sealed class InstalledPage : GnurtPage
    {
        public override string Id => "installed";
        public override string Title => Loc.T("Installed packages", "Package đã cài");
        public override string Subtitle => Loc.T("Packages in this project, Gnurt ones first.", "Các package trong project này, của Gnurt xếp trước.");
        public override string Group => TryGroup;
        public override GnurtIconKind Icon => GnurtIconKind.Package;

        public override async void Build(VisualElement root)
        {
            var status = GnurtUI.Hint(Loc.T("Loading…", "Đang tải…"));
            root.Add(status);

            IReadOnlyList<PackageInfo> packages;
            try
            {
                packages = await PackageOps.ListAsync();
            }
            catch (Exception e)
            {
                status.RemoveFromHierarchy();
                root.Add(GnurtUI.Error(e.Message));
                return;
            }
            status.RemoveFromHierarchy();

            var gnurt = packages.Where(p => PackageOps.IsGnurtPackage(p.name)).OrderBy(p => p.name).ToList();
            var modules = packages.Where(p => p.name.StartsWith("com.unity.modules.", StringComparison.Ordinal)).OrderBy(p => p.displayName).ToList();
            var others = packages.Except(gnurt).Except(modules).OrderBy(p => p.displayName).ToList();

            root.Add(GnurtUI.Stats(
                GnurtUI.Stat("GNURT", gnurt.Count.ToString(), Loc.T("Gnurt packages", "package của Gnurt")),
                GnurtUI.Stat(Loc.T("OTHER", "KHÁC"), others.Count.ToString(), Loc.T("Unity & third party", "Unity và bên thứ ba")),
                GnurtUI.Stat(Loc.T("UNITY MODULES", "MODULE UNITY"), modules.Count.ToString(), Loc.T("built in", "có sẵn trong Unity"))));

            var gnurtCard = GnurtUI.Card(Loc.T("Gnurt packages", "Package của Gnurt"), Loc.T("Name starts with com.gnurt.", "Tên bắt đầu bằng com.gnurt."));
            foreach (var p in gnurt)
                gnurtCard.Add(GnurtUI.ListRow(GnurtUI.Avatar(GnurtEditorAssets.Logo), p.displayName, p.name,
                    GnurtUI.Badge("v" + p.version, BadgeKind.Accent), GnurtUI.Badge(p.source.ToString())));
            if (gnurt.Count == 0) gnurtCard.Add(GnurtUI.Hint(Loc.T("No Gnurt package yet.", "Chưa có package nào của Gnurt.")));
            root.Add(gnurtCard);

            var otherCard = GnurtUI.Card(Loc.T("Not from Gnurt", "Không thuộc Gnurt"), Loc.T("Unity packages and third-party packages.", "Package của Unity và của bên thứ ba."));
            foreach (var p in others)
                otherCard.Add(GnurtUI.ListRow(GnurtUI.Avatar(Initials(p.displayName), SourceColor(p.source)), p.displayName, p.name,
                    GnurtUI.Badge(p.version), GnurtUI.Badge(p.source.ToString())));

            // Module có sẵn của Unity rất nhiều mà ít khi cần xem, nên gập lại.
            var foldout = new Foldout { text = Loc.T($"Built-in Unity modules ({modules.Count})", $"Module có sẵn của Unity ({modules.Count})"), value = false };
            foldout.AddToClassList("gnurt-foldout");
            foreach (var p in modules) foldout.Add(GnurtUI.ListRow(null, p.displayName, p.name));
            otherCard.Add(foldout);
            root.Add(otherCard);
        }
    }

    // Liệt kê tag version của một repo git, như Hub sẽ làm để biết có những version nào.
    sealed class TagsPage : GnurtPage
    {
        public override string Id => "tags";
        public override string Title => Loc.T("Version tags", "Tag version");
        public override string Subtitle => Loc.T(
            "Each release of a package is a git tag (core/v1.2.0). Hub reads this list to show the versions you can install or update to.",
            "Mỗi bản phát hành của package là một tag git (core/v1.2.0). Hub đọc danh sách này để hiện các version có thể cài hoặc update.");
        public override string Group => TryGroup;
        public override GnurtIconKind Icon => GnurtIconKind.Tag;

        public override void Build(VisualElement root)
        {
            var url = new TextField(Loc.T("Repo URL", "URL repo")) { value = "https://github.com/AppsFlyerSDK/appsflyer-unity-plugin.git" };
            var prefix = new TextField(Loc.T("Tag prefix", "Tiền tố tag")) { value = "" };
            url.AddToClassList("gnurt-field");
            prefix.AddToClassList("gnurt-field");

            var results = GnurtUI.Card();
            results.style.display = DisplayStyle.None;

            var button = GnurtUI.Button(Loc.T("List versions", "Liệt kê version"), null, primary: true);
            button.clicked += async () =>
            {
                results.style.display = DisplayStyle.Flex;
                results.Clear();
                results.Add(GnurtUI.Hint(Loc.T("Asking git…", "Đang hỏi git…")));
                button.SetEnabled(false);
                try
                {
                    var tags = await PackageOps.ListVersionTagsAsync(url.value, prefix.value);
                    results.Clear();
                    results.Add(GnurtUI.Text(Loc.T($"{tags.Count} versions, newest first", $"{tags.Count} version, mới nhất xếp trước"), "gnurt-card__title"));
                    for (int i = 0; i < tags.Count; i++)
                        results.Add(GnurtUI.ListRow(new GnurtIcon(GnurtIconKind.Tag), tags[i], null,
                            i == 0 ? GnurtUI.Badge(Loc.T("Latest", "Mới nhất"), BadgeKind.Success) : null));
                }
                catch (Exception e)
                {
                    results.Clear();
                    results.Add(GnurtUI.Error(e.Message));
                }
                finally
                {
                    button.SetEnabled(true);
                }
            };

            var form = GnurtUI.Card(Loc.T("Repository", "Repo"), Loc.T(
                "Prefix keeps only one package's tags, e.g. \"hub/\" keeps hub/v1.0.0 and skips core/v1.0.0. Leave empty for v1.2.3 tags.",
                "Tiền tố để chỉ lấy tag của một package, ví dụ \"hub/\" lấy hub/v1.0.0 và bỏ core/v1.0.0. Để trống nếu tag dạng v1.2.3."));
            form.Add(url);
            form.Add(prefix);
            form.Add(GnurtUI.Row(button));

            root.Add(form);
            root.Add(results);
        }
    }
}
