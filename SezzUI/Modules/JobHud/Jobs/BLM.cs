using System.Linq;
using System.Numerics;
using Dalamud.Game.ClientState.JobGauge.Types;
using SezzUI.Enums;
using SezzUI.Helper;

namespace SezzUI.Modules.JobHud.Jobs
{
	public sealed class BLM : BasePreset
	{
		public override uint JobId => JobIDs.BLM;

		public override void Configure(JobHud hud)
		{
			Bar bar1 = new(hud);
			bar1.Add(new(bar1) {TextureActionId = 144, StatusIds = new[] {161u, 162u, 163u, 1210u}, MaxStatusDurations = new[] {21f, 18f, 30f, 18f}, StatusTarget = Unit.Target, GlowBorderStatusId = 164, Features = IconFeatures.GlowIgnoresState}); // Thunder
			bar1.Add(new(bar1) {TextureActionId = 25796, CooldownActionId = 25796, CustomPowerCondition = IsInAstralFireOrIsInUmbralIce, GlowBorderUsable = true}); // Amplifier
			bar1.Add(new(bar1) {TextureActionId = 3574, CooldownActionId = 3574, StatusId = 867, MaxStatusDuration = 30}); // Sharpcast
			bar1.Add(new(bar1) {TextureActionId = 3573, CooldownActionId = 3573, StatusId = 737, MaxStatusDuration = 30, GlowBorderStatusId = 738, GlowBorderInvertCheck = true, GlowBorderStatusIdForced = 737, GlowBorderUsable = true}); // Ley Lines
			hud.AddBar(bar1);

			// Firestarter
			hud.AddAlert(new()
			{
				StatusId = 165,
				Image = "impact.png",
				Size = new Vector2(256, 128) * 0.8f,
				Position = new(0, -180),
				MaxDuration = 30,
				Level = 42
			});

			// Polyglots: 1
			hud.AddAlert(new()
			{
				PowerType = JobsHelper.PowerType.PolyglotStacks,
				ExactPowerAmount = 1,
				Image = "arcane_missiles_1.png",
				Size = new(128, 256),
				Position = new(-200, 50),
				Level = 70
			});

			// Polyglots: 2
			hud.AddAlert(new()
			{
				PowerType = JobsHelper.PowerType.PolyglotStacks,
				ExactPowerAmount = 2,
				Image = "arcane_missiles_2.png",
				Size = new(128, 256),
				Position = new(-220, 50),
				Level = 80
			});

			// Paradox
			hud.AddAlert(new()
			{
				CustomCondition = IsParadoxActive,
				Size = new(128, 256),
				Image = "echo_of_the_elements.png",
				Position = new(200, 50),
				FlipImageHorizontally = true,
				Level = 90
			});

			base.Configure(hud);

			Bar roleBar = hud.Bars.Last();
			roleBar.Add(new(roleBar) {TextureActionId = 157, CooldownActionId = 157, StatusId = 168, MaxStatusDuration = 20}, 1); // Manaward
			roleBar.Add(new(roleBar) {TextureActionId = 7421, CooldownActionId = 7421, StatusId = 1211, MaxStatusDuration = 15}, 1); // Triplecast
		}

		private static bool IsInAstralFireOrIsInUmbralIce()
		{
			BLMGauge gauge = Services.JobGauges.Get<BLMGauge>();
			return gauge.InAstralFire || gauge.InUmbralIce;
		}

		private static bool IsParadoxActive() => Services.JobGauges.Get<BLMGauge>().IsParadoxActive;
	}
}