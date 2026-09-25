# Auto Caravan Equip - Vehicle Framework Compatibility

A small RimWorld 1.6 patch mod. It gets [Auto Caravan Equip](https://steamcommunity.com/sharedfiles/filedetails/?id=1487284611)
(the Auto Seller extension) working again when you form caravans with
[Vehicle Framework](https://github.com/SmashPhil/Vehicle-Framework) installed.

## The problem

In vanilla, opening the Form Caravan dialog goes straight into world route planning. When you
accept a route, the game calls `Dialog_FormCaravan.Notify_NoLongerChoosingRoute`. Auto Caravan
Equip hooks that call to show its preset picker and apply its rules.

Vehicle Framework transpiles `Dialog_FormCaravan.PostOpen` so the dialog opens directly on its
Vehicles tab. It replaces the `WorldRoutePlanner.Start` call with its own `SetInitialTab`. The
notification never fires on open, so Auto Caravan Equip never runs and you get the plain dialog.

## The fix

- Postfix on Vehicle Framework's `SetInitialTab` marks the dialog, because that's exactly where
  vanilla would have started route planning.
- Postfix on `Dialog_FormCaravan.PostOpen` (runs last) calls Auto Caravan Equip's `FirstCar`,
  which shows the preset picker or applies your rules.
- Postfix on `Notify_NoLongerChoosingRoute` calls Auto Caravan Equip's `PostCarRoute` the first
  time a real destination is picked, so days-of-travel rules for food, medicine and so on still
  apply.

Everything is looked up by name through reflection. No other mod's DLL is referenced or shipped.
If either mod renames these methods, the patch logs a warning and disables itself instead of
erroring.

## Install

Copy the repo folder into `RimWorld/Mods/`. It needs `About/`, `LoadFolders.xml` and
`1.6/Assemblies/`. Load it after Harmony, Vehicle Framework, Auto Seller and Auto Caravan Equip.

## Build

```
dotnet build Source/AutoCaravanEquipVehicleCompat -c Release
```

The output goes to `1.6/Assemblies/`. Reference assemblies come from NuGet (`Krafs.Rimworld.Ref`,
`Lib.Harmony`), so you don't need a RimWorld install to build.
