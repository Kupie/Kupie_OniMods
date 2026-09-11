using HarmonyLib;
using KMod;

namespace VSyncFpsLimiter
{
	public sealed class Mod : UserMod2
	{
		// No options/PLib needed - base.OnLoad() already does
		// harmony.PatchAll(this.assembly), which picks up Patches.cs.
	}
}
