# Quick Reload - as a QuickReloadButton sub-mod of Kupie_OniMods

This replaces the standalone project from before. Drop the `QuickReloadButton`
folder into the root of `Kupie_OniMods` (next to `BuildOverlappingBuildings`,
`HideUIButtons`, `MultiplayerCompatPatch`), then merge the included
`OniMods.sln` changes into the repo's real `OniMods.sln` (or just replace it
with the one here - it's the same file with one new Project block and one new
set of ProjectConfigurationPlatforms entries, both for GUID
`{81FDCF0E-A93D-4F8D-BC01-7E6EFCC9C51B}`).

## What changed vs. the standalone version

The repo's `Directory.Build.props` already supplies every reference this mod
needs unconditionally - `0Harmony`, `Assembly-CSharp`,
`Assembly-CSharp-firstpass`, `UnityEngine`, `UnityEngine.CoreModule`, `System`
- straight from `OniManagedPath` (your local
`OxygenNotIncluded_Data\Managed`, auto-detected or set in
`Directory.Build.local.props`). None of the opt-in switches
(`UsePLib`, `UseNewtonsoftJson`, `UseUnityUI`, `UsePublicizer`,
`UseOniTogetherApi`) apply to this mod, so it doesn't need to touch
`Directory.Build.props` at all. That means:

- `QuickReloadButton.csproj` shrinks to the same few lines as
  `HideUIButtons.csproj` / `BuildOverlappingBuildings.csproj` - just
  `TargetFramework`/`RootNamespace`/`AssemblyName`, plus the two
  `CopyToOutputDirectory` entries for `mod.yaml`/`mod_info.yaml`.
- `packages.config` and the standalone `Properties\AssemblyInfo.cs` are gone -
  package references are centralized in `Directory.Packages.props` (this mod
  doesn't need any), and the SDK-style project auto-generates assembly info.
- `TargetFramework` is `net48` to match the sibling projects, and the entry
  point is `Mod.cs` (`public sealed class Mod : UserMod2`) with the patch
  moved to `Patches.cs`, matching `BuildOverlappingBuildings`'s file layout.
- `mod.yaml`/`mod_info.yaml` follow the sibling mods' exact key style
  (quoted strings, `supportedContent: ALL`, `minimumSupportedBuild`,
  `version`). `minimumSupportedBuild: 744825` matches the `Kupie/ONI_Decomp`
  build this was verified against.

The actual patch logic in `Patches.cs` is unchanged from the standalone
version.

## Building

From the repo root: `dotnet build OniMods.sln` (or open it in Visual Studio),
same as the other mods. If `OniManagedPath` isn't auto-detected, set it in
`Directory.Build.local.props` per the comment at the top of
`Directory.Build.props`.
