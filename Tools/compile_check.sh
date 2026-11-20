#!/usr/bin/env bash
# Compile các package Gnurt bằng nhiều bản Unity; thêm --tests để chạy luôn EditMode test.
# Dùng project tạm nằm ngoài repo, nên không đụng tới project dev đang mở trong Unity.
#
#   Tools/compile_check.sh           # chỉ compile
#   Tools/compile_check.sh --tests   # compile + chạy test
#
# Chạy bằng Git Bash. Lần đầu chậm (Unity phải tạo project tạm), các lần sau nhanh hơn.
set -uo pipefail

REPO="$(cd "$(dirname "$0")/.." && (pwd -W 2>/dev/null || pwd))"
UNITY_ROOT="C:/Program Files/Unity/Hub/Editor"
VERSIONS=("2022.3.62f2" "6000.3.6f1")
PACKAGES=("com.gnurt.core" "com.gnurt.hub")
WORK="$(cygpath -m "${LOCALAPPDATA:-$HOME}" 2>/dev/null || echo "$HOME")/Gnurt/compile_check"
RUN_TESTS=0
[[ "${1:-}" == "--tests" ]] && RUN_TESTS=1

# Mỗi bản Unity dùng một version test-framework khác nhau.
test_framework_for() {
  case "$1" in
    2022.3.*) echo "1.1.33" ;;
    *) echo "1.6.0" ;;
  esac
}

# Viết manifest trỏ thẳng tới thư mục package trong repo (file:), kèm test-framework.
write_manifest() {
  local project="$1" version="$2" deps="" testables=""
  for p in "${PACKAGES[@]}"; do
    deps+="    \"$p\": \"file:$REPO/Packages/$p\","$'\n'
    testables+="\"$p\", "
  done
  cat > "$project/Packages/manifest.json" <<EOF
{
  "dependencies": {
$deps    "com.unity.test-framework": "$(test_framework_for "$version")"
  },
  "testables": [ ${testables%, } ]
}
EOF
}

failed=0
for version in "${VERSIONS[@]}"; do
  unity="$UNITY_ROOT/$version/Editor/Unity.exe"
  if [[ ! -f "$unity" ]]; then
    echo "[$version] Không thấy Unity ở $unity, bỏ qua"
    continue
  fi

  project="$WORK/$version"
  log="$WORK/$version.log"
  mkdir -p "$project/Assets" "$project/Packages"
  write_manifest "$project" "$version"
  echo "[$version] Đang chạy Unity..."

  if (( RUN_TESTS )); then
    results="$WORK/$version-tests.xml"
    rm -f "$results"
    "$unity" -batchmode -nographics -projectPath "$project" -runTests -testPlatform EditMode -testResults "$results" -logFile "$log"
    code=$?
  else
    "$unity" -batchmode -nographics -quit -projectPath "$project" -logFile "$log"
    code=$?
  fi

  errors="$(grep -E "error CS[0-9]+" "$log" | sort -u)"
  if [[ -n "$errors" ]]; then
    echo "[$version] LỖI COMPILE:"
    echo "$errors"
    failed=1
    continue
  fi
  echo "[$version] Compile OK"

  if (( RUN_TESTS )); then
    if [[ ! -f "$results" ]]; then
      echo "[$version] Không có file kết quả test (Unity thoát với mã $code), xem log: $log"
      failed=1
      continue
    fi
    summary="$(grep -o '<test-run[^>]*>' "$results" | grep -oE '(total|passed|failed|skipped)="[0-9]+"' | tr '\n' ' ')"
    echo "[$version] Test: $summary"
    failed_tests="$(grep -oE '<test-case [^>]*result="Failed"[^>]*>' "$results" | grep -oE 'fullname="[^"]*"')"
    if [[ -n "$failed_tests" ]]; then
      echo "[$version] Test lỗi:"
      echo "$failed_tests"
      failed=1
    fi
  fi
done

exit $failed
