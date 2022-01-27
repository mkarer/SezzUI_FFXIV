using System;
using FFXIVClientStructs.FFXIV.Client.Game;

namespace SezzUI.GameEvents
{
	public class CooldownData
	{
		public ActionType Type = ActionType.None;

        /// <summary>
        ///     Start time in milliseconds since the system started (Environment.TickCount64)
        /// </summary>
        public long StartTime
		{
			get => _startTime;
			set
			{
				// TODO: Unsure if 80ms variation is fine (and where it comes from).
				// Without allowing a small variation it changes ALL THE FKING time?!
				if (_startTime != value && Math.Abs(_startTime - value) >= 80)
				{
					HasChanged = true;
				}

				_startTime = value;
			}
		}

		private long _startTime;

        /// <summary>
        ///     Total duration (until next charge) in milliseconds.
        /// </summary>
        public uint Duration
		{
			get => _duration;
			set
			{
				if (_duration != value)
				{
					HasChanged = true;
					_duration = value;
				}
			}
		}

		private uint _duration;

        /// <summary>
        ///     Maximum amount of charges at the current level.
        /// </summary>
        public ushort MaxCharges
		{
			get => _maxCharges;
			set
			{
				if (_maxCharges != value)
				{
					HasChanged = true;
					_maxCharges = value;
				}
			}
		}

		private ushort _maxCharges;

        /// <summary>
        ///     Current amount of charges.
        /// </summary>
        public ushort CurrentCharges
		{
			get => _currentCharges;
			set
			{
				if (_currentCharges != value)
				{
					HasChanged = true;
					_currentCharges = value;
				}
			}
		}

		private ushort _currentCharges;

        /// <summary>
        ///     Remaining cooldown in milliseconds or 0 if inactive.
        /// </summary>
        public uint Remaining => IsActive ? (uint) _remaining : 0;

		private long _remaining => Duration - (Environment.TickCount64 - StartTime);

        /// <summary>
        ///     Elapsed time in milliseconds or 0 if inactive.
        /// </summary>
        public uint Elapsed => IsActive ? Duration - Remaining : 0;

		// TODO: Force update when checking IsActive instead of checking remaining time for a negative value?
		public bool IsActive => _duration > 0 && _currentCharges != _maxCharges && _remaining > 0;

        /// <summary>
        ///     Returns if any value has changed since the last call of PrepareUpdate().
        /// </summary>
        public bool HasChanged { get; private set; }

		public void PrepareUpdate()
		{
			HasChanged = false;
		}
	}
}