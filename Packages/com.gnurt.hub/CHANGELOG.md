# Changelog

## [0.1.0] - Unreleased
- Window **Gnurt > Hub** với 4 trang: Tổng quan, Package, Cài đặt, Giới thiệu.
- Catalog `Editor/Catalog/catalog.json`: tải bản mới nhất trên GitHub, không được thì dùng bản đã lưu trong Library, cuối cùng là bản có sẵn.
- Cài, cập nhật, gỡ, đổi version package qua git. Dependency còn thiếu được cài kèm trong cùng một lần; không cho gỡ package đang được cần.
- Kênh cập nhật: Phát hành (tag `core/vX`, `hub/vX`) hoặc Dev (nhánh). Hub cài từ nhánh thì kênh Dev tự bật.
- Bản local: chuyển package sang thư mục repo trên máy để sửa và thử ngay, rồi chuyển lại git.
- Bootstrap: project chưa có `com.gnurt.core` thì tự cài từ cùng repo và cùng nhánh/tag với Hub.
