using System;
using UnityEditor;

namespace Gnurt.Editor
{
    public enum Language
    {
        Vietnamese = 0,
        English = 1,
    }

    /// <summary>Chuỗi hai ngôn ngữ cho giao diện Editor. Dùng: <c>Loc.T("Install", "Cài đặt")</c>.</summary>
    public static class Loc
    {
        const string PrefKey = "Gnurt.Language";
        static Language? _current;

        /// <summary>Bắn khi đổi ngôn ngữ, để các window vẽ lại.</summary>
        public static event Action Changed;

        /// <summary>Ngôn ngữ đang dùng, được nhớ trên máy (EditorPrefs).</summary>
        public static Language Current
        {
            get => _current ??= (Language)EditorPrefs.GetInt(PrefKey, (int)Language.Vietnamese);
            set
            {
                if (value == Current) return;
                _current = value;
                EditorPrefs.SetInt(PrefKey, (int)value);
                Changed?.Invoke();
            }
        }

        /// <summary>Chọn chuỗi theo ngôn ngữ đang dùng.</summary>
        public static string T(string english, string vietnamese) =>
            Current == Language.Vietnamese ? vietnamese : english;
    }

    /// <summary>Chuỗi hai ngôn ngữ để giữ lại và đọc sau (tên trang, mục trong danh sách...).</summary>
    public readonly struct LocText
    {
        public readonly string English;
        public readonly string Vietnamese;

        public LocText(string english, string vietnamese)
        {
            English = english;
            Vietnamese = vietnamese;
        }

        public override string ToString() => Loc.T(English, Vietnamese);

        public static implicit operator string(LocText text) => text.ToString();
    }
}
