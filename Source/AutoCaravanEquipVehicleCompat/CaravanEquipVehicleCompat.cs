using System;
using System.Reflection;
using HarmonyLib;
using RimWorld;
using RimWorld.Planet;
using Verse;

namespace AutoCaravanEquipVehicleCompat;

/// <summary>
/// Auto Caravan Equip runs its presets and rules from a postfix on
/// <see cref="Dialog_FormCaravan.Notify_NoLongerChoosingRoute"/>, relying on vanilla opening the
/// world route planner as soon as the dialog opens. Vehicle Framework transpiles that call out of
/// <see cref="Dialog_FormCaravan.PostOpen"/> (replacing it with its own SetInitialTab), so the
/// notification never fires on open and Auto Caravan Equip never engages.
///
/// This patch hooks the spot Vehicle Framework replaced, runs Auto Caravan Equip's "first open"
/// logic once the dialog has finished opening, and applies its route-dependent rules the first time
/// a destination is actually chosen.
/// </summary>
[StaticConstructorOnStartup]
public static class CaravanEquipVehicleCompat
{
  private const string HarmonyId = "kanid99.AutoCaravanEquip.VehicleFrameworkCompat";
  private const string LogPrefix = "[Auto Caravan Equip - Vehicle Framework Compat] ";

  private static readonly AccessTools.FieldRef<Dialog_FormCaravan, PlanetTile> DestinationTile =
    AccessTools.FieldRefAccess<Dialog_FormCaravan, PlanetTile>("destinationTile");

  private static readonly AccessTools.FieldRef<Dialog_FormCaravan, bool> ChoosingRoute =
    AccessTools.FieldRefAccess<Dialog_FormCaravan, bool>("choosingRoute");

  private static readonly MethodInfo NotifyTransferablesChanged =
    AccessTools.Method(typeof(Dialog_FormCaravan), "Notify_TransferablesChanged");

  private static Action<Dialog_FormCaravan> caravanEquipFirstOpen;
  private static Action<Dialog_FormCaravan> caravanEquipRouteChosen;

  // Dialog whose vanilla route-planner start was skipped by Vehicle Framework during PostOpen.
  private static Dialog_FormCaravan pendingOpen;

  // Dialog still waiting for Auto Caravan Equip's days-of-travel rules to be applied.
  private static Dialog_FormCaravan pendingRouteRules;

  static CaravanEquipVehicleCompat()
  {
    MethodInfo setInitialTab =
      AccessTools.Method("Vehicles.Patch_FormCaravanDialog:SetInitialTab");
    MethodInfo firstCar = AccessTools.Method("RWAutoCaravanEquip.Patcher:FirstCar");
    MethodInfo postCarRoute = AccessTools.Method("RWAutoCaravanEquip.Patcher:PostCarRoute");

    if (setInitialTab == null || firstCar == null || postCarRoute == null)
    {
      Log.Warning(LogPrefix + "Could not find the expected Vehicle Framework or Auto Caravan " +
        $"Equip methods (SetInitialTab: {setInitialTab != null}, FirstCar: {firstCar != null}, " +
        $"PostCarRoute: {postCarRoute != null}). One of the mods may have updated; patch disabled.");
      return;
    }

    caravanEquipFirstOpen = AccessTools.MethodDelegate<Action<Dialog_FormCaravan>>(firstCar);
    caravanEquipRouteChosen = AccessTools.MethodDelegate<Action<Dialog_FormCaravan>>(postCarRoute);

    Harmony harmony = new(HarmonyId);
    harmony.Patch(setInitialTab,
      postfix: new HarmonyMethod(typeof(CaravanEquipVehicleCompat), nameof(RouteStartSkipped)));
    harmony.Patch(AccessTools.Method(typeof(Dialog_FormCaravan), nameof(Dialog_FormCaravan.PostOpen)),
      postfix: new HarmonyMethod(typeof(CaravanEquipVehicleCompat), nameof(PostOpen))
      {
        priority = Priority.Last,
      });
    harmony.Patch(
      AccessTools.Method(typeof(Dialog_FormCaravan),
        nameof(Dialog_FormCaravan.Notify_NoLongerChoosingRoute)),
      postfix: new HarmonyMethod(typeof(CaravanEquipVehicleCompat), nameof(NoLongerChoosingRoute)));
    harmony.Patch(AccessTools.Method(typeof(Dialog_FormCaravan), nameof(Dialog_FormCaravan.PostClose)),
      postfix: new HarmonyMethod(typeof(CaravanEquipVehicleCompat), nameof(PostClose)));
  }

  /// <summary>
  /// Runs where vanilla would have started the world route planner, which is the moment Auto
  /// Caravan Equip expects to hook into.
  /// </summary>
  private static void RouteStartSkipped(Dialog_FormCaravan formCaravan)
  {
    pendingOpen = formCaravan;
  }

  private static void PostOpen(Dialog_FormCaravan __instance)
  {
    if (pendingOpen != __instance)
      return;

    pendingOpen = null;
    pendingRouteRules = __instance;
    try
    {
      caravanEquipFirstOpen(__instance);
      NotifyTransferablesChanged?.Invoke(__instance, null);
    }
    catch (Exception ex)
    {
      Log.Error(LogPrefix + "Failed to apply Auto Caravan Equip on dialog open: " + ex);
    }
  }

  /// <summary>
  /// Vanilla applies Auto Caravan Equip's route rules right after the initial route is chosen.
  /// With Vehicle Framework the route is chosen later, so apply them the first time a real
  /// destination exists.
  /// </summary>
  private static void NoLongerChoosingRoute(Dialog_FormCaravan __instance)
  {
    if (pendingRouteRules != __instance)
      return;

    PlanetTile destination = DestinationTile(__instance);
    if (!destination.Valid || destination == __instance.CurrentTile)
      return;

    pendingRouteRules = null;
    try
    {
      caravanEquipRouteChosen(__instance);
    }
    catch (Exception ex)
    {
      Log.Error(LogPrefix + "Failed to apply Auto Caravan Equip route rules: " + ex);
    }
  }

  private static void PostClose(Dialog_FormCaravan __instance)
  {
    // The dialog is temporarily closed while a route is being picked; only forget it for good.
    if (ChoosingRoute(__instance))
      return;

    if (pendingOpen == __instance)
      pendingOpen = null;
    if (pendingRouteRules == __instance)
      pendingRouteRules = null;
  }
}
