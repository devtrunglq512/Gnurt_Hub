using UnityEngine;
using UnityEngine.UIElements;

namespace Gnurt.Editor
{
    public enum GnurtIconKind
    {
        Dot,
        Home,
        Package,
        Tag,
        Log,
        Settings,
        Info,
    }

    /// <summary>
    /// Icon nét vẽ bằng Painter2D trên lưới 24×24: mọi icon cùng cỡ, cùng độ dày nét, nét ở mọi kích thước.
    /// Màu lấy từ thuộc tính <c>color</c> trong USS, nên hover hay trạng thái chọn tự đổi màu.
    /// </summary>
    public sealed class GnurtIcon : VisualElement
    {
        const float Grid = 24f;
        const float Stroke = 1.8f;

        readonly GnurtIconKind _kind;

        public GnurtIcon(GnurtIconKind kind)
        {
            _kind = kind;
            AddToClassList("gnurt-icon");
            pickingMode = PickingMode.Ignore;
            generateVisualContent += Draw;
            // USS đổi màu (hover, chọn) thì vẽ lại bằng màu mới.
            RegisterCallback<CustomStyleResolvedEvent>(_ => MarkDirtyRepaint());
        }

        void Draw(MeshGenerationContext context)
        {
            float scale = Mathf.Min(contentRect.width, contentRect.height) / Grid;
            if (scale <= 0) return;

            var p = context.painter2D;
            p.strokeColor = resolvedStyle.color;
            p.fillColor = resolvedStyle.color;
            p.lineWidth = Stroke * scale;
            p.lineCap = LineCap.Round;
            p.lineJoin = LineJoin.Round;

            var pen = new Pen(p, scale);
            switch (_kind)
            {
                case GnurtIconKind.Dot:
                    pen.FillCircle(12, 12, 3);
                    break;

                case GnurtIconKind.Home:
                    pen.Lines(3, 10.5f, 12, 3, 21, 10.5f);
                    pen.Lines(5, 9, 5, 20.5f, 19, 20.5f, 19, 9);
                    pen.Lines(10, 20.5f, 10, 14.5f, 14, 14.5f, 14, 20.5f);
                    break;

                case GnurtIconKind.Package: // hình hộp
                    pen.Lines(12, 2.8f, 20.5f, 7.4f, 20.5f, 16.6f, 12, 21.2f, 3.5f, 16.6f, 3.5f, 7.4f, 12, 2.8f);
                    pen.Lines(3.5f, 7.4f, 12, 12, 20.5f, 7.4f);
                    pen.Lines(12, 12, 12, 21.2f);
                    break;

                case GnurtIconKind.Tag:
                    pen.Lines(3.5f, 3.5f, 11.5f, 3.5f, 20.5f, 12.5f, 12.5f, 20.5f, 3.5f, 11.5f, 3.5f, 3.5f);
                    pen.Circle(8, 8, 1.6f);
                    break;

                case GnurtIconKind.Log: // cửa sổ terminal
                    pen.RoundRect(3, 4, 18, 16, 2.5f);
                    pen.Lines(7, 9.5f, 10, 12, 7, 14.5f);
                    pen.Lines(12, 14.5f, 17, 14.5f);
                    break;

                case GnurtIconKind.Settings: // ba thanh trượt
                    pen.Lines(4, 6, 7, 6);
                    pen.Circle(9, 6, 2);
                    pen.Lines(11, 6, 20, 6);
                    pen.Lines(4, 12, 13, 12);
                    pen.Circle(15, 12, 2);
                    pen.Lines(17, 12, 20, 12);
                    pen.Lines(4, 18, 6, 18);
                    pen.Circle(8, 18, 2);
                    pen.Lines(10, 18, 20, 18);
                    break;

                case GnurtIconKind.Info:
                    pen.Circle(12, 12, 9);
                    pen.Lines(12, 11, 12, 16.5f);
                    pen.FillCircle(12, 7.8f, 1.2f);
                    break;
            }
        }

        /// <summary>Vẽ theo đơn vị của lưới 24×24.</summary>
        readonly struct Pen
        {
            readonly Painter2D _p;
            readonly float _scale;

            public Pen(Painter2D painter, float scale)
            {
                _p = painter;
                _scale = scale;
            }

            Vector2 V(float x, float y) => new Vector2(x * _scale, y * _scale);

            /// <summary>Đường gấp khúc qua các cặp (x, y).</summary>
            public void Lines(params float[] xy)
            {
                _p.BeginPath();
                _p.MoveTo(V(xy[0], xy[1]));
                for (int i = 2; i < xy.Length; i += 2)
                    _p.LineTo(V(xy[i], xy[i + 1]));
                _p.Stroke();
            }

            public void RoundRect(float x, float y, float w, float h, float r)
            {
                _p.BeginPath();
                _p.MoveTo(V(x + r, y));
                _p.ArcTo(V(x + w, y), V(x + w, y + h), r * _scale);
                _p.ArcTo(V(x + w, y + h), V(x, y + h), r * _scale);
                _p.ArcTo(V(x, y + h), V(x, y), r * _scale);
                _p.ArcTo(V(x, y), V(x + w, y), r * _scale);
                _p.ClosePath();
                _p.Stroke();
            }

            public void Circle(float x, float y, float r)
            {
                _p.BeginPath();
                _p.Arc(V(x, y), r * _scale, Angle.Degrees(0), Angle.Degrees(360));
                _p.ClosePath();
                _p.Stroke();
            }

            public void FillCircle(float x, float y, float r)
            {
                _p.BeginPath();
                _p.Arc(V(x, y), r * _scale, Angle.Degrees(0), Angle.Degrees(360));
                _p.ClosePath();
                _p.Fill();
            }
        }
    }
}
