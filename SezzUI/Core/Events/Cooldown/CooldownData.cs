﻿using System;
using FFXIVClientStructs.FFXIV.Client.Game;

namespace SezzUI.GameEvents
{
    public class CooldownData
    {
        public ActionType Type = ActionType.None;

        /// <summary>
        /// Start time in milliseconds since the system started (Environment.TickCount64)
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
                    _hasChanged = true;
                }
                _startTime = value;
            }
        }
        private long _startTime = 0;

        /// <summary>
        /// Total duration (until next charge) in milliseconds.
        /// </summary>
        public uint Duration
        {
            get => _duration;
            set { if (_duration != value) { _hasChanged = true; _duration = value; } }
        }
        private uint _duration = 0;

        /// <summary>
        /// Maximum amount of charges at the current level.
        /// </summary>
        public ushort MaxCharges
        {
            get => _maxCharges;
            set { if (_maxCharges != value) { _hasChanged = true; _maxCharges = value; } }
        }
        private ushort _maxCharges = 0;

        /// <summary>
        /// Current amount of charges.
        /// </summary>
        public ushort CurrentCharges
        {
            get => _currentCharges;
            set { if (_currentCharges != value) { _hasChanged = true; _currentCharges = value; } }
        }
        private ushort _currentCharges = 0;

        /// <summary>
        /// Remaining cooldown in milliseconds or 0 if inactive.
        /// </summary>
        public uint Remaining => IsActive ? Duration - (uint)(Environment.TickCount64 - StartTime) : 0;

        /// <summary>
        /// Elapsed time in milliseconds or 0 if inactive.
        /// </summary>
        public uint Elapsed => IsActive ? Duration - Remaining : 0;

        public bool IsActive => _duration > 0 && _currentCharges != _maxCharges;

        /// <summary>
        /// Returns if any value has changed since the last call of PrepareUpdate().
        /// </summary>
        public bool HasChanged { get => _hasChanged; }
        private bool _hasChanged = false;
        public void PrepareUpdate() { _hasChanged = false; }
    }
}