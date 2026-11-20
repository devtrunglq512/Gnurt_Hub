using UnityEngine.UIElements;

namespace Gnurt.Editor
{
    /// <summary>Một trang trong <see cref="GnurtWindow"/>: một mục trên sidebar và nội dung của nó.</summary>
    public abstract class GnurtPage
    {
        /// <summary>Mã cố định của trang, dùng để nhớ trang đang mở và để mở trang từ menu.</summary>
        public abstract string Id { get; }

        /// <summary>Tên trang theo ngôn ngữ đang dùng (đọc lại mỗi lần vẽ).</summary>
        public abstract string Title { get; }

        /// <summary>Dòng mô tả dưới tên trang. null thì ẩn.</summary>
        public virtual string Subtitle => null;

        /// <summary>Tên nhóm trên sidebar. Các trang liền nhau cùng nhóm nằm dưới một tiêu đề nhóm.</summary>
        public virtual string Group => null;

        /// <summary>Icon cạnh tên trang trên sidebar.</summary>
        public virtual GnurtIconKind Icon => GnurtIconKind.Dot;

        /// <summary>Window đang chứa trang này.</summary>
        public GnurtWindow Window { get; internal set; }

        /// <summary>Vẽ nội dung trang vào root. Tên và mô tả trang đã được window vẽ sẵn ở trên.</summary>
        public abstract void Build(VisualElement root);

        /// <summary>Gọi sau khi trang được vẽ.</summary>
        public virtual void OnShow() { }

        /// <summary>Gọi khi rời trang, vẽ lại trang, hoặc đóng window.</summary>
        public virtual void OnHide() { }
    }
}
