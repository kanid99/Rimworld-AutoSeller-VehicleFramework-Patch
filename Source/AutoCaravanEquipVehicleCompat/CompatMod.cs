using UnityEngine;
using Verse;

namespace AutoCaravanEquipVehicleCompat;

/// <summary>
/// The mod settings page. It only shows the installed build - About.xml's modVersion,
/// 0.9.(commit count) like every SloppyMods mod - so a bug report can name it.
/// </summary>
public class CompatMod : Mod
{
  public CompatMod(ModContentPack content) : base(content)
  {
  }

  public override string SettingsCategory() => "Auto Caravan Equip - Vehicle Framework Compat";

  public override void DoSettingsWindowContents(Rect inRect)
  {
    Listing_Standard list = new Listing_Standard();
    list.Begin(inRect);
    list.Label("Build " + Content.ModMetaData.ModVersion);
    list.End();
  }
}
