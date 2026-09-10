using System.Collections.Generic;
using System.IO;
using HarmonyLib;
using Klei;
using STRINGS;
using UnityEngine.Events;

namespace QuickReloadButton
{
	// PauseScreen rebuilds its entire button list from scratch every time it's
	// shown (and once more after a manual save, to flip the Save button's
	// label). Postfixing ConfigureButtonInfos lets us slot our button into
	// that fresh list every time, right after Load and before Options.
	[HarmonyPatch(typeof(PauseScreen), "ConfigureButtonInfos")]
	public static class PauseScreen_ConfigureButtonInfos_Patch
	{
		public static void Postfix(PauseScreen __instance)
		{
			// The demo build's pause menu has no Save/Load buttons at all, so
			// there's nothing for Quick Reload to do there.
			if (GenericGameSettings.instance.demoMode)
			{
				return;
			}

			Traverse buttonsField = Traverse.Create(__instance).Field("buttons");
			IList<KButtonMenu.ButtonInfo> buttons = buttonsField.GetValue<IList<KButtonMenu.ButtonInfo>>();
			if (buttons == null)
			{
				return;
			}

			List<KButtonMenu.ButtonInfo> updated = new List<KButtonMenu.ButtonInfo>(buttons);
			int loadIndex = updated.FindIndex((b) => b.text == UI.FRONTEND.PAUSE_SCREEN.LOAD);
			if (loadIndex < 0)
			{
				return;
			}

			updated.Insert(loadIndex + 1, new KButtonMenu.ButtonInfo(
				"Quick Reload",
				global::Action.NumActions,
				new UnityAction(() => OnQuickReload(__instance)),
				null,
				null));

			buttonsField.SetValue(updated);
		}

		private static void OnQuickReload(PauseScreen instance)
		{
			string filename = SaveLoader.GetActiveSaveFilePath();
			if (string.IsNullOrEmpty(filename) || !File.Exists(filename))
			{
				// Nothing saved yet for this colony - there's no save to reload.
				return;
			}

			// Hide the pause menu while we save, same as the real Save button
			// does, so a second click can't queue up a second save.
			instance.gameObject.SetActive(false);
			try
			{
				SaveLoader.Instance.Save(filename, false, true);
			}
			catch (IOException)
			{
				// Save failed - leave the game running rather than reloading a
				// stale (or now half-written) save file.
				instance.gameObject.SetActive(true);
				return;
			}

			LoadingOverlay.Load(() =>
			{
				instance.Deactivate();
				LoadScreen.DoLoad(filename);
			});
		}
	}
}