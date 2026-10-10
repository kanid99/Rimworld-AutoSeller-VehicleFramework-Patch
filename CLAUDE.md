# Repo rules

## Branches and builds

Every update is a numbered build:

1. Pick the next build number: one higher than the latest entry in `CHANGELOG.md`.
2. Add a `## Build N` entry at the top of `CHANGELOG.md` saying what changed.
3. Rebuild `1.6/Assemblies/` (`dotnet build Source/AutoCaravanEquipVehicleCompat -c Release`) and
   commit the source, DLL and changelog together.
4. Push that commit to a new branch `build/N`. It's a frozen snapshot: never push to it again.
5. Push the same commit to `main`.

`main` always holds the latest build. To test an older build, check out its `build/N` branch.
