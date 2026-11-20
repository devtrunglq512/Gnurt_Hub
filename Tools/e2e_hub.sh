#!/usr/bin/env bash
# Test Hub như người dùng thật: project trống, chỉ thêm một dòng git URL của Hub, rồi để Hub tự lo phần còn lại.
# Mỗi bản Unity chạy hai lần trên cùng một project:
#   1. install: Unity mở project, bootstrap của Hub tự thêm com.gnurt.core, rồi thoát.
#   2. check:   Unity mở lại project (đủ Hub + core), kiểm tra catalog, tag, chặn gỡ core,
#               chuyển core sang bản local rồi về git.
# Phải mở lại giữa hai lần vì Unity 6000.3 chạy batchmode không compile package git được thêm giữa phiên.
# Cần đã push nhánh cần test lên GitHub.
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
RUN_ID="$(date +%Y%m%d-%H%M%S)"

failed=0
for version in "${VERSIONS[@]}"; do
  unity="$UNITY_ROOT/$version/Editor/Unity.exe"
  if [[ ! -f "$unity" ]]; then
    echo "[$version] Không thấy Unity ở $unity, bỏ qua"
    continue
  fi

  # Project mới tinh mỗi lần (thư mục mới theo thời gian): chỉ có bộ kiểm tra và một dòng Hub trong manifest.
  project="$WORK/$version-$RUN_ID"
  mkdir -p "$project/Packages" "$project/Assets"
  cp -r "$REPO/Tools/e2e/Assets/." "$project/Assets/"
  cat > "$project/Packages/manifest.json" <<EOF
{
  "dependencies": {
    "com.gnurt.hub": "$REPO_URL?path=/Packages/com.gnurt.hub#$REVISION"
  }
}
EOF

  code=0
  for phase in install check; do
    echo "[$version] Lần chạy \"$phase\", có thể mất vài phút…"
    GNURT_E2E_PHASE="$phase" GNURT_E2E_REVISION="$REVISION" GNURT_E2E_REPOS="$(dirname "$REPO")" \
      "$unity" -batchmode -nographics -projectPath "$project" -logFile "$project-$phase.log"
    code=$?
    [[ $code -ne 0 ]] && break
  done

  sed "s/^/[$version] /" "$project/e2e-result.txt" 2>/dev/null
  if [[ $code -ne 0 ]] || ! grep -q "^PASS" "$project/e2e-result.txt" 2>/dev/null; then
    failed=1
    echo "[$version] LỖI (Unity thoát với mã $code). Log: $project-$phase.log"
    grep -E "error CS|\[Gnurt\]|\[GnurtE2E\]" "$project-$phase.log" | sort -u | head -30
  fi
done

exit $failed
