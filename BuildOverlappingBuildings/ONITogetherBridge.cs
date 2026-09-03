using System.Reflection;
using HarmonyLib;


namespace BuildOverlappingBuildings
{
	internal static class OniTogetherBridge
	{
		// True only while this peer is actively replaying a placement someone else made.
		internal static bool IncomingPlacement;

		// Called explicitly from Mod.OnLoad, NOT discovered via [HarmonyPatch] + PatchAll.
		// A null TargetMethod() from an attribute-based patch is treated by Harmony as a
		// failure, not a skip, and PatchAll doesn't catch it - it aborts the whole mod's
		// OnLoad. Checking for null ourselves, before ever calling harmony.Patch(...), means
		// there's nothing for Harmony to throw about when ONI Together isn't installed.
		internal static void TryPatch(Harmony harmony)
		{
			var type = AccessTools.TypeByName("ONI_Together.Networking.Packets.Tools.Build.BuildPacket");
			var method = type == null ? null : AccessTools.Method(type, "OnDispatched");
			if (method == null)
				return; // ONI Together isn't installed - nothing to patch, nothing to fail

			harmony.Patch(method,
				prefix: new HarmonyMethod(typeof(OniTogetherBridge), nameof(Prefix)),
				finalizer: new HarmonyMethod(typeof(OniTogetherBridge), nameof(Finalizer)));
		}

		private static void Prefix() => IncomingPlacement = true;
		private static void Finalizer() => IncomingPlacement = false;
	}
}