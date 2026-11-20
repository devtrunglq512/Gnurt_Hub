using UnityEditor;
using UnityEngine;
using UnityEngine.UIElements;

namespace Gnurt.Editor
{
    /// <summary>Logo và style nằm trong package core, dùng chung cho mọi window Gnurt.</summary>
    public static class GnurtEditorAssets
    {
        public const string CoreRoot = "Packages/com.gnurt.core/";

        /// <summary>Logo Gnurt (256×256). Nguồn SVG: Docs/brand/GnurtLogo.svg của repo Gnurt_Hub.</summary>
        public static Texture2D Logo => AssetDatabase.LoadAssetAtPath<Texture2D>(CoreRoot + "Editor/Icons/GnurtLogo.png");

        public static StyleSheet WindowStyle => AssetDatabase.LoadAssetAtPath<StyleSheet>(CoreRoot + "Editor/Window/GnurtWindow.uss");
    }
}
