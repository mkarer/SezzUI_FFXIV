using System;
using Dalamud.Game.Gui.Dtr;
using Dalamud.Game.Text.SeStringHandling;
using SezzUI.Config;

namespace SezzUI.Modules.ServerInfoBar
{
	public abstract class Entry : PluginModule
	{
		public string Title;
		private DtrBarEntry? _dtrEntry;
		public PluginConfigObject Config => _config;

		public void ClearText() => SetText();

		public void SetText(SeString? text = null)
		{
			if (text == null || !Enabled)
			{
				_dtrEntry?.Dispose();
				_dtrEntry = null;
				return;
			}

			try
			{
				_dtrEntry ??= Plugin.DtrBar.Get(Title);
				_dtrEntry.Text = text;
			}
			catch (Exception ex)
			{
				Logger.Error(ex, "SetText", $"Error creating DTR entry: {ex}");
			}
		}

		protected Entry(PluginConfigObject config, string title) : base(config)
		{
			Title = title;
		}

		protected override void InternalDispose()
		{
			_dtrEntry?.Dispose();
		}
	}
}