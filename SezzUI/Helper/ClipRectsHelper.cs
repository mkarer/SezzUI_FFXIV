using System;
using System.Collections.Generic;
using System.Drawing;
using System.Numerics;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using FFXIVClientStructs.FFXIV.Client.UI;
using FFXIVClientStructs.FFXIV.Component.GUI;
using ImGuiNET;
using SezzUI.Configuration;
using SezzUI.Interface.GeneralElements;
using SezzUI.Modules;

namespace SezzUI.Helper;

public class ClipRectsHelper : IPluginDisposable
{
	public ClipRectsHelper()
	{
		Singletons.Get<ConfigurationManager>().ResetEvent += OnConfigReset;
		OnConfigReset(Singletons.Get<ConfigurationManager>());
	}

	private HUDOptionsConfig _config = null!;

	private void OnConfigReset(ConfigurationManager sender)
	{
		if (_config != null)
		{
			_config.ValueChangeEvent -= OnConfigPropertyChanged;
		}

		_config = sender.GetConfigObject<HUDOptionsConfig>();
		_config.ValueChangeEvent += OnConfigPropertyChanged;
	}

	private void OnConfigPropertyChanged(object sender, OnChangeBaseArgs args)
	{
		if (args.PropertyName == "EnableClipRects" && !_config.EnableClipRects)
		{
			_clipRects.Clear();
		}
	}

	public bool Enabled => _config.EnableClipRects;
	public bool ClippingEnabled => _config.EnableClipRects && !_config.HideInsteadOfClip;

	// these are ordered by priority, if 2 game windows are on top of a ui element
	// the one that comes first in this list is the one that will be clipped around
	internal static List<string> AddonNames = new()
	{
		"ContextMenu",
		"ItemDetail", // tooltip
		"ActionDetail", // tooltip
		"AreaMap",
		"JournalAccept",
		"Talk",
		"Teleport",
		"ActionMenu",
		"Character",
		"CharacterInspect",
		"CharacterTitle",
		"Tryon",
		"ArmouryBoard",
		"RecommendList",
		"GearSetList",
		"MiragePrismMiragePlate",
		"ItemSearch",
		"RetainerList",
		"Bank",
		"RetainerSellList",
		"RetainerSell",
		"SelectString",
		"Shop",
		"ShopExchangeCurrency",
		"ShopExchangeItem",
		"CollectablesShop",
		"MateriaAttach",
		"Repair",
		"Inventory",
		"InventoryLarge",
		"InventoryExpansion",
		"InventoryEvent",
		"InventoryBuddy",
		"Buddy",
		"BuddyEquipList",
		"BuddyInspect",
		"Currency",
		"Macro",
		"PcSearchDetail",
		"Social",
		"SocialDetailA",
		"SocialDetailB",
		"LookingForGroupSearch",
		"LookingForGroupCondition",
		"LookingForGroupDetail",
		"LookingForGroup",
		"ReadyCheck",
		"Marker",
		"FieldMarker",
		"CountdownSettingDialog",
		"CircleFinder",
		"CircleList",
		"CircleNameInputString",
		"Emote",
		"FreeCompany",
		"FreeCompanyProfile",
		"HousingSubmenu",
		"HousingSignBoard",
		"HousingMenu",
		"CrossWorldLinkshell",
		"ContactList",
		"CircleBookInputString",
		"CircleBookQuestion",
		"CircleBookGroupSetting",
		"MultipleHelpWindow",
		"CircleFinderSetting",
		"CircleBook",
		"CircleBookWriteMessage",
		"ColorantColoring",
		"MonsterNote",
		"RecipeNote",
		"GatheringNote",
		"ContentsNote",
		"SpearFishing",
		"Orchestrion",
		"MountNoteBook",
		"MinionNoteBook",
		"AetherCurrent",
		"MountSpeed",
		"FateProgress",
		"SystemMenu",
		"ConfigCharacter",
		"ConfigSystem",
		"ConfigKeybind",
		"AOZNotebook",
		"PvpProfile",
		"GoldSaucerInfo",
		"Achievement",
		"RecommendList",
		"JournalDetail",
		"Journal",
		"ContentsFinder",
		"ContentsFinderSetting",
		"ContentsFinderMenu",
		"ContentsInfo",
		"Dawn",
		"BeginnersMansionProblem",
		"BeginnersMansionProblemCompList",
		"SupportDesk",
		"HowToList",
		"HudLayout",
		"LinkShell",
		"ChatConfig",
		"ColorPicker",
		"PlayGuide",
		"SelectYesno"
	};

	private readonly List<ClipRect> _clipRects = new();

	public unsafe void Update()
	{
		if (!_config.EnableClipRects)
		{
			return;
		}

		_clipRects.Clear();

		AtkStage* stage = AtkStage.GetSingleton();
		if (stage == null)
		{
			return;
		}

		RaptureAtkUnitManager* manager = stage->RaptureAtkUnitManager;
		if (manager == null)
		{
			return;
		}

		AtkUnitList* loadedUnitsList = &manager->AtkUnitManager.AllLoadedUnitsList;
		if (loadedUnitsList == null)
		{
			return;
		}

		for (int i = 0; i < loadedUnitsList->Count; i++)
		{
			try
			{
				AtkUnitBase* addon = *(AtkUnitBase**) Unsafe.AsPointer(ref loadedUnitsList->EntriesSpan[i]);
				if (addon == null || !addon->IsVisible || addon->WindowNode == null || addon->Scale == 0)
				{
					continue;
				}

				string? name = Marshal.PtrToStringAnsi(new(addon->Name));
				if (name == null || !AddonNames.Contains(name))
				{
					continue;
				}

				float margin = 5 * addon->Scale;
				float bottomMargin = 13 * addon->Scale;

				ClipRect clipRect = new(new(addon->X + margin, addon->Y + margin), new(addon->X + addon->WindowNode->AtkResNode.Width * addon->Scale - margin, addon->Y + addon->WindowNode->AtkResNode.Height * addon->Scale - bottomMargin));

				// just in case this causes weird issues / crashes (doubt it though...)
				if (clipRect.Max.X < clipRect.Min.X || clipRect.Max.Y < clipRect.Min.Y)
				{
					continue;
				}

				_clipRects.Add(clipRect);
			}
			catch
			{
				//
			}
		}
	}

	public ClipRect? GetClipRectForArea(Vector2 pos, Vector2 size)
	{
		foreach (ClipRect clipRect in _clipRects)
		{
			ClipRect area = new(pos, pos + size);
			if (clipRect.IntersectsWith(area))
			{
				return clipRect;
			}
		}

		return null;
	}

	public static ClipRect[] GetInvertedClipRects(ClipRect clipRect)
	{
		float maxX = ImGui.GetMainViewport().Size.X;
		float maxY = ImGui.GetMainViewport().Size.Y;

		Vector2 aboveMin = new(0, 0);
		Vector2 aboveMax = new(maxX, clipRect.Min.Y);
		Vector2 leftMin = new(0, clipRect.Min.Y);
		Vector2 leftMax = new(clipRect.Min.X, maxY);

		Vector2 rightMin = new(clipRect.Max.X, clipRect.Min.Y);
		Vector2 rightMax = new(maxX, clipRect.Max.Y);
		Vector2 belowMin = new(clipRect.Min.X, clipRect.Max.Y);
		Vector2 belowMax = new(maxX, maxY);

		ClipRect[] invertedClipRects = new ClipRect[4];
		invertedClipRects[0] = new(aboveMin, aboveMax);
		invertedClipRects[1] = new(leftMin, leftMax);
		invertedClipRects[2] = new(rightMin, rightMax);
		invertedClipRects[3] = new(belowMin, belowMax);

		return invertedClipRects;
	}

	public bool IsPointClipped(Vector2 point)
	{
		if (!_config.EnableClipRects)
		{
			return false;
		}

		foreach (ClipRect clipRect in _clipRects)
		{
			if (clipRect.Contains(point))
			{
				return true;
			}
		}

		return false;
	}

	bool IPluginDisposable.IsDisposed { get; set; } = false;

	~ClipRectsHelper()
	{
		Dispose(false);
	}

	public void Dispose()
	{
		Dispose(true);
		GC.SuppressFinalize(this);
	}

	protected void Dispose(bool disposing)
	{
		if (!disposing || (this as IPluginDisposable).IsDisposed)
		{
			return;
		}

		Singletons.Get<ConfigurationManager>().ResetEvent -= OnConfigReset;
		_config.ValueChangeEvent -= OnConfigPropertyChanged;

		(this as IPluginDisposable).IsDisposed = true;
	}
}

public struct ClipRect
{
	public readonly Vector2 Min;
	public readonly Vector2 Max;

	private readonly Rectangle Rectangle;

	public ClipRect(Vector2 min, Vector2 max)
	{
		Vector2 screenSize = ImGui.GetMainViewport().Size;

		Min = Clamp(min, Vector2.Zero, screenSize);
		Max = Clamp(max, Vector2.Zero, screenSize);

		Vector2 size = Max - Min;

		Rectangle = new((int) Min.X, (int) Min.Y, (int) size.X, (int) size.Y);
	}

	public bool Contains(Vector2 point) => Rectangle.Contains((int) point.X, (int) point.Y);

	public bool IntersectsWith(ClipRect other) => Rectangle.IntersectsWith(other.Rectangle);

	private static Vector2 Clamp(Vector2 vector, Vector2 min, Vector2 max) => new(Math.Max(min.X, Math.Min(max.X, vector.X)), Math.Max(min.Y, Math.Min(max.Y, vector.Y)));
}