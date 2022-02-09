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

			EventManager.DutyFinderQueue.Update += Update;
			EventManager.DutyFinderQueue.Joined += Update;
			EventManager.DutyFinderQueue.Left += ClearText;
			EventManager.DutyFinderQueue.Ready += Update;

			Update();

			return true;
		}

		internal override bool Disable()
		{
			if (!base.Disable())
			{
				return false;
			}

			EventManager.DutyFinderQueue.Update -= Update;
			EventManager.DutyFinderQueue.Joined -= Update;
			EventManager.DutyFinderQueue.Left -= ClearText;
			EventManager.DutyFinderQueue.Ready -= Update;

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

			byte position = EventManager.DutyFinderQueue.Position;
			if (EventManager.DutyFinderQueue.IsReady)
			{
				sb.Append(" Ready!");
			}
			else
			{
				if (Config.DisplayPosition && position > 0)
				{
					sb.Append($" {Config.PositionPrefix}{EventManager.DutyFinderQueue.Position}");
				}

				if (Config.DisplayAverageWaitTime)
				{
					byte averageWaitTime = EventManager.DutyFinderQueue.AverageWaitTime;
					if (averageWaitTime > 0)
					{
						sb.Append(sb.Length == 1 ? " " : Config.Separator);
						sb.Append($"{Config.AverageWaitTimePrefix}{(averageWaitTime > 30 ? "30+" : averageWaitTime)}m");
					}
				}

				if (Config.DisplayEstimatedWaitTime)
				{
					byte estimatedWaitTime = EventManager.DutyFinderQueue.EstimatedWaitTime;
					if (estimatedWaitTime > 0)
					{
						sb.Append(sb.Length == 1 ? " " : Config.Separator);
						sb.Append($"{Config.EstimatedWaitTimePrefix}{estimatedWaitTime}m");
					}
				}
			}

			SetText(sb.ToString());
		}
	}
}

namespace SezzUI.Interface.GeneralElements
{
	public class DutyFinderQueueEntryConfig : PluginConfigObject
	{
		[InputText("Text Segment Separator", formattable = false, isMonitored = true)]
		[Order(0)]
		public string Separator = "/";

		[Checkbox("Display Role Waiting List Number", isMonitored = true)]
		[Order(10)]
		public bool DisplayPosition = true;

		[InputText("Role Waiting List Number Prefix", formattable = false, isMonitored = true)]
		[Order(11)]
		public string PositionPrefix = "#";

		[Checkbox("Display Average Wait Time", isMonitored = true)]
		[Order(20)]
		public bool DisplayAverageWaitTime = true;

		[InputText("Average Wait Time Prefix", formattable = false, isMonitored = true)]
		[Order(21)]
		public string AverageWaitTimePrefix = "";

		[Checkbox("Display Estimated Wait Time", isMonitored = true)]
		[Order(30)]
		public bool DisplayEstimatedWaitTime = true;

		[InputText("Estimated Wait Time Prefix", formattable = false, isMonitored = true)]
		[Order(31)]
		public string EstimatedWaitTimePrefix = "~";

		public void Reset()
		{
			Enabled = true;
			DisplayPosition = true;
			DisplayAverageWaitTime = false;
			DisplayEstimatedWaitTime = true;
			Separator = "/";
			PositionPrefix = "#";
			AverageWaitTimePrefix = "";
			EstimatedWaitTimePrefix = "~";
		}

		public DutyFinderQueueEntryConfig()
		{
			Reset();
		}

		public new static DutyFinderQueueEntryConfig DefaultConfig() => new();
	}
}