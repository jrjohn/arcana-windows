#!/usr/bin/env bash
# Claude Code Stop hook (wired in .claude/settings.json): Claude may not finish while the
# architecture check fails. Contract: stdin = hook JSON (session_id, stop_hook_active, ...);
# exit 0 = allow the stop; exit 2 = block it, stderr is shown to Claude as the reason.
#
#   echo '{"session_id":"x"}' | tools/claude-stop-arch-check.sh
#
# - Nothing changed under the source paths (vs HEAD, incl. untracked) -> exit 0, silent.
# - Docker missing -> exit 0 with a warning (the user cannot fix that by looping Claude).
# - tools/arch-check.sh FAIL -> exit 2 with score + CRITICAL violations.
# - After MAX_BLOCKS consecutive FAIL blocks in one session -> exit 0, loudly NOT done.
# Works on macOS bash 3.2 and Linux; no jq.
set -u

# Paths whose changes can change the arch-qube verdict (the layer directories of AGENTS.md).
SOURCE_RE='^(src|plugins|tests)/'
MAX_BLOCKS=3

ROOT="$(cd "$(dirname "$0")/.." && pwd)"
cd "$ROOT" || exit 0

INPUT="$(cat 2>/dev/null || true)"
SID=""
if command -v python3 >/dev/null 2>&1; then
    SID="$(printf '%s' "$INPUT" | python3 -c 'import json,sys
try: print(json.load(sys.stdin).get("session_id") or "")
except Exception: print("")' 2>/dev/null)"
fi
[ -n "$SID" ] || SID="$(printf '%s' "$INPUT" | tr -d '\n' | sed -n 's/.*"session_id"[[:space:]]*:[[:space:]]*"\([^"]*\)".*/\1/p')"
SID="$(printf '%s' "${SID:-unknown}" | tr -c 'A-Za-z0-9_.-' '_')"

GIT_DIR="$(git rev-parse --git-dir 2>/dev/null)" || exit 0
COUNTER="$GIT_DIR/arch-check-stop.$SID"

# 1. Skip when no source path changed (staged, unstaged or untracked).
CHANGED="$(git status --porcelain --untracked-files=all 2>/dev/null | sed -E 's/^...//; s/^.* -> //; s/^"//; s/"$//' | grep -E "$SOURCE_RE" || true)"
[ -n "$CHANGED" ] || exit 0

# 2. Run the check.
OUT="$("$ROOT/tools/arch-check.sh" 2>&1)"
RC=$?

if [ "$RC" -eq 0 ]; then
    rm -f "$COUNTER"
    exit 0
fi

if [ "$RC" -eq 2 ]; then
    {
        echo "WARNING: architecture NOT checked — tools/arch-check.sh could not run:"
        printf '%s\n' "$OUT" | grep 'arch-check:' | tail -3
        echo "Install/start Docker, or run the pipx command in AGENTS.md. The work is NOT verified."
    } >&2
    exit 0
fi

# 3. FAIL: summarise the report.
REPORT="$ROOT/arch-qube-reports/arch-qube.json"
SUMMARY=""
if command -v python3 >/dev/null 2>&1 && [ -f "$REPORT" ]; then
    SUMMARY="$(python3 - "$REPORT" <<'PY' 2>/dev/null
import json, sys
d = json.load(open(sys.argv[1]))
s = d.get("score", {})
print("score %s (threshold %s), pass=%s" % (s.get("total"), s.get("threshold"), s.get("pass")))
for r in d.get("rules", []):
    v = r.get("violations") or []
    if not v:
        continue
    print("%s rule %s (%s): %d violation(s)" % (str(r.get("severity", "")).upper(), r.get("id"), r.get("name"), len(v)))
    for x in v[:10]:
        print("  %s:%s  %s" % (x.get("file"), x.get("line"), x.get("message")))
    if len(v) > 10:
        print("  ... %d more" % (len(v) - 10))
PY
)"
fi
[ -n "$SUMMARY" ] || SUMMARY="$(grep -iE 'score|critical|FAIL' "$ROOT/arch-qube-reports/arch-qube.md" 2>/dev/null | head -20)"

N=0
[ -f "$COUNTER" ] && N="$(cat "$COUNTER" 2>/dev/null || echo 0)"
case "$N" in ''|*[!0-9]*) N=0 ;; esac
N=$((N + 1))

if [ "$N" -gt "$MAX_BLOCKS" ]; then
    {
        echo "!!! ARCHITECTURE CHECK STILL FAILING after $MAX_BLOCKS blocked stops — letting you stop, but the work is NOT done. !!!"
        printf '%s\n' "$SUMMARY"
        echo "Tell the user the architecture check fails; report: arch-qube-reports/arch-qube.md"
    } >&2
    exit 0
fi
echo "$N" > "$COUNTER"

{
    echo "Architecture check FAILED (tools/arch-check.sh exit $RC) — block $N/$MAX_BLOCKS:"
    printf '%s\n' "$SUMMARY"
    echo "Report: arch-qube-reports/arch-qube.md. Fix the code per AGENTS.md; do not lower the threshold, add exclusions, or move code to dodge a rule."
} >&2
exit 2
