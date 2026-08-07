using HarmonyLib;
using PeterHan.PLib.Options;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

namespace HideUIButtons
{
	[HarmonyPatch(typeof(ToolMenu), "OnSpawn")]
	public static class ToolMenu_OnSpawn_HideButtons_Patch
	{
		public static void Postfix(ToolMenu __instance)
		{
			ToolbarButtonOptions options = POptions.ReadSettings<ToolbarButtonOptions>() ?? new ToolbarButtonOptions();

			Dictionary<string, bool> hideMap = new Dictionary<string, bool>
			{
				{ "DigTool", options.HideDig },
				{ "CancelTool", options.HideCancel },
				{ "DeconstructTool", options.HideDeconstruct },
				{ "PrioritizeTool", options.HidePrioritize },
				{ "DisinfectTool", options.HideDisinfect },
				{ "ClearTool", options.HideMarkForStorage },
				{ "AttackTool", options.HideAttack },
				{ "MopTool", options.HideMop },
				{ "CaptureTool", options.HideCapture },
				{ "HarvestTool", options.HideHarvest },
				{ "EmptyPipeTool", options.HideEmptyPipe },
				{ "DisconnectTool", options.HideDisconnect },
			};

			HashSet<RectTransform> touchedParents = new HashSet<RectTransform>();

			foreach (ToolMenu.ToolCollection tc in __instance.basicTools)
			{
				if (tc.tools.Count == 0 || tc.toggle == null)
				{
					continue;
				}

				bool hide;
				if (hideMap.TryGetValue(tc.tools[0].toolName, out hide) && hide)
				{
					tc.toggle.SetActive(false);
					RectTransform parent = tc.toggle.transform.parent as RectTransform;
					if (parent != null)
					{
						touchedParents.Add(parent);
					}
				}
			}

			foreach (RectTransform parent in touchedParents)
			{
				LayoutRebuilder.ForceRebuildLayoutImmediate(parent);
			}
		}
	}
}
