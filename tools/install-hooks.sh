#!/usr/bin/env bash
# Point git at tools/githooks so the pre-commit layering check runs on every commit.
# Git does not install hooks on clone: run this once in every fresh clone (AGENTS.md §0).
set -euo pipefail
ROOT="$(cd "$(dirname "${BASH_SOURCE[0]}")/.." && pwd)"
chmod +x "$ROOT"/tools/githooks/* "$ROOT"/tools/*.sh
git -C "$ROOT" config core.hooksPath tools/githooks
echo "hooks installed: core.hooksPath=tools/githooks"
