# HAR Player-Only Apparel and Weapon Bypass

RimWorld 1.6 and 1.7 mod for [Humanoid Alien Races (HAR)](https://steamcommunity.com/sharedfiles/filedetails/?id=839005762).

This mod lets player-controlled colonists bypass HAR apparel and weapon race restrictions on a per-pawn basis, while leaving NPC behavior and gear generation alone.

Both game versions load the same DLL from `Assemblies/` and share `Textures/`.
`LoadFolders.xml` explicitly selects the root folder for 1.6 and 1.7. When updating,
replace the old mod folder so the previous `1.6/Assemblies` copy does not remain.

## Features

- Lets player pawns manually wear HAR-restricted apparel.
- Lets player pawns manually equip HAR-restricted weapons.
- Keeps NPC and non-player pawns on normal HAR restrictions.
- Preserves NPC spawn outfit and weapon control.
- Adds separate apparel and weapon bypass gizmos.
- Supports default on/off settings for each bypass type.


## Development packages and releases

Actions → **Build dev package** validates pushes and pull requests to `master`
and `dev`, and supports manual runs. Download its artifact and extract the mod
ZIP inside it; replace your previous mod folder when installing. Packages support
RimWorld 1.6 and 1.7. Install Harmony and Humanoid Alien Races separately.

Actions → **Create release** publishes a stable release from `master`, or a
prerelease from `master` or `dev`. Leave the version blank to allocate the next
Los Angeles calendar version (`vYYYY.MM.DD[.N]`, or `-beta.N` for prereleases).
Pushing a matching `v*` tag also publishes a release. Development builds only
upload artifacts and do not publish releases.

The workflows use the [shared RimWorld build system](https://github.com/ZadesSC/HighDensityHydroponics/blob/master/docs/rimworld-build-system.md),
pinned to `25c60977b5dbd240e3624ea963e734699139cb43`. Runtime files are allowlisted
in `.github/rimworld-build.json`; source, tests, Workshop artwork, and dependency
DLLs are excluded. ZIPs include the license; SHA256SUMS accompanies the ZIP.

This repository uses **committed DLL mode**, not CI compilation. The initial
fingerprint records the existing source and DLL baseline; it does not prove
that the DLL was compiled from that source. After changing source, rebuild and
validate the DLL against RimWorld 1.6, Harmony, and HAR, then refresh the
fingerprint with the pinned shared helper and commit both together:

```sh
python3 /path/to/shared/tools/ci/rimworld_build.py fingerprint --root .
python3 /path/to/shared/tools/ci/rimworld_build.py build --root .
```

CI rejects source or DLL changes that do not match the committed fingerprint.
Never refresh it just to bypass an unexplained failure. On branches containing it (currently `dev`),
`bash Tests/check_contentfinder_threading.sh` also runs for every package.
