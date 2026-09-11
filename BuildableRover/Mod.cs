using HarmonyLib;
using KMod;

namespace BuildableRover
{
	public sealed class Mod : UserMod2
	{
		public override void OnLoad(Harmony harmony)
		{
			base.OnLoad(harmony);

			// base.OnLoad already ran harmony.PatchAll(this.assembly),
			// which picked up the tech-gating patch in Patches.cs.
			//
			// CreateLocStringKeys walks BuildableRover (Strings.cs) via
			// reflection and registers each LocString under
			// "STRINGS.BUILDINGS.PREFABS.BuildableRover.<field name>" - the
			// exact keys BuildingTemplates looks up by convention from the
			// building's ID, same mechanism the game's own generated
			// STRINGS.BUILDINGS.PREFABS classes use.
			LocString.CreateLocStringKeys(typeof(BUILDABLEROVER), "STRINGS.BUILDINGS.PREFABS.");
		}
	}
}
