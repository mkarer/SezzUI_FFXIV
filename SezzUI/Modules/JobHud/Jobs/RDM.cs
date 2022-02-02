using System.Linq;
using System.Numerics;
using Dalamud.Game.ClientState.JobGauge.Types;
using SezzUI.Helpers;

namespace SezzUI.Modules.JobHud.Jobs
{
	public sealed class RDM : BasePreset
	{
		public override uint JobId => JobIDs.RDM;

		public override void Configure(JobHud hud)
		{
			Bar bar1 = new(hud);
			bar1.Add(new(bar1) {TextureActionId = 7518, CooldownActionId = 7518, StatusId = 1238, MaxStatusDuration = 20}); // Acceleration
			bar1.Add(new(bar1) {TextureActionId = 7506, CooldownActionId = 7506}); // Corps-a-corps
			bar1.Add(new(bar1) {TextureActionId = 7515, CooldownActionId = 7515}); // Displacement
			bar1.Add(new(bar1) {TextureActionId = 7521, CooldownActionId = 7521, CustomPowerCondition = IsManaficationNotOvercapping, RequiresCombat = true}); // Manafication
			bar1.Add(new(bar1) {TextureActionId = 7520, CooldownActionId = 7520, StatusId = 1239, MaxStatusDuration = 20, StatusSourcePlayer = false}); // Embolden
			hud.AddBar(bar1);

			// TODO: Arrows for Mana (<< White | Black >>)

			// Dualcast
			hud.AddAlert(new()
			{
				StatusId = 1249,
				Image = Plugin.AssemblyLocation + "Media\\Images\\Overlays\\genericarc_05_90.png",
				Size = new Vector2(256, 128) * 0.9f,
				Position = new(0, -180),
				Color = new(249f / 255f, 51f / 255f, 243f / 255f, 1f),
				MaxDuration = 15,
				TextOffset = new(0, -28)
			});

			// Verstone
			hud.AddAlert(new()
			{
				StatusId = 1235,
				Image = Plugin.AssemblyLocation + "Media\\Images\\Overlays\\genericarc_01.png",
				Size = new Vector2(128, 256) * 0.8f,
				Position = new(-160, 0),
				Color = new(255f / 255f, 250f / 255f, 174f / 255f, 1f),
				MaxDuration = 30,
				Level = 30,
				TextOffset = new(-8, 0)
			});

			// Verfire
			hud.AddAlert(new()
			{
				StatusId = 1234,
				Image = Plugin.AssemblyLocation + "Media\\Images\\Overlays\\genericarc_01.png",
				Size = new Vector2(128, 256) * 0.8f,
				Position = new(160, 0),
				Color = new(246f / 255f, 176f / 255f, 64f / 255f, 1f),
				MaxDuration = 30,
				Level = 26,
				FlipImageHorizontally = true,
				TextOffset = new(8, 0)
			});

			base.Configure(hud);

			Bar roleBar = hud.Bars.Last();
			roleBar.Add(new(roleBar) {TextureActionId = 25857, CooldownActionId = 25857, StatusId = 2707, MaxStatusDuration = 10, StatusSourcePlayer = false}, 1); // Magick Barrier
		}

		private static bool IsManaficationNotOvercapping()
		{
			RDMGauge gauge = Plugin.JobGauges.Get<RDMGauge>();
			return gauge.BlackMana <= 50 && gauge.WhiteMana <= 50;
		}
	}
}