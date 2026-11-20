# Changelog

## [0.1.0] - Unreleased

### Runtime (`Gnurt.Core`)
- `GLog` / `GLogger`: log có tag và level (số cố định 0–5), tắt được từng tag, sự kiện `ErrorLogged`.
- `MainThread`: đưa code về main thread (`Run`, `Post`), chạy cả trong Editor lẫn bản build.
- `EventBus`: sự kiện toàn cục type-safe, mỗi handler chạy trong try/catch riêng, có `PublishOnMainThread`.
- `Observable<T>`: giá trị kèm sự kiện `Changed`.
- `GnurtService`: vòng đời Idle → Initializing → Ready/Failed, có timeout và `WhenReady`.
- `Services`: đăng ký/lấy service theo kiểu, `WhenReady` chờ được cả service chưa đăng ký.
- `Singleton<T>`, `PersistentMonoSingleton<T>`.
- Dọn dữ liệu static tạo trong Play Mode khi thoát (hỗ trợ tắt domain reload).

### Editor (`Gnurt.Core.Editor`)
- `Loc` / `LocText`: chuỗi hai ngôn ngữ vi/en.
- `GnurtWindow` / `GnurtPage` / `GnurtUI`: khung window chung (header có logo, sidebar chia nhóm có icon, footer), theme sáng/tối, mở mặc định 1040×680 qua `GnurtWindow.Open<T>()`.
- `GnurtIcon`: icon nét vẽ bằng Painter2D, đổi màu theo USS.
- `GnurtEditorAssets`: logo Gnurt và style dùng chung.
- `PackageOps`: liệt kê, cài, gỡ package; liệt kê tag version của repo git; `IsGnurtPackage`, `InfoOf`.
- `GitSource`, `PackageVersion`, `VersionTags`: đọc git URL, so sánh version, lọc tag theo tiền tố.
