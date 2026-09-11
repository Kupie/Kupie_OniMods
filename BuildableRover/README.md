# Rover Depot

Build a Rover on the ground, on your starting planetoid, with no rocket and
no trip to orbit.

## What it does

Adds a "Rover Depot" building (Rocketry build-menu tab, unlocked by the
**BasicRocketry** tech - the same one that unlocks CommandModule and
SteamEngine, i.e. the first tech that lets you build a rocket at all) that
costs the same **500kg of a single raw metal** the vanilla Rover's Module
wants. Build it like any other building - a Duplicant hauls the metal and
constructs it normally. The instant construction finishes, a `ScoutRover`
(the same Rover prefab Spaced Out's Rover's Module produces) appears where
the Depot was, colored the same metal it was built from, and the Depot
itself deletes itself. No Storage, no internal "lander" object, no deploy
button, no Clustercraft/orbit dependency at all.

## How it works

- `BuildableRoverConfig.cs` - a completely normal `IBuildingConfig`. The only
  non-default things it does: pass `new float[] { 500f }` as the
  construction mass (instead of a `TUNING.BUILDINGS.CONSTRUCTION_MASS_KG`
  tier, to hit exactly 500kg) with `MATERIALS.RAW_METALS` as the material
  choices, and call `ModUtil.AddBuildingToPlanScreen` to slot it into the
  Rocketry tab.
- `RoverSpawner.cs` - the only custom component. Its `OnSpawn()` only fires
  once the building has actually finished construction (the "under
  construction" ghost is a different, separate object in ONI's building
  pipeline), so by the time it runs, the metal has already been delivered
  and consumed by the normal Build chore. It reads the completed building's
  `PrimaryElement.ElementID` (whatever metal the Duplicant actually built it
  from), spawns a `ScoutRover` with that same element, and deletes the
  Depot. The spawn is deferred one tick via `GameScheduler` so we're not
  deleting this GameObject while sibling components are still mid-`OnSpawn`.
- `Strings.cs` / `Mod.cs` - `BuildingTemplates` looks up display text by
  convention from the building ID (`STRINGS.BUILDINGS.PREFABS.BuildableRover.NAME`
  / `.DESC` / `.EFFECT`), but nothing populates those keys just by defining a
  building - that's why they showed up as `MISSING.STRINGS...` in the build
  menu. `BuildableRover` in `Strings.cs` mirrors the shape of the game's own
  `STRINGS.BUILDINGS.PREFABS.<ID>` classes (we can't literally add a nested
  class onto the game's non-partial one), and `Mod.OnLoad` calls
  `LocString.CreateLocStringKeys` - the same reflection-based registration
  the game uses internally - to wire those fields to the exact keys
  `BuildingTemplates` expects.
- `Patches.cs` - the one Harmony patch this mod needs. `ModUtil` has a
  built-in helper for adding a building to the build menu
  (`AddBuildingToPlanScreen`, used in `BuildableRoverConfig`), but nothing for
  "add this building to an existing tech's unlocks" - so gating BuildableRover
  behind `BasicRocketry` means Postfixing `Database.Techs`'s constructor
  (where every tech's `unlockedItemIDs` list gets built) and calling
  `Tech.AddUnlockedItemIDs("BuildableRover")` on the `BasicRocketry` instance.
  Guarded on `DlcManager.IsExpansion1Active()` since the building itself
  doesn't exist otherwise.
- No PLib, no Publicizer - everything used here is a public API (confirmed
  against your `ONI_Decomp` checkout: `ScoutModuleConfig`, `RationBoxConfig`,
  `BuildingInternalConstructor`, `ModUtil`, `GameScheduler`, `Database.Techs`,
  `LocString`).

## Things worth double-checking once it's compiled

I don't have your actual game DLLs to compile against - everything above is
checked against your `ONI_Decomp` decompile, not a live build, so treat this
as a solid first draft rather than a finished, tested mod:

- **`"Rocketry"` as the PlanScreen category** - confirmed as a real category
  string elsewhere in the decompiled source, but I didn't verify subcategory
  names, so it'll land in "uncategorized" within that tab. Cosmetic only.
- **DLC gate** - `GetRequiredDlcIds()` returns `DlcManager.EXPANSION1`
  (Spaced Out), matching `ScoutModuleConfig`. If that constant's named
  differently in your build, the compiler will tell you immediately.
- **`"BasicRocketry"` as the gating tech** - matches the tech that unlocks
  `CommandModule`/`SteamEngine` in your `ONI_Decomp` checkout. If your build
  renamed it, `Techs_Constructor_Patch.Postfix`'s `TryGet` call will just
  silently no-op (it null-checks) - the building will fall back to
  unlocked-from-the-start, not throw.
- **`"scout_bot_kanim"` as the building's ghost/icon** - confirmed and fixed:
  `BuildingDef.DefaultAnimState` defaults to `"off"`, which doesn't exist in
  a creature kanim, so `CreateBuildingDef` now explicitly sets it to
  `"idle_loop"` (the same state `BaseRoverConfig` itself uses for the
  Rover's idle animation). The build-menu icon worked before this fix
  because it's a baked snapshot sprite, unrelated to live anim-state
  playback - the ghost and the completed building's initial pose aren't.

## Wiring into Kupie_OniMods

Checked against your latest push (the one that added `VSyncFpsLimiter` and
moved `UnityEngine.UI`/`Unity.TextMeshPro` to unconditional references in
`Directory.Build.props`).

1. Drop the `BuildableRover/` folder in alongside your other mod projects.
2. Overwrite your root `OniMods.sln` with the one included here - I added
   `BuildableRover`'s project entry (new GUID `4D424DE7-A40D-40EB-AD22-4D3865F4847F`)
   after `VSyncFpsLimiter` and its six `Debug|Release x Any CPU|x64|x86`
   lines in `ProjectConfigurationPlatforms`, same as every other project.
   `dotnet sln OniMods.sln add BuildableRover/BuildableRover.csproj` does the exact
   same thing if you'd rather run it than diff the file.
3. No `Directory.Build.props` changes needed. BuildableRover doesn't need
   `UsePLib`, `UsePublicizer`, `UseUnityInputModule`, or `UseOniTogetherApi` -
   it only touches public `IBuildingConfig`/`Storage`/`PrimaryElement`-level
   APIs, nothing private/internal, so it just rides the default
   Harmony/Assembly-CSharp/UnityEngine(+UI/TextMeshPro) references every
   project gets now.
