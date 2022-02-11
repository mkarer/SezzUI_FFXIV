using System;
using Dalamud.Game.Gui.Dtr;
using Dalamud.Game.Text.SeStringHandling;
using SezzUI.Configuration;

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
			if (text == null || !(this as IPluginComponent).IsEnabled)
			{
				_dtrEntry?.Dispose();
				_dtrEntry = null;
				return;
			}

			try
			{
				_dtrEntry ??= Service.DtrBar.Get(Title);
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
			_dtrEntry?.Dispose();
		}
	}
}