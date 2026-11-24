#!/usr/bin/env bash
# Start a new Windows desktop product from this repo's full layered skeleton.
#
#   tools/new-app.sh Billing ~/Documents/projects/billing-windows
#
# Bash script. On Windows run it from Git Bash (ships with Git for Windows) or WSL; it needs
# git, perl and tar, all of which Git Bash provides.
#
# Copies the committed tree (git archive HEAD), so the new product keeps AGENTS.md, CLAUDE.md,
# tools/ (scaffolding + hook + arch-check), both Jenkinsfiles and the arch-qube gate.
# `dotnet new winui` plus a few copied folders loses all of that (AGENTS.md §1).
#
# No --no-demo option: there is no demo-only module. Orders / Customers / Products are the
# worked examples tools/new-feature.sh anchors on (AppDbContext, DI, NavGraph, MainWindow), and
# plugins/FlowChartModule is the reference external plugin docs/PLUGIN_DEVELOPMENT_GUIDE.md
# walks through. Remove what the product does not need through the DI / plugin registration,
# not by flattening.
set -euo pipefail

[ $# -eq 2 ] || { echo "usage: $0 <ProductName> <dest-dir>   e.g. $0 Billing ~/projects/billing-windows" >&2; exit 1; }
NAME="$1"
DEST="$2"
[[ "$NAME" =~ ^[A-Z][A-Za-z0-9]*$ ]] || { echo "ProductName: PascalCase letters/digits (it becomes the solution prefix, root namespace and assembly names, e.g. Billing.App)" >&2; exit 1; }
[[ "$NAME" =~ ^(Arcana|Microsoft|System)$ ]] && { echo "ProductName must differ from the template's own name and framework namespaces" >&2; exit 1; }
[ ! -e "$DEST" ] || { echo "$DEST already exists — refusing to overwrite" >&2; exit 1; }
command -v perl >/dev/null || { echo "perl is required" >&2; exit 1; }
KEBAB="$(printf '%s' "$NAME" | perl -pe 's/(?<!^)([A-Z])/-$1/g; $_=lc')"

ROOT="$(cd "$(dirname "${BASH_SOURCE[0]}")/.." && pwd)"
git -C "$ROOT" diff --quiet HEAD -- AGENTS.md tools/ || echo "note: uncommitted changes in AGENTS.md/tools are NOT copied (git archive HEAD)" >&2

mkdir -p "$DEST"
git -C "$ROOT" archive HEAD | tar -x -C "$DEST"
SRC_SHA="$(git -C "$ROOT" rev-parse --short HEAD)"
cd "$DEST"

# Prebuilt plugin package (a DLL + zip built from the template) carries the template's assembly
# name inside a binary; the product rebuilds it with plugins/FlowChartModule/build.ps1.
rm -rf plugins/FlowChartModule/package plugins/FlowChartModule/*.zip

# ---- Product identity. Every place the template name is load-bearing:
#  1. Solution prefix `Arcana` = project / assembly names (Arcana.App.csproj …), RootNamespace and
#     every `namespace Arcana.*` / `using Arcana.*`, XAML x:Class / using: namespaces, the sln.
#  2. MSIX identity in src/Arcana.App/Package.appxmanifest (Identity Name, Publisher CN,
#     DisplayName) and a fresh PhoneProductId GUID (two products must not share it).
#  3. The LocalApplicationData folder ("Arcana/data/arcana.db", logs) — two products must not
#     share one database. Lower-case `arcana.db` is renamed too.
#  4. Jenkins APP_NAME `arcana-windows` / `dotnet-app`, the image name, Sonar key `dotnet-app` /
#     name `Dotnet App`, and the per-host arch-qube container names.
# Not renamed on purpose: arcana.boo (registry / arch-qube image host), lower-case plugin ids
# (`arcana.module.customer` — stable plugin contract ids), the skill name
# arcana-windows-developer-skill. tools/ is excluded: these scripts are generic (they detect the
# prefix from src/*.Domain) and must keep working in the product.
GUID="$(uuidgen 2>/dev/null || perl -e 'printf "%04x%04x-%04x-%04x-%04x-%04x%04x%04x", map { int rand 65536 } 1..8')"
GUID="$(printf '%s' "$GUID" | tr 'A-Z' 'a-z')"
TEXT_FILES="$(grep -rIlE -e 'Arcana' -e 'arcana-windows' -e 'dotnet-app' -e 'Dotnet App' -e 'arcana\.db' -e 'arcana-arch-qube-' . --exclude-dir=.git --exclude-dir=tools --exclude=CHANGELOG.md || true)"
[ -n "$TEXT_FILES" ] && printf '%s\n' "$TEXT_FILES" | while IFS= read -r f; do
    N="$NAME" K="$KEBAB" G="$GUID" perl -pi -e '
        s/arcana-arch-qube-windows-/$ENV{K}-arch-qube-windows-/g;
        s/arcana-arch-qube-dotnet-/$ENV{K}-arch-qube-dotnet-/g;
        s/\barcana-windows(?!-developer-skill)\b/$ENV{K}/g;
        s/\bdotnet-app\b/$ENV{K}/g;
        s/Dotnet App/$ENV{N}/g;
        s/\barcana\.db\b/$ENV{K}.db/g;
        s/PhoneProductId="[0-9a-fA-F-]+"/PhoneProductId="$ENV{G}"/g;
        s/\bArcana\b/$ENV{N}/g;
    ' "$f"
done

# Rename every path containing "Arcana" (deepest first so parents move last).
find . -path ./.git -prune -o -name '*Arcana*' -print | awk '{ print length, $0 }' | sort -rn | cut -d' ' -f2- |
while IFS= read -r p; do
    base="$(basename "$p")"; dir="$(dirname "$p")"
    mv "$p" "$dir/${base//Arcana/$NAME}"
done

LEFT="$(grep -rInE --exclude-dir=.git --exclude-dir=tools --exclude=CHANGELOG.md '\bArcana\b|arcana-windows(-developer-skill)?|\bdotnet-app\b|Dotnet App' . | grep -v 'arcana-windows-developer-skill' || true)"
LEFT_PATHS="$(find . -path ./.git -prune -o -name '*Arcana*' -print)"
if [ -n "$LEFT$LEFT_PATHS" ]; then
    echo "new-app: template identities still present after rename:" >&2
    printf '%s\n%s\n' "$LEFT" "$LEFT_PATHS" | head >&2
    exit 1
fi

# ---- Fresh history: version 0.1.0, empty changelog.
rm -f CHANGELOG.md
printf '{\n  ".": "0.1.0"\n}\n' > .release-please-manifest.json
perl -pi -e "s/VERSION(\s*)= '[0-9.]+'/VERSION\${1}= '0.1.0'/" Jenkinsfile.dotnet Jenkinsfile.windows
perl -pi -e 's/\$\{VERSION:-[0-9.]+\}/\${VERSION:-0.1.0}/g' docker-compose.yml

cat >> AGENTS.md <<EOT

## Origin

Created from arcana-windows @ \`${SRC_SHA}\` by \`tools/new-app.sh\` (solution prefix, namespaces,
assemblies, MSIX identity, app-data folder, Jenkins APP_NAME, image and Sonar names renamed to
\`${NAME}\` / \`${KEBAB}\`). The layers, rules and definition of done above apply unchanged.
Remove template features you do not need through DI registration and plugin registration
(\`App.xaml.cs\`), not by flattening.
EOT

git init -q
tools/install-hooks.sh >/dev/null
git add -A
# --no-verify only for this import commit: every file counts as "added" here, so the hook
# would judge the template's own legacy root files (App.xaml.cs, MainWindow.xaml.cs,
# GlobalSuppressions.cs) as new files outside a layer. The template itself passes
# arch-check; every later commit in the product goes through the hook.
git -c user.name="${GIT_AUTHOR_NAME:-new-app}" -c user.email="${GIT_AUTHOR_EMAIL:-new-app@localhost}" \
    commit -q --no-verify -m "chore: start ${NAME} from arcana-windows ${SRC_SHA}"

echo "created $DEST (${NAME}) from arcana-windows ${SRC_SHA}; hooks: $(git config core.hooksPath)"
echo "next: cd $DEST && tools/new-feature.sh <Name> && tools/arch-check.sh"
