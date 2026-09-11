namespace BuildableRover
{
	// Mirrors the shape of the game's own STRINGS.BUILDINGS.PREFABS.<ID>
	// classes (see STRINGS/BUILDINGS.cs, e.g. SCOUTMODULE) - NAME/DESC/
	// EFFECT is the exact field set BuildingTemplates expects to find at
	// "STRINGS.BUILDINGS.PREFABS.BuildableRover.<field>". We can't add a
	// nested class onto the game's own (non-partial) STRINGS.BUILDINGS.
	// PREFABS type, so this lives in our own namespace and gets wired to
	// those exact keys at runtime instead - see Mod.cs.
	public static class BUILDABLEROVER
	{
		public static LocString NAME = "Rover";

		public static LocString DESC =
			"A ground-built rover robot.";

		public static LocString EFFECT =
			"Build a Rover on the spot without using a rocket module.";
	}
}
