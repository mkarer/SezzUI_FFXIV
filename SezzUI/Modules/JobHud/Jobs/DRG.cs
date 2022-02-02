using System.Numerics;
using Dalamud.Game.ClientState.JobGauge.Types;
using SezzUI.Enums;
using SezzUI.Helpers;

namespace SezzUI.Modules.JobHud.Jobs
{
	public sealed class DRG : BasePreset
	{
		public override uint JobId => JobIDs.DRG;

		public override void Configure(JobHud hud)
		{
			Bar bar1 = new(hud);
			bar1.Add(new(bar1) {TextureActionId = 88, StatusIds = new[] {118u, 1312u, 2719u}, MaxStatusDuration = 24, StatusTarget = Unit.Target}); // Chaos Thrust
			bar1.Add(new(bar1) {TextureActionId = 87, StatusId = 2720, MaxStatusDuration = 30}); // Disembowel (Power Surge)
			bar1.Add(new(bar1) {TextureActionId = 3555, CooldownActionId = 3555, GlowBorderUsable = true, RequiredPowerAmount = 2, RequiredPowerType = JobsHelper.PowerType.EyeOfTheDragon, StacksPowerType = JobsHelper.PowerType.EyeOfTheDragon}); // Geirskogul
			bar1.Add(new(bar1) {TextureActionId = 25773, CooldownActionId = 25773, GlowBorderUsable = true, RequiredPowerAmount = 2, RequiredPowerType = JobsHelper.PowerType.FirstmindsFocus, StacksPowerType = JobsHelper.PowerType.FirstmindsFocus}); // Wyrmwind Thrust
			hud.AddBar(bar1);

			Bar bar2 = new(hud);
			bar2.Add(new(bar2) {TextureActionId = 83, CooldownActionId = 83, StatusId = 116, MaxStatusDuration = 5}); // Life Surge
			bar2.Add(new(bar2) {TextureActionId = 85, CooldownActionId = 85, StatusId = 1864, MaxStatusDuration = 20}); // Lance Charge
			bar2.Add(new(bar2) {TextureActionId = 7398, CooldownActionId = 7398, StatusId = 1910, MaxStatusDuration = 20}); // Dragon Sight (Right Eye)
			bar2.Add(new(bar2) {TextureActionId = 3557, CooldownActionId = 3557, StatusId = 786, MaxStatusDuration = 15, StatusSourcePlayer = false}); // Battle Litany
			hud.AddBar(bar2);

			base.Configure(hud);

			// Life of the Dragon
			hud.AddAlert(new()
			{
				Image = Plugin.AssemblyLocation + "Media\\Images\\Overlays\\berserk.png",
				CustomCondition = HasLifeOfTheDragon,
				CustomDuration = GetLifeOfTheDragonDuration,
				Size = new Vector2(256, 128) * 0.8f,
				Position = new(0, -180),
				MaxDuration = 30,
				Level = 70
			});
		}

		private static bool HasLifeOfTheDragon() => GetLifeOfTheDragonDuration() > 0;

		private static float GetLifeOfTheDragonDuration()
		{
			DRGGauge gauge = Plugin.JobGauges.Get<DRGGauge>();
			return gauge.LOTDTimer / 1000f;
		}
	}
}