# Gnurt Hub & Core: kiến trúc và kế hoạch

Repo này chứa hai package UPM:

- `com.gnurt.core`: nền chung cho **mọi** package Gnurt, gồm pattern, hạ tầng và bộ UI cho Editor.
- `com.gnurt.hub`: một window duy nhất để cài, update và kích hoạt license cho các package Gnurt khác.

Cả hai đều public. Các package có thu phí nằm ở repo riêng (xem mục 1).

---

## 1. Toàn hệ thống

```
                 com.gnurt.core   ← nền chung (public)
          ▲            ▲              ▲              ▲
   com.gnurt.hub  firebase-manager  logicmap    gnurt ads sdk
     (public)       (cần key)      (tuỳ chọn)    (cần key)
        │
        └── gọi ──► gnurt-api (Cloudflare: Workers + D1 + R2) ◄── Web admin (/admin, chỉ bạn vào được)
```

| Repo | Nội dung | Public? |
|---|---|---|
| **Gnurt_Hub** (repo này) | `com.gnurt.core`, `com.gnurt.hub` | Public |
| Gnurt_Sdk | SDK ads/analytics (ads, Firebase, AppsFlyer), thiết kế theo grus-sdk-v3 | Private, cấp qua key |
| Gnurt_Firebase_Manager | `com.gnurt.firebase-manager`: tool Editor cho các việc Firebase thường ngày | Private, cấp qua key |
| Gnurt_LogicMap | `com.gnurt.logicmap` | Chưa chốt |
| gnurt-api (mới, không phải project Unity) | License server viết bằng TypeScript + Hono, chạy trên Cloudflare. Cùng một Worker phục vụ luôn **web admin** ở `/admin` (đối tác → project → Gen key, thu hồi, xem log), bảo vệ bằng Cloudflare Access | Private |

**Nguyên tắc**
- Mọi package chỉ phụ thuộc vào `com.gnurt.core`, không phụ thuộc lẫn nhau.
- Extension không tham chiếu Hub. Hub biết các extension qua catalog.
- `package.json` không khai được dependency dạng git (giới hạn của Unity). Vì vậy dependency được khai trong catalog, và Hub cài chúng cùng lúc với extension.

## 2. Phân phối

**Chốt cách cài:**

| Loại package | Cài bằng | Repo |
|---|---|---|
| Miễn phí: `com.gnurt.core`, `com.gnurt.hub` (và extension miễn phí nếu có) | **Git URL** kèm tag | Public trên GitHub |
| Có thu phí: FirebaseManager, SDK ads… | **tgz chứa DLL**, Hub tải từ license server sau khi nhập key (Phase 3 trở đi) | Private |

**Nhánh:**
- Phát triển trên nhánh `dev/<version>`, hiện tại là `dev/0.1.0`. Chỉ commit và push lên nhánh đó.
- Không đụng `main` cho tới khi xong version. Xong thì merge vào `main` rồi mới gắn tag phát hành (`core/v0.1.0`, `hub/v0.1.0`).
- Trong lúc phát triển, cài thử bằng tên nhánh thay cho tag: `...Gnurt_Hub.git?path=/Packages/com.gnurt.hub#dev/0.1.0`. Bootstrap của Hub cài core từ **cùng nhánh hoặc cùng repo** với Hub, nên bản dev luôn đi với core bản dev.

**Chi tiết:**
- Cài bằng git URL kèm tag. Mỗi package có tag riêng `core/vX.Y.Z`, `hub/vX.Y.Z`, khớp với `version` trong `package.json`. Ghi `CHANGELOG.md`, theo semver. **Không dời tag** đã phát hành.
- URL dự kiến (repo GitHub chưa tạo):
  - `https://github.com/devtrunglq512/Gnurt_Hub.git?path=/Packages/com.gnurt.hub#hub/v0.1.0`
  - `https://github.com/devtrunglq512/Gnurt_Hub.git?path=/Packages/com.gnurt.core#core/v0.1.0`
- Project game commit `Packages/manifest.json` và `Packages/packages-lock.json`. File lock ghim commit hash để cả team dùng đúng một bản.
- **Không dùng LFS cho thư mục package.** Cuối `.gitattributes` có các dòng ghi đè LFS cho `Packages/com.gnurt.*`. Lý do: máy không có git-lfs sẽ chỉ nhận file con trỏ, và mỗi lần ai đó cài là trừ vào quota LFS của chủ repo.
- Unity tối thiểu **2022.3**. Repo dev chạy 6000.3.6f1; kiểm tra compile thêm với 2022.3.62f2. API chỉ có trên Unity 6 thì bọc trong `#if UNITY_6000_0_OR_NEWER`.
- Package có thu phí không đi đường git: phát dạng tgz chứa DLL qua license server (mục 5).

## 3. `com.gnurt.core`

| Assembly | Namespace | Ghi chú |
|---|---|---|
| `Gnurt.Core` | `Gnurt` | Runtime, không phụ thuộc gì |
| `Gnurt.Core.Editor` | `Gnurt.Editor` | Chỉ chạy trong Editor |
| `Gnurt.Core.Tests.Editor` | `Gnurt.Tests` | EditMode test |

**Nội dung dự kiến**

| Nhóm | Thành phần | Dùng để làm gì |
|---|---|---|
| Observer | `EventBus`: type-safe, event là `readonly struct`, mỗi handler chạy trong try/catch riêng, publish trên main thread | Các module báo cho nhau mà không cần biết nhau |
| | `Observable<T>`: giá trị kèm sự kiện thay đổi | Settings, UI binding |
| Service | `Services` (đăng ký/lấy service) + `GnurtService` (Idle → Initializing → Ready/Failed, có timeout, có `WhenReady`) | Quy ước chung của cả hệ thống |
| Singleton | `Singleton<T>`, `PersistentMonoSingleton<T>` | Có sẵn nhưng hạn chế dùng |
| State | `StateMachine<TState>` | Luồng game, luồng UI |
| Tiện ích | `Result<T>`, `IClock` (đồng hồ thay được khi test), `MainThread` (chạy qua PlayerLoop), `GLog` (tag + level), `IKeyValueStore` | Hạ tầng |
| Pool | Bọc lại `UnityEngine.Pool` | Không viết lại thứ Unity đã có |
| Editor | `GnurtWindow` (sidebar + trang + USS chung), `Loc` (vi/en), `Defines`, `PackageOps` (cài/gỡ/update qua git và tgz, liệt kê version), `SetupChecklist` (dòng ✅/❌ kèm nút Fix), `SettingsAsset<T>` | Mọi window chung một giao diện; Hub và nút Update của từng extension dùng chung `PackageOps` |

**Quy tắc**
- Chỉ đưa vào core những gì có **từ 2 package trở lên** cần dùng.
- Singleton làm code khó test và tạo phụ thuộc ngầm. Quy ước: service đăng ký vào `Services`, game gọi qua facade tĩnh.
- Giữ version **0.x** (cho phép đổi API) cho tới khi SDK ads dùng xong phần runtime.
- **Không đặt code kiểm tra license trong core** (xem mục 5).

## 4. `com.gnurt.hub`

| Assembly | Phụ thuộc | Việc |
|---|---|---|
| `Gnurt.Hub.Bootstrap` | Không có | Project chưa có `com.gnurt.core` thì tự thêm vào, nên người dùng chỉ phải dán **một** URL |
| `Gnurt.Hub.Editor` | `Gnurt.Core`, `Gnurt.Core.Editor` | Chỉ compile khi project đã có core: `versionDefines` đặt `GNURT_CORE` khi có package `com.gnurt.core`, và `defineConstraints` yêu cầu `GNURT_CORE` |

**Tính năng (đã làm ở Phase 2)**
- **Catalog:** một file duy nhất `Packages/com.gnurt.hub/Editor/Catalog/catalog.json`. Mỗi mục gồm `name`, `displayName`, mô tả vi/en, `repo`, `path`, `tagPrefix`, `dependencies`, `minUnity`, `planned` (chưa phát hành), `licensed` (cần key, Phase 3). Thêm extension mới chỉ cần sửa file này.
  - Hub tải bản mới nhất từ `raw.githubusercontent.com`: Hub bản dev đọc catalog trên đúng nhánh của nó, Hub bản phát hành đọc trên `main`. Không tải được thì dùng bản đã lưu ở `Library/Gnurt/HubCatalog.json`, cuối cùng là bản nằm sẵn trong package.
- **Trạng thái:** `Client.List` cho biết version đang cài và nguồn (git, local, embedded); so với tag mới nhất để báo có bản mới.
- **Danh sách version:** mỗi repo chỉ chạy `git ls-remote --tags` một lần, rồi lọc theo `tagPrefix` của từng package.
- **Cài, update, gỡ:** dependency còn thiếu được cài kèm trong cùng một lần `Client.AddAndRemove`. Không cho gỡ package đang được package khác cần.
- **Kênh cập nhật:** *Phát hành* cài tag mới nhất; *Dev* cài nhánh (mặc định là nhánh Hub đang dùng). Hub cài từ nhánh thì kênh Dev tự bật.
- **Bản local:** thư mục chứa các repo (mặc định là thư mục cha của project). Package nào có repo ở đó thì có nút chuyển sang `file:` để sửa và thử ngay, rồi chuyển lại git. Cùng ổ đĩa thì đường dẫn tương đối, khác ổ thì tuyệt đối.
- **Cài đặt riêng từng người:** lưu trong `UserSettings/EditorUserSettings.asset`, không commit.
- Hub tự update chính nó (Hub cũng là một mục trong catalog).
- **Kích hoạt license** (Phase 3).

**Bootstrap:** khi Unity mở mà chưa có core (hoặc core cũ hơn bản Hub cần), tự `Client.Add` core từ cùng repo với Hub. Hub cài bằng tag `hub/vX` thì core lấy tag `core/v{RequiredCoreVersion}`; Hub cài bằng nhánh/commit thì core lấy đúng nhánh/commit đó. Chưa có core thì menu **Gnurt > Hub** là nút cài core.

**Lỗi đã biết của Unity 6000.3:** khi chạy batchmode, package git thêm vào **giữa phiên** (bằng `Client.Add`) không được compile cho tới khi mở lại Unity. Package local (`file:`) không bị, Unity 2022.3 không bị. Đã thử `RequestScriptCompilation`, `AssetDatabase.Refresh(ForceUpdate)`, `Client.Resolve`, `ImportAsset` đều không gỡ được; mở lại project thì hết. Unity mở cửa sổ thường chưa kiểm được. Cách xử lý:
- Bootstrap theo dõi các lượt compile. Lượt nào có Hub mà không có core (dù core đã nằm trong project) thì hỏi người dùng mở lại project; batchmode thì chỉ ghi warning. Unity compile bình thường thì không bao giờ hỏi.
- `Tools/e2e_hub.sh` chạy Unity hai lần: lần đầu để bootstrap thêm core, lần sau mở lại để kiểm tra.

**Kiểm tra:** `Tools/compile_check.sh --tests` (logic, cả 2022.3 và 6000.3) và `Tools/e2e_hub.sh` (cài thật từ GitHub vào project trống, kiểm tra bootstrap, catalog, tag, chặn gỡ, chuyển local rồi về git).

**Hub không làm:** không quản lý SDK bên thứ ba (MAX, AppsFlyer…), không cấu hình bên trong extension.

## 5. License cho package có thu phí

**Server:** Cloudflare Workers + D1 (SQL) + R2 (lưu tgz, không tính phí băng thông tải xuống). API viết bằng TypeScript + Hono, chạy trên **domain riêng** để sau này đổi nhà cung cấp mà Hub đã cài ở máy đối tác không bị hỏng.

**Dữ liệu**

| Bảng | Trường chính |
|---|---|
| Đối tác | tên, liên hệ, trạng thái |
| Project (thuộc đối tác) | Android package, iOS bundle id, services (`*` = mọi service, kể cả service ra mắt sau; hoặc danh sách), hạn dùng, đã thu hồi |
| Key (thuộc project) | chuỗi ngẫu nhiên kiểu `GNURT-XXXX-XXXX-XXXX`, server chỉ lưu bản hash |
| Log kích hoạt | thời gian, IP, version Unity, package name, `PlayerSettings.productGUID` |

**Luồng kích hoạt**
1. Admin tạo đối tác → tạo project → chọn services → Gen key → gửi key cho đối tác.
2. Đối tác cài Hub vào project → nhập key. Hub gửi key kèm package name lấy từ Player Settings.
3. Server kiểm tra key còn hiệu lực, đúng project, đúng package name, rồi trả về **file license đã ký RSA-SHA256** (đối tác, project, package name, services, hạn dùng). Unity (Mono/IL2CPP) xác minh được chữ ký này mà không cần thư viện ngoài.
4. Hub lưu file vào `Assets/GnurtHub/Resources/GnurtLicense.json`. File được commit vào repo game, nên cả team của đối tác dùng chung. Đặt trong `Resources` để SDK chạy trong game đọc được license ở bản build.
5. Bấm Install: server cấp link tải tgz ngắn hạn → Hub lưu vào `Packages/tarballs/` → `Client.Add("file:tarballs/...")`.

**Kiểm tra ở 4 chỗ**
- **Kích hoạt:** do server kiểm tra.
- **Editor:** package name trong Player Settings phải nằm trong license, nếu không thì khoá window của service.
- **Build:** `IPreprocessBuildWithReport` ném `BuildFailedException` kèm thông báo rõ ràng.
- **Runtime** (chỉ cho SDK chạy trong game): `Application.identifier` phải khớp license, nếu không thì SDK không khởi động.

Ngoài ra Editor kiểm tra lại online định kỳ (ví dụ 7 ngày một lần), có thời gian ân hạn khi mất mạng.

**Chống bypass**
- Package có thu phí phát dạng **DLL** (obfuscate phần kiểm tra license), không phát mã nguồn.
- Code kiểm tra license nằm ở repo private và được **compile thẳng vào DLL của từng service**. Nếu nó là một DLL dùng chung thì chỉ cần sửa đúng một file là mở khoá được tất cả.
- DLL compile một lần nên `#if UNITY_6000_0_OR_NEWER` không còn tác dụng: phải rẽ nhánh lúc chạy, hoặc build hai bản DLL.

**Rủi ro còn lại**
- Trong lúc dev, người khác có thể tạm đổi package name để dùng tool trong Editor. Build phát hành vẫn bị chặn vì không thể lên store bằng package name của người khác. Server phát hiện qua log: một key xuất hiện từ nhiều `productGUID`.
- Đối tác có thể gửi file tgz cho người khác. Giảm thiểu bằng cách nhúng mã khách hàng vào từng tgz, kết hợp với hợp đồng.

## 6. Quy ước

- **File trong Assets của game:** mỗi package Gnurt có một thư mục riêng `Assets/Gnurt<Tên>/`. Ví dụ Hub dùng `Assets/GnurtHub/`. Mọi thứ package tạo ra trong Assets (ScriptableObject, thư mục `Resources/` riêng, file license…) đều nằm trong đó, không để file lẻ ở chỗ khác. File trong `Resources` đặt tên có tiền tố `Gnurt` để không trùng với package khác.
- **Menu:** mọi window Gnurt nằm trong menu `Gnurt` trên thanh menu trên cùng của Unity (ngang hàng `Tools`), ví dụ `Gnurt/Hub`. Window mở bằng `GnurtWindow.Open<T>()` để có kích thước mặc định 1040×680.
- **Logo và màu:** file gốc `Docs/brand/GnurtLogo.svg`, bản PNG nằm ở `Packages/com.gnurt.core/Editor/Icons/GnurtLogo.png`. Ý nghĩa: chữ G mở về phía ngôi sao (đích hoàn hảo); nền và màu vàng sáng dần lên góc trên phải (thăng tiến). Màu nhấn của giao diện là tím lấy từ nền logo; đường kẻ dưới header màu vàng của chữ G.
- **Comment trong code:** ngắn, đơn giản, dễ hiểu, viết tiếng Việt. Chỉ giải thích *vì sao* hoặc điều không hiển nhiên, không kể lại code.
- **Tắt domain reload (Enter Play Mode Options):** dữ liệu static tạo trong Play Mode (subscriber của `EventBus`, service, singleton) được dọn khi thoát Play Mode, nhờ `PlayModeTracker` trong core.
- **Thread:** `EventBus`, `Services`, `GnurtService.WhenReady` gọi từ main thread. Callback từ thread khác thì đi qua `MainThread.Run` hoặc `EventBus.PublishOnMainThread`.

## 7. Kế hoạch

| Phase | Việc | Trạng thái |
|---|---|---|
| 0 | Khung repo: project Unity, git + luật LFS, hai package, các asmdef, tài liệu này | ✅ |
| 1 | Core bản tối thiểu: `GLog`, `MainThread`, `EventBus`, `Observable`, `Services`/`GnurtService`, `Singleton`, bộ UI Editor (`GnurtWindow`, `Loc`, `GnurtUI`), `PackageOps`, EditMode test, `Tools/compile_check.sh` | ✅ (chưa tag `core/v0.1.0`) |
| 2 | Hub cho package miễn phí: Bootstrap cài core, catalog, cài/update/gỡ qua git, kênh phát hành/dev, bản local | ✅ (chưa tag `hub/v0.1.0`) |
| 3 | Theo thứ tự: 3a server Cloudflare (D1 schema, API kích hoạt / quyền / tải file / admin) → 3b web admin → 3c Hub nhập key và cài tgz | |
| 4 | FirebaseManager: service có key đầu tiên, build DLL, CI build tgz | |
| 5 | SDK ads/analytics (repo Gnurt_Sdk) | |

## 8. Quyết định và ghi chú

| Ngày | Quyết định |
|---|---|
| 2026-11-20 | License server riêng (không dùng collaborator GitHub), chạy trên Cloudflare |
| 2026-11-20 | License tính theo đối tác + project, ràng buộc bằng package name (không dùng keystore) |
| 2026-11-20 | Repo: Gnurt_Hub chứa core + hub; Gnurt_Sdk dành cho SDK ads |
| 2026-11-20 | Thứ tự: core → hub → server → FirebaseManager → SDK ads |
| 2026-11-20 | Package miễn phí cài qua git (repo public); package thu phí cài qua tgz từ license server |
| 2026-11-20 | Commit và push chỉ trên `dev/0.1.0`; `main` chỉ nhận bản đã xong |

**Còn mở**
- Repo public chưa có file `LICENSE`: người khác xem được nhưng về mặt pháp lý không được dùng. Cần chọn license nếu muốn cho dùng.
- grus-sdk-v3 là code của công ty: mặc định chỉ dùng làm tài liệu thiết kế, code Gnurt viết mới hoàn toàn.
