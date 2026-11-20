using System;
using UnityEditor;
using UnityEngine;
using UnityEngine.UIElements;

namespace Gnurt.Editor
{
    public enum BadgeKind
    {
        Neutral,
        Success,
        Warning,
        Accent,
        Danger,
    }

    /// <summary>Các khối giao diện dùng chung, đã có style sẵn trong GnurtWindow.uss.</summary>
    public static class GnurtUI
    {
        /// <summary>Một khối chứa có class và các phần tử con.</summary>
        public static VisualElement Element(string className, params VisualElement[] children)
        {
            var element = new VisualElement();
            element.AddToClassList(className);
            foreach (var child in children)
                if (child != null) element.Add(child);
            return element;
        }

        /// <summary>Một dòng chữ. Mặc định là chữ thường, tự xuống dòng.</summary>
        public static Label Text(string text, string className = "gnurt-text")
        {
            var label = new Label(text);
            label.AddToClassList(className);
            return label;
        }

        /// <summary>Chữ phụ, màu nhạt.</summary>
        public static Label Hint(string text) => Text(text, "gnurt-hint");

        /// <summary>Chữ báo lỗi, màu đỏ.</summary>
        public static Label Error(string text) => Text(text, "gnurt-error");

        /// <summary>Tiêu đề một phần nội dung trong trang.</summary>
        public static Label SectionTitle(string text) => Text(text, "gnurt-section-title");

        /// <summary>Tên trang và dòng mô tả. Window tự vẽ cho mỗi trang.</summary>
        public static VisualElement PageHeader(string title, string subtitle)
        {
            var header = Element("gnurt-page__header", Text(title, "gnurt-page__title"));
            if (!string.IsNullOrEmpty(subtitle)) header.Add(Text(subtitle, "gnurt-page__subtitle"));
            return header;
        }

        /// <summary>Khung có viền để gom nhóm, kèm tiêu đề và mô tả (có thể bỏ trống).</summary>
        public static VisualElement Card(string title = null, string subtitle = null)
        {
            var card = Element("gnurt-card");
            if (!string.IsNullOrEmpty(title)) card.Add(Text(title, "gnurt-card__title"));
            if (!string.IsNullOrEmpty(subtitle)) card.Add(Text(subtitle, "gnurt-card__subtitle"));
            return card;
        }

        /// <summary>Nhãn nhỏ bo tròn, màu theo loại.</summary>
        public static Label Badge(string text, BadgeKind kind = BadgeKind.Neutral)
        {
            var badge = Text(text, "gnurt-badge");
            badge.AddToClassList("gnurt-badge--" + kind.ToString().ToLowerInvariant());
            return badge;
        }

        /// <summary>Hàng các ô số liệu, tự xuống dòng khi window hẹp.</summary>
        public static VisualElement Stats(params VisualElement[] stats) => Element("gnurt-stats", stats);

        /// <summary>Một ô số liệu: nhãn nhỏ, con số lớn, ghi chú.</summary>
        public static VisualElement Stat(string label, string value, string hint = null)
        {
            var valueLabel = Text(value, "gnurt-stat__value");
            valueLabel.tooltip = value; // chữ dài bị cắt "…" thì rê chuột vào để xem đủ

            var stat = Element("gnurt-stat", Text(label, "gnurt-stat__label"), valueLabel);
            if (!string.IsNullOrEmpty(hint)) stat.Add(Text(hint, "gnurt-stat__hint"));
            return stat;
        }

        /// <summary>Ô vuông nhỏ có chữ viết tắt trên nền màu.</summary>
        public static VisualElement Avatar(string letters, Color color)
        {
            var avatar = Element("gnurt-avatar", Text(letters, "gnurt-avatar__text"));
            avatar.style.backgroundColor = color;
            return avatar;
        }

        /// <summary>Ô vuông nhỏ có hình.</summary>
        public static VisualElement Avatar(Texture2D image)
        {
            var avatar = Element("gnurt-avatar");
            avatar.style.backgroundImage = image;
            return avatar;
        }

        /// <summary>Nút bấm. primary = nút chính, tô màu nhấn.</summary>
        public static Button Button(string text, Action onClick, bool primary = false)
        {
            var button = new UnityEngine.UIElements.Button(onClick) { text = text };
            button.AddToClassList("gnurt-button");
            if (primary) button.AddToClassList("gnurt-button--primary");
            return button;
        }

        /// <summary>Xếp các phần tử thành hàng ngang.</summary>
        public static VisualElement Row(params VisualElement[] children) => Element("gnurt-row", children);

        /// <summary>Chia cột; window hẹp thì các cột tự xuống dòng.</summary>
        public static VisualElement Columns(params VisualElement[] columns)
        {
            foreach (var column in columns) column.AddToClassList("gnurt-column");
            return Element("gnurt-columns", columns);
        }

        /// <summary>Một dòng trong danh sách: hình bên trái, tên và mô tả, các nhãn bên phải.</summary>
        public static VisualElement ListRow(VisualElement leading, string title, string subtitle = null, params VisualElement[] trailing)
        {
            var text = Element("gnurt-list-row__text", Text(title, "gnurt-list-row__title"));
            if (!string.IsNullOrEmpty(subtitle)) text.Add(Text(subtitle, "gnurt-list-row__subtitle"));

            var row = Element("gnurt-list-row", leading, text, Spacer());
            foreach (var item in trailing)
                if (item != null) row.Add(item); // cho phép truyền null khi nhãn chỉ hiện theo điều kiện
            return row;
        }

        /// <summary>Một dòng có chấm đầu dòng.</summary>
        public static VisualElement Bullet(string text) =>
            Element("gnurt-bullet", Text("•", "gnurt-bullet__dot"), Text(text, "gnurt-bullet__text"));

        /// <summary>Một cặp tên – giá trị. Giá trị bôi đen copy được.</summary>
        public static VisualElement KeyValue(string key, string value)
        {
            var valueLabel = Text(value, "gnurt-kv__value");
            valueLabel.selection.isSelectable = true;
            return Element("gnurt-kv", Text(key, "gnurt-kv__key"), valueLabel);
        }

        /// <summary>Khoảng trống co giãn, đẩy các phần tử sau nó sang phải.</summary>
        public static VisualElement Spacer() => Element("gnurt-spacer");

        /// <summary>Tên nền tảng ngắn gọn để hiện trên giao diện: StandaloneWindows64 → Windows.</summary>
        public static string PlatformName(BuildTarget target)
        {
            switch (target)
            {
                case BuildTarget.StandaloneWindows:
                case BuildTarget.StandaloneWindows64: return "Windows";
                case BuildTarget.StandaloneOSX: return "macOS";
                case BuildTarget.StandaloneLinux64: return "Linux";
                default: return target.ToString(); // Android, iOS, WebGL... vốn đã ngắn
            }
        }
    }
}
