using System;
using HarmonyLib;
using UnityEngine;
using UnityEngine.UI;

namespace VSyncFpsLimiter
{
	// Central place for the settings themselves: prefs keys, the FPS presets,
	// and the actual QualitySettings/Application calls. Kept separate from the
	// Harmony patches so both the boot-time apply and the in-menu buttons call
	// through the exact same logic.
	public static class DisplaySettings
	{
		public const string VSyncKey = "VSyncFpsLimiter_VSyncEnabled";
		public const string FpsCapIndexKey = "VSyncFpsLimiter_FpsCapIndex";

		// Index into QualitySettings.vSyncCount: 0 = off, 1 = every vblank
		// (normal vsync), 2 = every other vblank (half-rate - e.g. 30fps on
		// a 60Hz display). The old on/off-only version of this mod stored
		// VSyncKey as a plain 0/1, which still maps correctly onto this
		// (Off/On) - no migration needed for existing saves.
		public static readonly int[] VSyncCounts = { 0, 1, 2 };
		public static readonly string[] VSyncLabels = { "Off", "On", "Half" };

		// -1 is Unity's sentinel for "no target frame rate" (i.e. uncapped).
		public static readonly int[] FpsCaps = { -1, 30, 60, 120, 144, 240 };
		public static readonly string[] FpsCapLabels = { "Unlimited", "30", "60", "120", "144", "240" };

		public static void ApplyVSync(int mode)
		{
			// Unity ignores Application.targetFrameRate entirely whenever
			// vSyncCount > 0 - that includes Half (2), not just On (1) - so
			// switching to either On or Half will override whatever FPS cap
			// is set. That's expected, not a bug: vsync IS a frame limiter,
			// just tied to the display's refresh rate instead of a fixed
			// number.
			mode = Mathf.Clamp(mode, 0, VSyncCounts.Length - 1);
			QualitySettings.vSyncCount = VSyncCounts[mode];

			// Same underlying quirk ApplyFpsCap's -1 reset works around:
			// Unity's frame pacer doesn't reliably pick back up an
			// already-set targetFrameRate just because vSyncCount changed
			// back to 0 - it needs the value re-asserted at the moment vsync
			// actually turns off, or the game keeps running uncapped until
			// something else forces a recompute. Only do this if an FPS cap
			// has actually been configured, so a fresh install with nothing
			// saved doesn't get a cap forced on it as a side effect of
			// toggling vsync.
			if (mode == 0 && KPlayerPrefs.HasKey(FpsCapIndexKey))
			{
				ApplyFpsCap(GetSavedFpsCapIndex(0));
			}
		}

		public static void ApplyFpsCap(int index)
		{
			index = Mathf.Clamp(index, 0, FpsCaps.Length - 1);

			// Setting targetFrameRate directly from one fixed value to
			// another doesn't reliably take effect immediately while the
			// game is already running - Unity's own docs example for this
			// property resets to -1 first, then sets the real target, for
			// exactly this reason. Without this, a value set here can sit
			// unused until something else (e.g. toggling VSync) forces a
			// recompute.
			Application.targetFrameRate = -1;
			Application.targetFrameRate = FpsCaps[index];
		}

		public static int GetSavedVSyncMode(int fallback)
		{
			return KPlayerPrefs.HasKey(VSyncKey)
				? Mathf.Clamp(KPlayerPrefs.GetInt(VSyncKey), 0, VSyncCounts.Length - 1)
				: fallback;
		}

		public static int GetSavedFpsCapIndex(int fallback)
		{
			return KPlayerPrefs.HasKey(FpsCapIndexKey)
				? Mathf.Clamp(KPlayerPrefs.GetInt(FpsCapIndexKey), 0, FpsCaps.Length - 1)
				: fallback;
		}
	}

	// GraphicsOptionsScreen.SetSettingsFromPrefs() is the same static method
	// LaunchInitializer calls on game boot to re-apply saved resolution and
	// low-res-texture settings before any menu exists. Postfixing it means our
	// saved VSync/FPS-cap prefs get re-applied at the same point, every launch -
	// no need to reopen the options screen after restarting.
	[HarmonyPatch(typeof(GraphicsOptionsScreen), nameof(GraphicsOptionsScreen.SetSettingsFromPrefs))]
	public static class GraphicsOptionsScreen_SetSettingsFromPrefs_Patch
	{
		public static void Postfix()
		{
			if (KPlayerPrefs.HasKey(DisplaySettings.VSyncKey))
			{
				DisplaySettings.ApplyVSync(DisplaySettings.GetSavedVSyncMode(1));
			}

			if (KPlayerPrefs.HasKey(DisplaySettings.FpsCapIndexKey))
			{
				DisplaySettings.ApplyFpsCap(DisplaySettings.GetSavedFpsCapIndex(0));
			}

			// If neither key exists yet (first run with this mod installed),
			// we deliberately leave the game's own defaults untouched.
		}
	}

	// Inserts a "VSync" button beside "Fullscreen" and an "FPS Limit" button
	// beside "Low Resolution Textures", both in the DISPLAY section of the
	// in-game Graphics Options screen. VSync cycles Off -> On -> Half (every
	// other vblank, e.g. 30fps on a 60Hz display) -> back to Off. The FPS
	// button reads "Cap: <value>", greys out, and shows "VSync: <mode>"
	// instead whenever VSync is On or Half, since Unity ignores the FPS cap
	// entirely in either of those cases.
	//
	// Written and verified against Kupie/ONI_Decomp, which is C# source only -
	// it doesn't include the actual Unity scene/prefab hierarchy for this
	// screen. The row lookup below is written defensively because of that
	// (searches a couple of ancestor levels for the row, climbs past a
	// HorizontalLayoutGroup if the row turns out to be nested one level
	// deeper than the shared line), but it's worth checking the player log
	// the first time it runs - the Debug.Log at the bottom of Postfix reports
	// which row each control landed in. MaxRowSearchDepth is the constant to
	// tune if it's off.
	[HarmonyPatch(typeof(GraphicsOptionsScreen), "OnSpawn")]
	public static class GraphicsOptionsScreen_OnSpawn_Patch
	{
		private const int MaxRowSearchDepth = 2;

		public static void Postfix(GraphicsOptionsScreen __instance, MultiToggle ___fullscreenToggle, MultiToggle ___lowResToggle, KButton ___doneButton)
		{
			if (___fullscreenToggle == null || ___lowResToggle == null)
			{
				Debug.LogWarning("[VSyncFpsLimiter] fullscreenToggle/lowResToggle field not found on GraphicsOptionsScreen - skipping UI insert.");
				return;
			}

			// Same mechanism for both: find the row the given toggle sits in
			// and append a clone of it as a new item at the end of that same
			// line, so VSync lands beside Fullscreen and the FPS button lands
			// beside Low Resolution Textures, rather than on new lines below.
			GameObject vsyncRow = CloneIntoSameRow(___fullscreenToggle.transform);
			GameObject fpsRow = CloneIntoSameRow(___lowResToggle.transform);
			if (vsyncRow == null || fpsRow == null)
			{
				Debug.LogWarning("[VSyncFpsLimiter] Could not build one or both rows - skipping UI insert.");
				return;
			}

			// Build the FPS button first so SetupVSyncButton has something to
			// grey out/relabel whenever VSync gets changed.
			KButton fpsButton = SetupFpsButton(fpsRow, ___doneButton);
			SetupVSyncButton(vsyncRow, ___doneButton, (mode) => RefreshFpsButton(fpsButton, mode));
			RefreshFpsButton(fpsButton, DisplaySettings.GetSavedVSyncMode(QualitySettings.vSyncCount > 0 ? 1 : 0));

			Debug.Log(string.Format(
				"[VSyncFpsLimiter] Inserted VSync into row '{0}' and FPS Limit into row '{1}'.",
				vsyncRow.transform.parent.name, fpsRow.transform.parent.name));
		}

		// Finds the row/line that toggleTransform belongs to and appends a
		// clone of it as a new item at the end of that same line - i.e. to
		// the right of the existing checkbox, not as a new line below it.
		private static GameObject CloneIntoSameRow(Transform toggleTransform)
		{
			// unit is the narrow single-option piece (checkbox + its own
			// label) - always what gets cloned, so the result only ever
			// contains one control, never a copy of a neighboring option.
			Transform unit = FindRow(toggleTransform);
			if (unit == null)
			{
				return null;
			}

			// insertionAnchor is what the clone gets inserted after. Usually
			// that's the same object as unit - but if its parent is itself a
			// horizontal strip (a shared line with room for more than one
			// item), climb one level so the clone gets appended as a sibling
			// within that shared line instead of one level too deep.
			Transform insertionAnchor = unit;
			if (insertionAnchor.parent != null && insertionAnchor.parent.GetComponent<HorizontalLayoutGroup>() != null)
			{
				insertionAnchor = insertionAnchor.parent;
			}

			Transform container = insertionAnchor.parent;
			if (container == null)
			{
				return null;
			}

			GameObject clone = Util.KInstantiateUI(unit.gameObject, container.gameObject, true);
			clone.transform.SetSiblingIndex(insertionAnchor.GetSiblingIndex() + 1);
			return clone;
		}

		// Walk up a couple of ancestor levels from the toggle looking for the
		// object that also contains the option's label - i.e. the full row
		// (checkbox + text) rather than just the checkbox itself. Some Klei
		// row prefabs put the label as a child of the toggle GameObject
		// directly; others put toggle and label as siblings under a shared
		// row parent. This covers both without needing the actual prefab.
		private static Transform FindRow(Transform toggleTransform)
		{
			Transform t = toggleTransform;
			for (int depth = 0; depth < MaxRowSearchDepth && t != null; depth++)
			{
				if (t.GetComponentInChildren<LocText>() != null)
				{
					return t;
				}
				t = t.parent;
			}
			return toggleTransform.parent; // best-effort fallback
		}

		// Replaces a cloned row's checkbox with a real KButton (cloned from
		// the screen's own Done button), positioned into the exact slot the
		// checkbox occupied. Shared by both VSync and FPS Limit, since both
		// need more than the two states a checkbox's sprite naturally
		// supports. Returns the new button, or null if anything needed to
		// build it was missing.
		private static KButton SwapCheckboxForButton(GameObject row, KButton buttonTemplate, string rowLabelText)
		{
			MultiToggle placeholder = row.GetComponentInChildren<MultiToggle>();
			LocText rowLabel = row.GetComponentInChildren<LocText>();
			if (rowLabel != null)
			{
				rowLabel.SetText(rowLabelText);

				// Guard against the row's label being a child of the toggle
				// we're about to destroy below (rather than a sibling of it) -
				// pull it up to the row itself first. A no-op if it already
				// lives elsewhere in the row.
				if (placeholder != null && rowLabel.transform.IsChildOf(placeholder.transform))
				{
					rowLabel.transform.SetParent(row.transform, true);
				}
			}

			if (placeholder == null || buttonTemplate == null)
			{
				Debug.LogWarning("[VSyncFpsLimiter] Could not build a button for row '" + row.name + "' - missing checkbox placeholder or button template.");
				return null;
			}

			// Clone the Done button into the exact slot the checkbox
			// occupied, then remove the checkbox. Copying the RectTransform
			// values directly (rather than guessing a fixed size) means this
			// keeps working regardless of the row's actual layout.
			RectTransform slot = placeholder.GetComponent<RectTransform>();
			Transform slotParent = slot.parent;
			int slotIndex = slot.GetSiblingIndex();

			GameObject buttonGo = Util.KInstantiateUI(buttonTemplate.gameObject, slotParent.gameObject, true);
			RectTransform buttonRect = buttonGo.GetComponent<RectTransform>();
			buttonRect.anchorMin = slot.anchorMin;
			buttonRect.anchorMax = slot.anchorMax;
			buttonRect.pivot = slot.pivot;
			buttonRect.anchoredPosition = slot.anchoredPosition;

			// Don't reuse the checkbox's own sizeDelta as-is - it's sized for
			// a small square glyph, not text like "Unlimited", which is
			// exactly what was squeezing an earlier version of this button
			// down to a sliver. Keep its height (row height is still a sane
			// value to match), but enforce a sane minimum width for text.
			buttonRect.sizeDelta = new Vector2(Mathf.Max(slot.sizeDelta.x, 140f), Mathf.Max(slot.sizeDelta.y, 28f));
			buttonGo.transform.SetSiblingIndex(slotIndex);

			// Whether or not something upstream is a LayoutGroup driving
			// child width automatically, give it an explicit size hint
			// instead of leaving that to chance - replaces rather than just
			// removes whatever LayoutElement the Done-button clone came
			// with, since removing it entirely can leave a LayoutGroup
			// parent to fall back on the text's own (still-squeezed) preferred
			// width on the first layout pass.
			LayoutElement layoutElement = buttonGo.GetComponent<LayoutElement>();
			if (layoutElement == null)
			{
				layoutElement = buttonGo.AddComponent<LayoutElement>();
			}
			layoutElement.minWidth = 110f;
			layoutElement.preferredWidth = 140f;
			layoutElement.flexibleWidth = 0f;

			UnityEngine.Object.Destroy(placeholder.gameObject);

			KButton button = buttonGo.GetComponent<KButton>();

			// KButton.onClick is a C# event (not a plain field like
			// MultiToggle's), so it can't be cleared with "= null" from
			// outside the class - ClearOnClick() is KButton's own public
			// method for exactly that, defensive against a cloned Done
			// button carrying over its original handler.
			button.ClearOnClick();

			return button;
		}

		private static void SetupVSyncButton(GameObject row, KButton buttonTemplate, Action<int> onChanged)
		{
			KButton button = SwapCheckboxForButton(row, buttonTemplate, "VSync:");
			if (button == null)
			{
				return;
			}

			int mode = DisplaySettings.GetSavedVSyncMode(QualitySettings.vSyncCount > 0 ? 1 : 0);
			LocText label = button.GetComponentInChildren<LocText>();

			void Refresh()
			{
				if (label != null)
				{
					label.SetText("VSync: " + DisplaySettings.VSyncLabels[mode]);
				}
			}

			Refresh();
			button.onClick += () =>
			{
				mode = (mode + 1) % DisplaySettings.VSyncCounts.Length;
				DisplaySettings.ApplyVSync(mode);
				KPlayerPrefs.SetInt(DisplaySettings.VSyncKey, mode);
				Refresh();
				onChanged(mode);
			};
		}

		private static KButton SetupFpsButton(GameObject row, KButton buttonTemplate)
		{
			KButton button = SwapCheckboxForButton(row, buttonTemplate, "FPS Limit:");
			if (button == null)
			{
				return null;
			}

			int index = DisplaySettings.GetSavedFpsCapIndex(0);
			LocText label = button.GetComponentInChildren<LocText>();

			void Refresh()
			{
				if (label != null)
				{
					label.SetText("Cap: " + DisplaySettings.FpsCapLabels[index]);
				}
			}

			Refresh();
			button.onClick += () =>
			{
				index = (index + 1) % DisplaySettings.FpsCaps.Length;
				DisplaySettings.ApplyFpsCap(index);
				KPlayerPrefs.SetInt(DisplaySettings.FpsCapIndexKey, index);
				Refresh();
			};

			return button;
		}

		// Called once at setup and again every time VSync gets changed. While
		// VSync is On or Half, Unity ignores Application.targetFrameRate
		// entirely, so the FPS button is greyed out and relabeled to make
		// that visible instead of leaving a live-looking control that
		// quietly does nothing.
		private static void RefreshFpsButton(KButton button, int vsyncMode)
		{
			if (button == null)
			{
				return;
			}

			bool vsyncActive = vsyncMode != 0;
			button.isInteractable = !vsyncActive;

			LocText label = button.GetComponentInChildren<LocText>();
			if (label == null)
			{
				return;
			}

			if (vsyncActive)
			{
				label.SetText("VSync: " + DisplaySettings.VSyncLabels[vsyncMode]);
			}
			else
			{
				int index = DisplaySettings.GetSavedFpsCapIndex(0);
				label.SetText("Cap: " + DisplaySettings.FpsCapLabels[index]);
			}
		}
	}
}