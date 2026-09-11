using Database;
using HarmonyLib;

namespace BuildableRover
{
	// Techs builds its whole tree - every "new Tech(id, unlockedItemIDs, ...)"
	// call - directly inside its constructor (Database/Techs.cs), so
	// Postfixing the constructor is the only hook point: there's no
	// ModUtil helper for "add this building to an existing tech's unlocks"
	// the way there is for AddBuildingToPlanScreen.
	[HarmonyPatch(typeof(Techs), nameof(Techs.Init))]
	public static class Techs_Init_Patch
	{
		public static void Postfix(Techs __instance)
		{
			// BuildableRover is Spaced Out-only, so don't modify the
			// research tree when the building isn't available.
			if (!DlcManager.IsExpansion1Active())
			{
				return;
			}

			var tech = __instance.TryGet("ArtificialFriends");

			if (tech != null && !tech.unlockedItemIDs.Contains(BuildableRoverConfig.ID))
			{
				tech.AddUnlockedItemIDs(BuildableRoverConfig.ID);
			}
		}
	}
}
