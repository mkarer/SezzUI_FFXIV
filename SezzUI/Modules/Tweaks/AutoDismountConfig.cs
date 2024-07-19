using SezzUI.Configuration;
using SezzUI.Configuration.Attributes;

namespace SezzUI.Modules.Tweaks;

[Section("Tweaks")]
[SubSection("Auto Dismount", 0)]
public class AutoDismountConfig : PluginConfigObject
{
	[Checkbox("Queue failed cast after dismounting")]
	[Order(20)]
	public bool AutoCast = true;

	public void Reset()
	{
		Enabled = false;
		AutoCast = true;
	}

	public AutoDismountConfig()
	{
		Reset();
	}

	public new static AutoDismountConfig DefaultConfig() => new();
}