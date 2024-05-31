using SezzUI.Configuration;
using SezzUI.Configuration.Attributes;
using SezzUI.Enums;

namespace SezzUI.Interface.BarManager;

[Disableable(false)]
[DisableParentSettings("Anchor", "Position")]
public class BarManagerBarConfig : AnchorablePluginConfigObject
{
	// Size/Border
	[DragInt("Border Size", min = 0, max = 20)]
	[Order(30)]
	public int BorderSize = 1;

	[ColorEdit4("Border Color")]
	[Order(31)]
	public PluginConfigColor BorderColor = new(new(1f, 1f, 1f, 40f / 255f));

	[DragInt("Bar Padding", min = 0, max = 20)]
	[Order(32)]
	public int Padding = 2;

	// Bar Style
	[Combo("Style", "Classic", "Ruri", spacing = true)]
	[Order(40)]
	public BarManagerStyle Style = BarManagerStyle.Ruri;

	[Checkbox("Show Icons")]
	[Order(41)]
	public bool ShowIcons = true;

	// Bar Filling
	[Combo("Fill Direction", "Left", "Right", "Up", "Down", spacing = true)]
	[Order(50)]
	public BarDirection FillDirection = BarDirection.Right;

	[ColorEdit4("Fill Color")]
	[Order(51)]
	public PluginConfigColor FillColor = new(new(21f / 255f, 60f / 255f, 197f / 255f, 0.7f)); // Cooldowns: Spells
	//public PluginConfigColor FillColor = new(new(73f / 255f, 214f / 255f, 126f / 255f, 0.7f)); // Cooldowns: Items
	//public PluginConfigColor FillColor = new(new(137f / 255f, 68f / 255f, 137f / 255f, 0.7f)); // Player: Buffs/Debuffs
	//public PluginConfigColor FillColor = new(new(0f, 181f / 255f, 181f / 255f, 0.7f)); // Player: Proccs
	//public PluginConfigColor FillColor = new(new(0f, 137f / 255f, 30f / 255f, 0.7f)); // Target: Buffs
	//public PluginConfigColor FillColor = new(new(137f / 255f, 0f, 16f / 255f, 0.7f)); // Target: Debuffs

	[ColorEdit4("Background Color")]
	[Order(52)]
	public PluginConfigColor BackgroundColor = new(new(0f, 0f, 0f, 100f / 255f));

	[Checkbox("Inverse Fill")]
	[Order(53)]
	public bool FillInverted;

	// Name Text
	[ColorEdit4("Name Text Color", spacing = true)]
	[Order(70)]
	public PluginConfigColor NameTextColor = new(new(1f, 1f, 1f, 1));

	[Combo("Name Text Style", "Normal", "Shadowed", "Outline")]
	[Order(71)]
	public TextStyle NameTextStyle = TextStyle.Outline;

	// Count Text
	[ColorEdit4("Stacks Text Color", spacing = true)]
	[Order(80)]
	public PluginConfigColor CountTextColor = new(new(0f, 1f, 0f, 1));

	[Combo("Stacks Text Style", "Normal", "Shadowed", "Outline")]
	[Order(81)]
	public TextStyle CountTextStyle = TextStyle.Outline;

	// Duration Text
	[Checkbox("Show Duration Text", spacing = true)]
	[Order(90)]
	public bool ShowDuration = true;

	[ColorEdit4("Duration Text Color")]
	[Order(91, collapseWith = nameof(ShowDuration))]
	public PluginConfigColor DurationTextColor = new(new(1f, 1f, 1f, 1));

	[Combo("Duration Text Style", "Normal", "Shadowed", "Outline")]
	[Order(92, collapseWith = nameof(ShowDuration))]
	public TextStyle DurationTextextStyle = TextStyle.Outline;

	[DragInt("Milliseconds Threshold [s]", min = 0, max = 600, help = "Display milliseconds (in addition to seconds) when duration is lower or equal to this value.")]
	[Order(93, collapseWith = nameof(ShowDuration))]
	public int MillisecondsThreshold = 1;

	[Checkbox("Show Remaining Duration", help = "Display remaining duration instead of elapsed duration.")]
	[Order(94, collapseWith = nameof(ShowDuration))]
	public bool ShowDurationRemaining = true;

	public void SetDefaultStyleAttributes()
	{
		switch (Style)
		{
			case BarManagerStyle.Classic:
				NameTextStyle = TextStyle.Shadowed;
				CountTextStyle = TextStyle.Shadowed;
				DurationTextextStyle = TextStyle.Shadowed;
				break;

			case BarManagerStyle.Ruri:
				NameTextStyle = TextStyle.Outline;
				CountTextStyle = TextStyle.Outline;
				DurationTextextStyle = TextStyle.Outline;
				break;
		}
	}

	public virtual void Reset()
	{
		Style = BarManagerStyle.Classic;
		SetDefaultStyleAttributes();

		Size = new(316f, 28f);
		BorderSize = 1;
		Padding = 2;
		ShowIcons = true;
		FillDirection = BarDirection.Right;
		FillInverted = false;
		FillColor.Vector = new(21f / 255f, 60f / 255f, 197f / 255f, 0.7f);
		BackgroundColor.Vector = new(0f, 0f, 0f, 100f / 255f);
		BorderColor.Vector = new(1f, 1f, 1f, 40f / 255f);
		NameTextColor.Vector = new(1f, 1f, 1f, 1);
		CountTextColor.Vector = new(0f, 1f, 0f, 1);
		DurationTextColor.Vector = new(1f, 1f, 1f, 1);
		ShowDuration = true;
		ShowDurationRemaining = false;
		MillisecondsThreshold = 1;
	}

	public BarManagerBarConfig()
	{
		Reset();
	}

	public new static BarManagerBarConfig DefaultConfig() => new();
}