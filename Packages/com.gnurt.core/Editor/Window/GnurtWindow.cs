using System.Collections.Generic;
using UnityEditor;
using UnityEngine;
using UnityEngine.UIElements;

namespace Gnurt.Editor
{
    /// <summary>
    /// Khung window chung cho mọi tool Gnurt: header có logo, sidebar chia nhóm, vùng nội dung và footer.
    /// Lớp con chỉ cần khai tên và danh sách trang. Mở bằng <see cref="Open{T}"/> để có kích thước mặc định.
    /// </summary>
    public abstract class GnurtWindow : EditorWindow
    {
        [SerializeField] string _pageId; // nhớ trang đang mở qua các lần reload

        readonly Dictionary<string, VisualElement> _navItems = new Dictionary<string, VisualElement>();
        List<GnurtPage> _pages = new List<GnurtPage>();
        GnurtPage _current;
        ScrollView _content;

        /// <summary>Tên trên header và trên tab của window.</summary>
        protected abstract string HeaderTitle { get; }

        /// <summary>Dòng chữ nhỏ dưới tên. null thì ẩn.</summary>
        protected virtual string HeaderSubtitle => null;

        /// <summary>Version hiện trên header. null thì ẩn.</summary>
        protected virtual string Version => null;

        /// <summary>Chữ ở góc phải footer.</summary>
        protected virtual string FooterText => "Gnurt";

        /// <summary>Kích thước khi mở window lần đầu.</summary>
        protected virtual Vector2 DefaultSize => new Vector2(1040, 680);

        protected virtual Vector2 MinimumSize => new Vector2(760, 480);

        /// <summary>Các trang, theo thứ tự trên sidebar.</summary>
        protected abstract IEnumerable<GnurtPage> CreatePages();

        /// <summary>Thêm nút hoặc badge riêng vào header, nằm trước nút đổi ngôn ngữ.</summary>
        protected virtual void AddHeaderControls(VisualElement controls) { }

        /// <summary>
        /// Mở window, hoặc đưa lên trước nếu đang mở. Mở mới thì đặt kích thước mặc định, nằm giữa cửa sổ Unity.
        /// </summary>
        public static T Open<T>(string pageId = null) where T : GnurtWindow
        {
            bool alreadyOpen = HasOpenInstances<T>();
            var window = GetWindow<T>();
            window.minSize = window.MinimumSize;
            if (!alreadyOpen)
            {
                var main = EditorGUIUtility.GetMainWindowPosition();
                var size = window.DefaultSize;
                window.position = new Rect(main.center - size / 2, size);
            }
            if (pageId != null) window.SelectPage(pageId);
            return window;
        }

        protected virtual void OnEnable() => Loc.Changed += Rebuild;

        protected virtual void OnDisable()
        {
            Loc.Changed -= Rebuild;
            HideCurrent();
        }

        // Unity gọi hàm này khi cần dựng giao diện. Để protected để Unity tìm thấy từ lớp con.
        protected virtual void CreateGUI() => Rebuild();

        /// <summary>Vẽ lại cả window: header, sidebar và trang. Dùng khi đổi ngôn ngữ.</summary>
        public void Rebuild()
        {
            HideCurrent();
            titleContent = new GUIContent(HeaderTitle, GnurtEditorAssets.Logo);

            var root = rootVisualElement;
            root.Clear();
            _navItems.Clear();

            // Không xoá styleSheets có sẵn của root: Unity gắn theme Editor (font chữ) ở đó.
            var style = GnurtEditorAssets.WindowStyle;
            if (style != null && !root.styleSheets.Contains(style)) root.styleSheets.Add(style);
            root.AddToClassList("gnurt-root");
            root.EnableInClassList("gnurt--dark", EditorGUIUtility.isProSkin);
            root.EnableInClassList("gnurt--light", !EditorGUIUtility.isProSkin);

            _pages = new List<GnurtPage>(CreatePages());
            foreach (var page in _pages) page.Window = this;

            _content = new ScrollView(ScrollViewMode.Vertical);
            _content.AddToClassList("gnurt-content");

            root.Add(BuildHeader());
            root.Add(GnurtUI.Element("gnurt-accent-line"));
            root.Add(GnurtUI.Element("gnurt-body", BuildSidebar(), _content));
            root.Add(BuildFooter());

            SelectPage(_pageId);
        }

        /// <summary>Mở trang theo Id. Không có trang đó thì mở trang đầu tiên.</summary>
        public void SelectPage(string pageId)
        {
            if (_content == null)
            {
                _pageId = pageId; // giao diện chưa dựng xong: nhớ lại, dựng xong sẽ mở
                return;
            }

            var page = _pages.Find(p => p.Id == pageId) ?? (_pages.Count > 0 ? _pages[0] : null);
            if (page == null) return;

            HideCurrent();
            _current = page;
            _pageId = page.Id;
            foreach (var pair in _navItems)
                pair.Value.EnableInClassList("gnurt-nav-item--selected", pair.Key == page.Id);
            Render(keepScroll: false);
        }

        /// <summary>Vẽ lại cả window (header, sidebar, trang) nhưng giữ trang đang mở và vị trí cuộn.</summary>
        public void RefreshAll()
        {
            var scroll = _content?.scrollOffset ?? Vector2.zero;
            Rebuild();
            _content?.schedule.Execute(() => _content.scrollOffset = scroll);
        }

        /// <summary>Vẽ lại trang đang mở, giữ nguyên vị trí cuộn. Dùng khi dữ liệu của trang thay đổi.</summary>
        public void RefreshPage()
        {
            if (_current == null) return;
            _current.OnHide();
            Render(keepScroll: true);
        }

        void Render(bool keepScroll)
        {
            var scroll = _content.scrollOffset;
            _content.Clear();

            var page = GnurtUI.Element("gnurt-page", GnurtUI.PageHeader(_current.Title, _current.Subtitle));
            _current.Build(page);
            _content.Add(page);
            _current.OnShow();

            if (keepScroll) _content.schedule.Execute(() => _content.scrollOffset = scroll);
            else _content.scrollOffset = Vector2.zero;
        }

        void HideCurrent()
        {
            _current?.OnHide();
            _current = null;
        }

        VisualElement BuildHeader()
        {
            var logo = GnurtUI.Element("gnurt-header__logo");
            logo.style.backgroundImage = GnurtEditorAssets.Logo;

            var titles = GnurtUI.Element("gnurt-header__titles", GnurtUI.Text(HeaderTitle, "gnurt-header__title"));
            if (!string.IsNullOrEmpty(HeaderSubtitle))
                titles.Add(GnurtUI.Text(HeaderSubtitle, "gnurt-header__subtitle"));

            var controls = GnurtUI.Element("gnurt-header__controls");
            if (!string.IsNullOrEmpty(Version))
                controls.Add(GnurtUI.Badge("v" + Version, BadgeKind.Accent));
            AddHeaderControls(controls);
            controls.Add(BuildLanguageSwitch());

            return GnurtUI.Element("gnurt-header", logo, titles, GnurtUI.Spacer(), controls);
        }

        // Công tắc EN | VI, bên đang chọn được tô màu.
        static VisualElement BuildLanguageSwitch()
        {
            var group = GnurtUI.Element("gnurt-segmented");
            group.tooltip = Loc.T("Language", "Ngôn ngữ");
            group.Add(LanguageOption("EN", Language.English));
            group.Add(LanguageOption("VI", Language.Vietnamese));
            return group;
        }

        static VisualElement LanguageOption(string text, Language language)
        {
            var option = GnurtUI.Text(text, "gnurt-segmented__option");
            option.EnableInClassList("gnurt-segmented__option--selected", Loc.Current == language);
            option.RegisterCallback<ClickEvent>(_ => Loc.Current = language);
            return option;
        }

        VisualElement BuildSidebar()
        {
            var sidebar = GnurtUI.Element("gnurt-sidebar");
            string group = null;
            foreach (var page in _pages)
            {
                if (page.Group != group)
                {
                    group = page.Group;
                    if (group != null) sidebar.Add(GnurtUI.Text(group.ToUpperInvariant(), "gnurt-nav-group"));
                }

                var item = GnurtUI.Element("gnurt-nav-item", new GnurtIcon(page.Icon), GnurtUI.Text(page.Title, "gnurt-nav-item__label"));
                string id = page.Id;
                item.RegisterCallback<ClickEvent>(_ => SelectPage(id));
                _navItems[id] = item;
                sidebar.Add(item);
            }
            return sidebar;
        }

        VisualElement BuildFooter()
        {
            return GnurtUI.Element("gnurt-footer",
                GnurtUI.Text($"Unity {Application.unityVersion}  ·  {GnurtUI.PlatformName(EditorUserBuildSettings.activeBuildTarget)}", "gnurt-footer__text"),
                GnurtUI.Spacer(),
                GnurtUI.Text(FooterText, "gnurt-footer__text"));
        }
    }
}
