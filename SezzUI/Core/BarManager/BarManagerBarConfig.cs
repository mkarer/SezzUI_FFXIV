using System.Numerics;
using SezzUI.Enums;

namespace SezzUI.BarManager
{
	public class BarManagerBarConfig
	{
		public Vector2 Size = new(316f, 28f);
		public ushort BorderSize = 1;
		public ushort Padding = 2;
		public bool ShowIcons = true;
		public BarDirection FillDirection = BarDirection.Right;
		public bool FillInverted = false;

		private BarManagerStyle _style = BarManagerStyle.Classic;

		public BarManagerStyle Style
		{
			get => _style;
			set
			{
				_style = value;

				switch (value)
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
		}

		public Vector4 FillColor = new(21f / 255f, 60f / 255f, 197f / 255f, 0.7f); // Cooldowns: Spells

		//public Vector4 FillColor = new(73f / 255f, 214f / 255f, 126f / 255f, 0.7f); // Cooldowns: Items
		//public Vector4 FillColor = new(137f / 255f, 68f / 255f, 137f / 255f, 0.7f); // Player: Buffs/Debuffs
		//public Vector4 FillColor = new(0f, 181f / 255f, 181f / 255f, 0.7f); // Player: Proccs
		//public Vector4 FillColor = new(0f, 137f / 255f, 30f / 255f, 0.7f); // Target: Buffs
		//public Vector4 FillColor = new(137f / 255f, 0f, 16f / 255f, 0.7f); // Target: Debuffs
		public Vector4 BackgroundColor = new(0f, 0f, 0f, 100f / 255f);
		public Vector4 BorderColor = new(1f, 1f, 1f, 40f / 255f);

		public Vector4 NameTextColor = new(1f, 1f, 1f, 1);
		public TextStyle NameTextStyle = TextStyle.Normal;

		public Vector4 CountTextColor = new(0f, 1f, 0f, 1);
		public TextStyle CountTextStyle = TextStyle.Normal;

		public Vector4 DurationTextColor = new(1f, 1f, 1f, 1);
		public TextStyle DurationTextextStyle = TextStyle.Normal;

		public bool ShowDuration = true;

		public bool ShowDurationRemaining = false;

		public byte MillisecondsThreshold = 1;

		public BarManagerBarConfig()
		{
			Style = _style; // Set style-based properties
		}
	}
}