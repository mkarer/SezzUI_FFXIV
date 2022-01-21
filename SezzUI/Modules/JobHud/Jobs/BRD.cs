using System.Linq;
using System.Numerics;
using Dalamud.Game.ClientState.JobGauge.Enums;
using Dalamud.Game.ClientState.JobGauge.Types;
using DelvUI.Helpers;
using SezzUI.Enums;

namespace SezzUI.Modules.JobHud.Jobs
{
	public sealed class BRD : BasePreset
	{
		public override uint JobId => JobIDs.BRD;

		public override void Configure(JobHud hud)
		{
			Bar bar1 = new(hud);
			bar1.Add(new(bar1) {TextureActionId = 100, StatusIds = new[] {124u, 1200u}, MaxStatusDuration = 45, StatusTarget = Unit.Target}); // Venomous Bite
			bar1.Add(new(bar1) {TextureActionId = 113, StatusIds = new[] {129u, 1201u}, MaxStatusDuration = 45, StatusTarget = Unit.Target}); // Windbite
			bar1.Add(new(bar1) {TextureActionId = 101, CooldownActionId = 101, StatusId = 125, MaxStatusDuration = 20}); // Raging Strikes
			bar1.Add(new(bar1) {TextureActionId = 107, CooldownActionId = 107, StatusId = 128, MaxStatusDuration = 10}); // Barrage
			hud.AddBar(bar1);

			Bar bar2 = new(hud);
			bar2.Add(new(bar2) {TextureActionId = 3559, CooldownActionId = 3559, CustomDuration = GetWanderersMinuetDuration, CustomStacks = GetWanderersMinuetStacks}); // The Wanderer's Minuet
			bar2.Add(new(bar2) {TextureActionId = 114, CooldownActionId = 114, CustomDuration = GetMagesBalladDuration}); // Mage's Ballad
			bar2.Add(new(bar2) {TextureActionId = 116, CooldownActionId = 116, CustomDuration = GetArmysPaeonDuration, CustomStacks = GetArmysPaeonStacks}); // Army's Paeon
			bar2.Add(new(bar2) {TextureActionId = 118, CooldownActionId = 118, StatusId = 141, MaxStatusDuration = 15, StatusSourcePlayer = false, CustomPowerCondition = IsPlaying}); // Battle Voice
			hud.AddBar(bar2);

			// Straight Shot
			hud.AddAlert(new()
			{
				StatusId = 122,
				Image = Plugin.AssemblyLocation + "Media\\Images\\Overlays\\focus_fire.png",
				Size = new Vector2(256, 128) * 0.8f,
				Position = new(0, -180),
				MaxDuration = 30,
				Level = 2,
				TextOffset = new(0, -15)
			});

			base.Configure(hud);

			Bar roleBar = hud.Bars.Last();
			//roleBar.Add(new Icon(roleBar) {TextureActionId = 3561, CooldownActionId = 3561, StatusId = 866, MaxStatusDuration = 30, StatusTarget = Enums.Unit.TargetOrPlayer, StatusSourcePlayer = false}, 1); // The Warden's Paean
			roleBar.Add(new(roleBar) {TextureActionId = 7405, CooldownActionId = 7405, StatusIds = new[] { 1934u, 1826u, 1951u }, MaxStatusDuration = 15, StatusSourcePlayer = false}, 1); // Troubadour
			roleBar.Add(new(roleBar) {TextureActionId = 7408, CooldownActionId = 7408, StatusId = 1202, MaxStatusDuration = 15, StatusSourcePlayer = false}, 1); // Nature's Minne
		}

		private static bool IsPlaying()
		{
			BRDGauge gauge = Plugin.JobGauges.Get<BRDGauge>();
			return gauge != null && gauge.Song != Song.NONE;
		}

		private static (float, float) GetSongDuration(Song song)
		{
			BRDGauge gauge = Plugin.JobGauges.Get<BRDGauge>();
			if (gauge != null && gauge.Song == song)
			{
				return (gauge.SongTimer / 1000f, 45f);
			}

			return (0, 0);
		}

		private static (byte, byte) GetSongStacks(Song song)
		{
			byte maxStacks = song switch
			{
				Song.WANDERER => 3,
				Song.ARMY => 4,
				_ => 0
			};

			BRDGauge gauge = Plugin.JobGauges.Get<BRDGauge>();
			return (gauge != null && gauge.Song == song ? gauge.Repertoire : (byte) 0, maxStacks);
		}

		private static (float, float) GetWanderersMinuetDuration() => GetSongDuration(Song.WANDERER);

		private static (byte, byte) GetWanderersMinuetStacks() => GetSongStacks(Song.WANDERER);

		private static (float, float) GetMagesBalladDuration() => GetSongDuration(Song.MAGE);

		private static (float, float) GetArmysPaeonDuration() => GetSongDuration(Song.ARMY);

		private static (byte, byte) GetArmysPaeonStacks() => GetSongStacks(Song.ARMY);
	}
}