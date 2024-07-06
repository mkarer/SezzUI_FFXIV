using System;
using Dalamud.Game.Gui.Dtr;
using Dalamud.Game.Text.SeStringHandling;
using SezzUI.Configuration;

namespace SezzUI.Modules.ServerInfoBar;

public abstract class Entry : PluginModule
{
	public string Title { get; protected set; }
	private IDtrBarEntry? _dtrEntry;
	internal PluginConfigObject Config => _config;

	protected void ClearText() => SetText();

	protected void SetText(SeString? text = null)
	{
		if (text == null || !(this as IPluginComponent).IsEnabled)
		{
			_dtrEntry?.Remove();
			_dtrEntry = null;
			return;
		}

		try
		{
			_dtrEntry ??= Services.DtrBar.Get(Title);
			_dtrEntry.Text = text;
		}
		catch (Exception ex)
		{
			Logger.Error($"Error creating DTR entry: {ex}");
		}
	}

	protected Entry(PluginConfigObject config, string title) : base(config)
	{
		Title = title;
	}

	protected override void OnDispose()
	{
		_dtrEntry?.Remove();
	}
}