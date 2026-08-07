using Newtonsoft.Json;
using PeterHan.PLib.Options;

namespace HideUIButtons
{
	[JsonObject(MemberSerialization.OptIn)]
	[ModInfo("https://github.com/Kupie/Berkays_OniMods")]
	public class ToolbarButtonOptions
	{
		[Option("Hide Dig", "Hides the Dig button.", "Basic Tools")]
		[JsonProperty]
		public bool HideDig { get; set; }

		[Option("Hide Cancel", "Hides the Cancel button.", "Basic Tools")]
		[JsonProperty]
		public bool HideCancel { get; set; }

		[Option("Hide Deconstruct", "Hides the Deconstruct button.", "Basic Tools")]
		[JsonProperty]
		public bool HideDeconstruct { get; set; }

		[Option("Hide Prioritize", "Hides the Prioritize button.", "Basic Tools")]
		[JsonProperty]
		public bool HidePrioritize { get; set; }

		[Option("Hide Disinfect", "Hides the Disinfect button.", "Small Tools")]
		[JsonProperty]
		public bool HideDisinfect { get; set; }

		[Option("Hide Mark For Storage", "Hides the Mark For Storage button.", "Small Tools")]
		[JsonProperty]
		public bool HideMarkForStorage { get; set; }

		[Option("Hide Attack", "Hides the Attack button.", "Small Tools")]
		[JsonProperty]
		public bool HideAttack { get; set; }

		[Option("Hide Mop", "Hides the Mop button.", "Small Tools")]
		[JsonProperty]
		public bool HideMop { get; set; }

		[Option("Hide Capture", "Hides the Capture button.", "Small Tools")]
		[JsonProperty]
		public bool HideCapture { get; set; }

		[Option("Hide Harvest", "Hides the Harvest button.", "Small Tools")]
		[JsonProperty]
		public bool HideHarvest { get; set; }

		[Option("Hide Empty Pipe", "Hides the Empty Pipe button.", "Small Tools")]
		[JsonProperty]
		public bool HideEmptyPipe { get; set; }

		[Option("Hide Disconnect", "Hides the Disconnect button.", "Small Tools")]
		[JsonProperty]
		public bool HideDisconnect { get; set; }
	}
}
