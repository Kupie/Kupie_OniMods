using STRINGS;
using TUNING;
using UnityEngine;

namespace BuildableRover
{
	// A ground-buildable stand-in for the vanilla Rover's Module + Rover's
	// Lander (Spaced Out DLC). It costs the same 500kg of a chosen raw
	// metal that a real Rover's Module wants, but there's no rocket, no
	// orbit, and no lander: build it like any other building, and the
	// instant a Duplicant finishes construction, a Rover is standing where
	// it was built (see RoverSpawner.cs).
	public class BuildableRoverConfig : IBuildingConfig
	{
		public const string ID = "BuildableRover";

		// ScoutRover / the rest of the Rover machinery only exist when the
		// Spaced Out DLC is enabled - same requirement the vanilla
		// Rover's Module (ScoutModuleConfig) has.
		public override string[] GetRequiredDlcIds()
		{
			return DlcManager.EXPANSION1;
		}

		public override BuildingDef CreateBuildingDef()
		{
			string id = ID;
			int width = 1;
			int height = 2;
			// The actual Rover's own art, not the cargo module it used to
			// ride down in - this building only exists for a moment before
			// RoverSpawner replaces it, so it should look like what it's
			// about to become.
			string anim = "buildable_rover_kanim";
			int hitpoints = 200;
			float constructionTime = 90f;

			// This is the actual "cost" of the Rover: 500kg of a single
			// raw metal, exactly like the 500kg the vanilla Rover's
			// Module wants loaded into it. Passing the mass directly
			// (rather than one of the TUNING.BUILDINGS.CONSTRUCTION_MASS_KG
			// tiers) keeps it an exact match instead of a rounded tier.
			float[] constructionMass = new float[] { 500f };
			string[] constructionMaterials = MATERIALS.RAW_METALS;

			float meltingPoint = 9999f;
			BuildLocationRule buildLocationRule = BuildLocationRule.OnFloor;
			EffectorValues noise = NOISE_POLLUTION.NONE;

			BuildingDef def = BuildingTemplates.CreateBuildingDef(
				id,
				width,
				height,
				anim,
				hitpoints,
				constructionTime,
				constructionMass,
				constructionMaterials,
				meltingPoint,
				buildLocationRule,
				global::TUNING.BUILDINGS.DECOR.NONE,
				noise,
				0.2f);

			def.Overheatable = false;
			def.Floodable = false;
			def.RequiresPowerInput = false;
			def.AddSearchTerms(SEARCH_TERMS.ROBOT);

			// scout_bot_kanim is a creature kanim (idle_loop, walk, ...),
			// not a building kanim - it has no "off" state, which is what
			// BuildingDef.DefaultAnimState defaults to. Without this, both
			// the build ghost and the completed building's initial pose
			// try to play a state that doesn't exist. idle_loop matches
			// what BaseRoverConfig itself uses for the Rover's own idle
			// animation, so it's guaranteed to be a real state in this
			// kanim.
			def.DefaultAnimState = "idle_loop";

			// Adds this building to the build menu next to the real
			// rocket parts. See Patches.cs for the tech-tree gating -
			// this building is unlocked by the BasicRocketry tech, same
			// as CommandModule/SteamEngine.
			ModUtil.AddBuildingToPlanScreen(
				new HashedString("Utilities"),
				id,
				TUNING.BUILDINGS.PlanSubcategoryName.sanitation.ToString(),
				"SweepBotStation",
				ModUtil.BuildingOrdering.After
			);

			return def;
		}

		public override void ConfigureBuildingTemplate(GameObject go, Tag prefab_tag)
		{
			Prioritizable.AddRef(go);

			// No storage, no internal constructor chore - the normal
			// Constructable/Build chore delivers the 500kg of metal, and
			// RoverSpawner takes over the instant that finishes.
			go.AddOrGet<RoverSpawner>();
		}

		// IBuildingConfig declares this abstract (it's an "I"-prefixed
		// abstract class in this codebase, not a real interface - every
		// method needs an override, even ones with nothing to do).
		// Nothing needed post-configure here: RoverSpawner does everything
		// once the building is complete.
		public override void DoPostConfigureComplete(GameObject go)
		{
		}
	}
}
