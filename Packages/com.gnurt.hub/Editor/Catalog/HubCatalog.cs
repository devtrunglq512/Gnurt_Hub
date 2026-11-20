using System;
using Gnurt.Editor;
using UnityEngine;

namespace Gnurt.Hub
{
    /// <summary>Danh sách package Gnurt mà Hub biết, đọc từ catalog.json.</summary>
    [Serializable]
    public sealed class HubCatalog
    {
        public int version;
        public CatalogEntry[] packages = Array.Empty<CatalogEntry>();

        public CatalogEntry Find(string name) => Array.Find(packages, p => p.name == name);

        /// <summary>Đọc JSON. Sai định dạng hoặc thiếu trường bắt buộc thì trả false kèm lý do.</summary>
        public static bool TryParse(string json, out HubCatalog catalog, out string error)
        {
            catalog = null;
            error = null;
            HubCatalog parsed;
            try
            {
                parsed = JsonUtility.FromJson<HubCatalog>(json);
            }
            catch (Exception e)
            {
                error = "JSON lỗi: " + e.Message;
                return false;
            }

            if (parsed?.packages == null || parsed.packages.Length == 0)
            {
                error = "catalog không có package nào";
                return false;
            }

            foreach (var entry in parsed.packages)
            {
                if (string.IsNullOrEmpty(entry.name))
                {
                    error = "có package thiếu tên";
                    return false;
                }
                // Package đã phát hành thì phải biết cài từ đâu.
                if (!entry.planned && string.IsNullOrEmpty(entry.repo))
                {
                    error = $"{entry.name} thiếu repo";
                    return false;
                }
                entry.dependencies ??= Array.Empty<string>();
                entry.tagPrefix ??= "";
            }

            catalog = parsed;
            return true;
        }
    }

    /// <summary>Một package trong catalog.</summary>
    [Serializable]
    public sealed class CatalogEntry
    {
        public string name;
        public string displayName;
        public string descriptionEn;
        public string descriptionVi;

        /// <summary>URL repo git, ví dụ https://github.com/devtrunglq512/Gnurt_Hub.git</summary>
        public string repo;

        /// <summary>Thư mục package trong repo, ví dụ /Packages/com.gnurt.core</summary>
        public string path;

        /// <summary>Tiền tố tag version của package này, ví dụ "core/" cho tag core/v1.2.0.</summary>
        public string tagPrefix;

        public string[] dependencies;
        public string minUnity;

        /// <summary>Chưa phát hành: chỉ hiện để biết, chưa cài được.</summary>
        public bool planned;

        /// <summary>Cần key mới cài được (làm ở Phase 3).</summary>
        public bool licensed;

        public string Title => string.IsNullOrEmpty(displayName) ? name : displayName;
        public string Description => Loc.T(descriptionEn, descriptionVi);
    }
}
