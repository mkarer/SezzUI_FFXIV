using System.Text;
using SezzUI.Config;
using SezzUI.Config.Attributes;
using SezzUI.Interface.GeneralElements;

namespace SezzUI.Modules.ServerInfoBar.Entries
{
	public class DutyFinderQueue : Entry
	{
		private const string ICON = "\ue081"; // Clock: \ue031
		public new DutyFinderQueueEntryConfig Config => (DutyFinderQueueEntryConfig) _config;

		public DutyFinderQueue(DutyFinderQueueEntryConfig config) : base(config, "SezzUI: Duty Finder Queue Status")
		{
			Config.ValueChangeEvent += OnConfigPropertyChanged;
			ConfigurationManager.Instance.Reset += OnConfigReset;
		}

		protected override void InternalDispose()
		{
			Config.ValueChangeEvent -= OnConfigPropertyChanged;
			ConfigurationManager.Instance.Reset += OnConfigReset;

			base.InternalDispose();
		}

		private void OnConfigReset(ConfigurationManager configurationManager, PluginConfigObject config)
		{
			if (config != _config)
			{
				return;
			}

			Toggle(Config.Enabled);
			Update();
		}

		private void OnConfigPropertyChanged(PluginConfigObject sender, OnChangeBaseArgs args)
		{
			if (args.PropertyName == "Enabled")
			{
				Toggle(Config.Enabled);
			}
			else
			{
				Update();
			}
		}

		internal override bool Enable()
		{
			if (!base.Enable())
			{
				return false;
			}

			EventManager.DutyFinderQueue.Update += OnQueueUpdate;
			EventManager.DutyFinderQueue.Joined += OnQueueJoined;
			EventManager.DutyFinderQueue.Left += OnQueueLeft;
			EventManager.DutyFinderQueue.Ready += OnQueueReady;

			Update();

			return true;
		}

		internal override bool Disable()
		{
			if (!base.Disable())
			{
				return false;
			}

			EventManager.DutyFinderQueue.Update -= OnQueueUpdate;
			EventManager.DutyFinderQueue.Joined -= OnQueueJoined;
			EventManager.DutyFinderQueue.Left -= OnQueueLeft;
			EventManager.DutyFinderQueue.Ready -= OnQueueReady;

			ClearText();

			return true;
		}

		private void Update()
		{
			if (!Config.Enabled || !EventManager.DutyFinderQueue.IsQueued)
			{
				return;
			}

			StringBuilder sb = new(ICON);

			if (EventManager.DutyFinderQueue.Position != 0)
			{
				sb.Append($" #{(EventManager.DutyFinderQueue.Position != 0 ? EventManager.DutyFinderQueue.Position : "?")}");
				if (Config.DisplayEstimatedWaitTime)
				{
					sb.Append("/");
				}
			}

			if (Config.DisplayEstimatedWaitTime)
			{
				if (EventManager.DutyFinderQueue.Position == 0)
				{
					sb.Append(" ");
				}

				sb.Append($"{EventManager.DutyFinderQueue.EstimatedWaitTime}m");
			}

			SetText(sb.ToString());
		}

		private void OnQueueUpdate(byte queuePosition, byte waitTime, uint contentFinderConditionId) => Update();

		private void OnQueueJoined() => SetText($"{ICON}");

		private void OnQueueLeft() => ClearText();

		private void OnQueueReady() => SetText($"{ICON} Ready!");
	}
}

namespace SezzUI.Interface.GeneralElements
{
	public class DutyFinderQueueEntryConfig : PluginConfigObject
	{
		[Checkbox("Display Estimated Wait Time", isMonitored = true)]
		[Order(0)]
		public bool DisplayEstimatedWaitTime = true;

		public void Reset()
		{
			Enabled = true;
			DisplayEstimatedWaitTime = true;
		}

		public DutyFinderQueueEntryConfig()
		{
			Reset();
		}

		public new static DutyFinderQueueEntryConfig DefaultConfig() => new();
	}
}