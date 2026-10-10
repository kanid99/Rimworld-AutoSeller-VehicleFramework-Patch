# Repo rules

## Build numbers

Builds are numbered `0.9.N`, where N is the commit count on `main` including the build's own
commit (`git rev-list --count HEAD` after committing), the same scheme as the other SloppyMods
mods. The number appears in three places, which must match:

- `<modVersion>` in `About/About.xml` (the in-game settings page reads it from there)
- the `Build 0.9.N` line at the top of the `About.xml` description
- the `## Build 0.9.N` entry at the top of `CHANGELOG.md`

## Shipping an update

1. Make the change, then rebuild `1.6/Assemblies/` with
   `dotnet build Source/AutoCaravanEquipVehicleCompat -c Release`.
2. Work out N for the commit you're about to make, update the three places above, and commit
   the source, DLL, About.xml and changelog together.
3. Check `git rev-list --count HEAD` equals N.
4. Push that commit to a new branch `build/0.9.N`. It's a frozen snapshot: never push to it again.
5. Push the same commit to `main`.

`main` always holds the latest build. To test an older build, check out its `build/0.9.N` branch.
