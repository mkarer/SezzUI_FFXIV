using System.Linq;
using System.Numerics;
using DelvUI.Helpers;

namespace SezzUI.Modules.JobHud.Jobs
{
	public sealed class DNC : BasePreset
	{
		public override uint JobId => JobIDs.DNC;

		public override void Configure(JobHud hud)
		{
			Bar bar1 = new(hud);
			bar1.Add(new(bar1) {TextureActionId = 15997, CooldownActionId = 15997, StatusIds = new[] {(uint) 1818, (uint) 1821, (uint) 2024, (uint) 2105, (uint) 2113}, MaxStatusDurations = new[] {15f, 60f, 60f, 60f, 60f}, StatusSourcePlayer = false, GlowBorderStatusId = 1818, Features = IconFeatures.GlowIgnoresState}); // Standard Step
			bar1.Add(new(bar1) {TextureActionId = 15998, CooldownActionId = 15998, StatusIds = new[] {(uint) 1819, (uint) 1822, (uint) 2050}, MaxStatusDurations = new[] {15f, 20f, 20f}, StatusSourcePlayer = false, GlowBorderStatusId = 1819, Features = IconFeatures.GlowIgnoresState}); // Technical Step
			bar1.Add(new(bar1) {TextureActionId = 16013, CooldownActionId = 16013}); // Flourish
			bar1.Add(new(bar1) {TextureActionId = 16010, CooldownActionId = 16010, RequiresCombat = true}); // En Avant
			bar1.Add(new(bar1) {TextureActionId = 16011, CooldownActionId = 16011, StatusId = 1825, MaxStatusDuration = 20}); // Devilment
			hud.AddBar(bar1);

			base.Configure(hud);

			Bar roleBar = hud.Bars.Last();
			roleBar.Add(new(roleBar) {TextureActionId = 16015, CooldownActionId = 16015}, 1); // Curing Waltz
			roleBar.Add(new(roleBar) {TextureActionId = 16012, CooldownActionId = 16012, StatusId = 1826, MaxStatusDuration = 15}, 1); // Shield Samba
			roleBar.Add(new(roleBar) {TextureActionId = 16014, CooldownActionId = 16014, StatusId = 1827, MaxStatusDuration = 15, StacksStatusId = 2696, GlowBorderStatusId = 2696, Features = IconFeatures.GlowIgnoresState}, 1); // Improvisation

			// Threefold Fan Dance
			hud.AddAlert(new()
			{
				StatusId = 1820,
				Image = Plugin.AssemblyLocation + "Media\\Images\\Overlays\\backlash_green.png",
				Size = new Vector2(256, 128) * 0.8f,
				Position = new(0, -180),
				MaxDuration = 30,
				Level = 66
			});
		}
	}
}