using System;
using System.IO;
using System.Reflection;
using HarmonyLib;
using KMod;
using PeterHan.PLib.Core;
using PeterHan.PLib.Options;
using STRINGS;
using UnityEngine;

namespace BuildOverlappingBuildings
{

    [RestartRequired]
    public sealed class BuildOverlappingBuildingsOptions
    {
        [Option("Overlap Override Key", "Hold this key while placing a building to ignore the occupied location warning.")]
        public KeyCode AllowOverlapKey { get; set; } = KeyCode.Semicolon;
    }

    public static class KupieLogging
    {
        public static void KupieLog(string message)
        {
            var assemblyFolder = Path.GetDirectoryName(Assembly.GetExecutingAssembly().Location);
            if (assemblyFolder == null)
            {
                return;
            }

            if (File.Exists(Path.Combine(assemblyFolder, "KupieLog_Enable.txt")))
            {
                File.AppendAllText(Path.Combine(assemblyFolder, "KupieLog.txt"), message + Environment.NewLine);
            }
        }
    }

    [HarmonyPatch(typeof(BuildingDef),
         nameof(BuildingDef.IsAreaClear),
         new Type[] {
            typeof(GameObject),
            typeof(int),
            typeof(Orientation),
            typeof(ObjectLayer),
            typeof(ObjectLayer),
            typeof(bool),
            typeof(bool),
            typeof(string),
            typeof(bool)},
         new ArgumentType[] {
            ArgumentType.Normal,
            ArgumentType.Normal,
            ArgumentType.Normal,
            ArgumentType.Normal,
            ArgumentType.Normal,
            ArgumentType.Normal,
            ArgumentType.Normal,
            ArgumentType.Out,
            ArgumentType.Normal
         }
     )]
    public static class Patches
    {
        private static int lastForcedRefreshFrame = -1;
        static void Postfix(ref bool __result, string fail_reason)
        {
            var overlapKey = Mod.Options?.AllowOverlapKey ?? KeyCode.Semicolon;
            bool wantsOverlap = Input.GetKey(overlapKey) || OniTogetherBridge.IncomingPlacement;

            if (!__result && fail_reason == UI.TOOLTIPS.HELP_BUILDLOCATION_OCCUPIED && wantsOverlap)
            {
                KupieLogging.KupieLog("IsAreaClear Postfix ran!");
                __result = true;
            }

            // Ghost refresh only makes sense for your own local key-held aiming - untouched,
            // still gated on Input.GetKeyDown/Up, never fires for a remote placement.
            if (Time.frameCount != lastForcedRefreshFrame
                && (Input.GetKeyDown(overlapKey) || Input.GetKeyUp(overlapKey))
                && BuildTool.Instance != null && BuildTool.Instance.active
                && Grid.IsValidBuildingCell(BuildTool.Instance.lastCell))
            {
                lastForcedRefreshFrame = Time.frameCount;
                Vector3 pos = Grid.CellToPosCCC(BuildTool.Instance.lastCell, Grid.SceneLayer.Building);
                BuildTool.Instance.UpdateVis(pos);
            }
        }
    }
}