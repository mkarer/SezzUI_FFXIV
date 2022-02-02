using System.Linq;
using SezzUI.Helpers;

namespace SezzUI.Modules.JobHud.Jobs
{
	public sealed class WAR : BasePreset
	{
		public override uint JobId => JobIDs.WAR;

		public override void Configure(JobHud hud)
		{
			byte jobLevel = Plugin.ClientState.LocalPlayer?.Level ?? 0;

			Bar bar1 = new(hud);
			bar1.Add(new(bar1) {TextureStatusId = 2677, StatusId = 2677, MaxStatusDuration = 60}); // Surging Tempest
			bar1.Add(new(bar1) {TextureActionId = 38, CooldownActionId = 38, StatusActionId = 38, MaxStatusDuration = 15, GlowBorderStatusIds = new[] {1177u, 9u}, Features = IconFeatures.GlowIgnoresState}); // Berserk
			bar1.Add(new(bar1) {TextureActionId = 7386, CooldownActionId = 7386}); // Onslaught
			bar1.Add(new(bar1) {TextureActionId = 52, CooldownActionId = 52, StatusId = 1897, MaxStatusDuration = 30, RequiresCombat = true}); // Infuriate
			bar1.Add(new(bar1) {TextureActionId = 3551, CooldownActionId = 3551, StatusIds = new[] {735u, 1857u}, MaxStatusDuration = jobLevel >= 82 ? 8 : 6}); // Raw Intuition/Nascent Flash
			hud.AddBar(bar1);

			Bar bar2 = new(hud);
			bar2.Add(new(bar2) {TextureActionId = 7531, CooldownActionId = 7531, StatusId = 1191, MaxStatusDuration = 20}); // Rampart
			bar2.Add(new(bar2) {TextureActionId = 40, CooldownActionId = 40, StatusId = 87, MaxStatusDuration = 10}); // Thrill of Battle
			bar2.Add(new(bar2) {TextureActionId = 7388, CooldownActionId = 7388, StatusId = 1457, MaxStatusDuration = 15, GlowBorderStatusIds = new[] {87u, 89u, 735u}}); // Shake It Off
			bar2.Add(new(bar2) {TextureActionId = 44, CooldownActionId = 44, StatusId = 89, MaxStatusDuration = 15}); // Vengeance
			bar2.Add(new(bar2) {TextureActionId = 43, CooldownActionId = 43, StatusId = 409, MaxStatusDuration = 10}); // Holmgang
			hud.AddBar(bar2);

			base.Configure(hud);

			Bar roleBar = hud.Bars.Last();
			roleBar.Add(new(roleBar) {TextureActionId = 3552, CooldownActionId = 3552}, 2); // Equilibrium
		}
	}
}