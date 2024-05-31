using SezzUI.Configuration;
using SezzUI.Configuration.Attributes;
using SezzUI.Modules.ServerInfoBar.Entries;

namespace SezzUI.Modules.ServerInfoBar;

[Section("Server Info Bar")]
[SubSection("General", 0)]
public class ServerInfoBarConfig : PluginConfigObject
{
	[NestedConfig("Duty Finder Queue Status", 10)]
	public DutyFinderQueueEntryConfig DutyFinderQueueStatus = new();

	public void Reset()
	{
		Enabled = true;
		DutyFinderQueueStatus.Reset();
	}

	public ServerInfoBarConfig()
	{
		Reset();
	}

	public new static ServerInfoBarConfig DefaultConfig() => new();
}