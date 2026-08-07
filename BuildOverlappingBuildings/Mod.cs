using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using HarmonyLib;
using KMod;
using PeterHan.PLib.Core;
using PeterHan.PLib.Options;

namespace BuildOverlappingBuildings
{
	public sealed class Mod : UserMod2
	{
		internal static BuildOverlappingBuildingsOptions Options { get; private set; }

		public override void OnLoad(Harmony harmony)
		{
			base.OnLoad(harmony);
			PUtil.InitLibrary();
			new POptions().RegisterOptions(this, typeof(BuildOverlappingBuildingsOptions));
			ReloadOptions();
		}

		internal static void ReloadOptions()
		{
			Options = POptions.ReadSettings<BuildOverlappingBuildingsOptions>() ?? new BuildOverlappingBuildingsOptions();
			KupieLogging.KupieLog($"Overlap key set to {Options.AllowOverlapKey}");
		}
	}

}
