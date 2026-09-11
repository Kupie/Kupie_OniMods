# VSync & FPS Limiter

Adds a "VSync" toggle and an "FPS Limit" cycle button to the DISPLAY section
of the in-game Graphics Options screen, directly after "Low Resolution
Textures" and before the "INTERFACE" header. Both apply immediately on click
and persist across restarts (no Apply/Revert dialog - unlike resolution
changes, these are always safely and instantly reversible).

FPS Limit cycles: Unlimited -> 30 -> 60 -> 120 -> 144 -> 240 -> back to
Unlimited. Turning VSync on will override any FPS cap while it's active,
since Unity ignores `Application.targetFrameRate` whenever `vSyncCount > 0` -
that's expected, not a bug.

## Dependencies

None beyond what `Directory.Build.props` already supplies unconditionally
(`0Harmony`, `Assembly-CSharp`, `UnityEngine`, `UnityEngine.CoreModule`,
`System`). No PLib, no Newtonsoft.Json, no Unity UI/InputModule, no
Publicizer - none of the opt-in switches in `Directory.Build.props` need to
be turned on for this project.

## Verified against

`Kupie/ONI_Decomp` @ `8d4ace1` (2026-08-07) - `GraphicsOptionsScreen`,
`MultiToggle`, `KPlayerPrefs`, `Util.KInstantiateUI`, and
`LaunchInitializer`/`SetSettingsFromPrefs`. That repo is C# source only, so
the actual Unity scene/prefab hierarchy for the Graphics Options screen
couldn't be inspected directly - see the comment above
`GraphicsOptionsScreen_OnSpawn_Patch` in `Patches.cs` for how the row/header
lookup handles that, and what to check in the player log if the inserted
rows land in the wrong spot in-game.

`minimumSupportedBuild: 744825` in `mod_info.yaml` matches this same decomp
snapshot, same as `QuickReloadButton`.
