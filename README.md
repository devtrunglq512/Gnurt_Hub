# Gnurt Hub

<img src="Docs/brand/GnurtLogo.svg" width="96" alt="Gnurt">

Một window trong Unity để cài, cập nhật và gỡ các package của Gnurt. Repo gồm hai package:

| Package | Nội dung |
|---|---|
| `com.gnurt.hub` | Window **Gnurt > Hub** |
| `com.gnurt.core` | Nền chung cho mọi package Gnurt: log, EventBus, service, singleton, bộ giao diện Editor. Hub tự cài, không cần cài tay |

Yêu cầu: Unity **2022.3** trở lên, máy có **git**.

## Cài đặt

Trong Unity: **Window > Package Manager > + > Add package from git URL…**, dán:

```
https://github.com/devtrunglq512/Gnurt_Hub.git?path=/Packages/com.gnurt.hub#hub/v0.1.0
```

Hub sẽ tự cài `com.gnurt.core`. Sau đó mở **Gnurt > Hub** trên thanh menu.

> Unity 6000.3: đã gặp trường hợp Unity chưa nạp package git vừa thêm cho tới khi mở lại project. Nếu Hub hỏi "Mở lại project", hoặc chưa thấy menu **Gnurt**, hãy mở lại project là xong.

> Bản 0.1.0 đang phát triển trên nhánh `dev/0.1.0`, chưa có tag phát hành. Để thử bản này, thay `#hub/v0.1.0` bằng `#dev/0.1.0`.

## Dùng Hub

- **Package**: mỗi package một thẻ, có nút Cài / Cập nhật / Gỡ và menu chọn version.
- **Cài đặt**: chọn kênh *Phát hành* (tag) hay *Dev* (nhánh); đặt thư mục chứa các repo Gnurt để dùng bản local.
- Cài một package thì Hub tự cài kèm các package nó cần, trong một lần.

## Phát triển

- Mở project trong repo này bằng Unity 6000.3. Hai package nằm thẳng trong `Packages/` nên sửa trực tiếp được.
- `Tools/compile_check.sh --tests`: compile và chạy test trên Unity 2022.3 và 6000.3.
- `Tools/e2e_hub.sh`: thử cài Hub thật từ GitHub vào project trống.
- Thiết kế và các quyết định: [Docs/ARCHITECTURE.md](Docs/ARCHITECTURE.md).
