#!/usr/bin/env bash
# Test Hub như người dùng thật: project trống, chỉ thêm một dòng git URL của Hub, rồi để Hub tự lo phần còn lại
# (bootstrap tự cài core, tải catalog từ GitHub, đọc tag, chặn gỡ core, chuyển core sang bản local rồi về git).
# Cần đã push nhánh cần test lên GitHub. Kết quả từng bước in ra cuối cùng.
#
#   Tools/e2e_hub.sh                          # nhánh dev/0.1.0, Unity 6000.3 và 2022.3
#   Tools/e2e_hub.sh dev/0.1.0 6000.3.6f1     # chỉ một bản Unity
set -uo pipefail

REVISION="${1:-dev/0.1.0}"
shift || true
VERSIONS=("$@")
[[ ${#VERSIONS[@]} -eq 0 ]] && VERSIONS=("6000.3.6f1" "2022.3.62f2")

REPO_URL="https://github.com/devtrunglq512/Gnurt_Hub.git"
REPO="$(cd "$(dirname "$0")/.." && (pwd -W 2>/dev/null || pwd))"
UNITY_ROOT="C:/Program Files/Unity/Hub/Editor"
WORK="$(cygpath -m "${LOCALAPPDATA:-$HOME}" 2>/dev/null || echo "$HOME")/Gnurt/e2e"

failed=0
for version in "${VERSIONS[@]}"; do
  unity="$UNITY_ROOT/$version/Editor/Unity.exe"
  if [[ ! -f "$unity" ]]; then
    echo "[$version] Không thấy Unity ở $unity, bỏ qua"
    continue
  fi

  # Project mới tinh mỗi lần: chỉ có bộ kiểm tra và một dòng Hub trong manifest.
  project="$WORK/$version"
  rm -rf "$project"
  mkdir -p "$project/Packages" "$project/Assets"
  cp -r "$REPO/Tools/e2e/Assets/." "$project/Assets/"
  cat > "$project/Packages/manifest.json" <<EOF
{
  "dependencies": {
    "com.gnurt.hub": "$REPO_URL?path=/Packages/com.gnurt.hub#$REVISION"
  }
}
EOF

  echo "[$version] Đang chạy, có thể mất vài phút…"
  GNURT_E2E_REVISION="$REVISION" GNURT_E2E_REPOS="$(dirname "$REPO")" \
    "$unity" -batchmode -nographics -projectPath "$project" -logFile "$WORK/$version.log"
  code=$?

  sed "s/^/[$version] /" "$project/e2e-result.txt" 2>/dev/null
  if [[ $code -ne 0 ]] || ! grep -q "^PASS" "$project/e2e-result.txt" 2>/dev/null; then
    failed=1
    echo "[$version] LỖI (Unity thoát với mã $code). Log: $WORK/$version.log"
    grep -E "error CS|\[Gnurt\]|\[GnurtE2E\]" "$WORK/$version.log" | sort -u | head -30
  fi
done

exit $failed
