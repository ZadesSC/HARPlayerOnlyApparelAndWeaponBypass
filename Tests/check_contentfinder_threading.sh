#!/usr/bin/env bash
set -euo pipefail

source_file="Source/HARPlayerOnlyApparelAndWeaponBypass.cs"

helpers_block="$(awk '
    /internal static class Helpers/ { in_helpers = 1 }
    in_helpers { print }
    in_helpers && /^    }$/ { exit }
' "$source_file")"

if grep -q 'ContentFinder<Texture2D>\.Get' <<<"$helpers_block"; then
    echo "Helpers must not load textures with ContentFinder; it is used by non-UI patches." >&2
    exit 1
fi

if ! grep -q 'internal static class BypassTex' "$source_file"; then
    echo "Expected a dedicated BypassTex texture holder." >&2
    exit 1
fi

if ! grep -q 'BypassTex\.Preload();' "$source_file"; then
    echo "Expected Bootstrap to preload BypassTex on startup." >&2
    exit 1
fi

if ! grep -q 'icon = BypassTex\.ApparelBypassIcon' "$source_file"; then
    echo "Expected apparel gizmo to use BypassTex.ApparelBypassIcon." >&2
    exit 1
fi

if ! grep -q 'icon = BypassTex\.WeaponBypassIcon' "$source_file"; then
    echo "Expected weapon gizmo to use BypassTex.WeaponBypassIcon." >&2
    exit 1
fi
