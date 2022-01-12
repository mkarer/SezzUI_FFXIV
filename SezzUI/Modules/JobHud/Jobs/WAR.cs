using System.Linq;

namespace SezzUI.Modules.JobHud.Jobs
{
    public sealed class WAR : BasePreset
    {
        public override uint JobId => DelvUI.Helpers.JobIDs.WAR;

        public override void Configure(JobHud hud)
        {
            byte jobLevel = Plugin.ClientState.LocalPlayer?.Level ?? 0;

            Bar bar1 = new(hud);
            bar1.Add(new Icon(bar1) { TextureStatusId = 2677, StatusId = 2677, MaxStatusDuration = 60, StatusTarget = Enums.Unit.Player, Level = 50 }); // Surging Tempest
            bar1.Add(new Icon(bar1) { TextureActionId = 38, CooldownActionId = 38, StatusActionId = 38, MaxStatusDuration = 15, StatusTarget = Enums.Unit.Player, GlowBorderStatusIds = new[] { (uint)1177, (uint)9 }, Features = IconFeatures.GlowIgnoresState, Level = 6 }); // Berserk
            bar1.Add(new Icon(bar1) { TextureActionId = 7386, CooldownActionId = 7386, Level = 62 }); // Onslaught
            bar1.Add(new Icon(bar1) { TextureActionId = 52, CooldownActionId = 52, StatusId = 1897, MaxStatusDuration = 30, StatusTarget = Enums.Unit.Player, RequiresCombat = true, Level = 50 }); // Infuriate
            bar1.Add(new Icon(bar1) { TextureActionId = 3551, CooldownActionId = 3551, StatusIds = new[] { (uint)735, (uint)1857 }, MaxStatusDuration = jobLevel >= 82 ? 8 : 6, StatusTarget = Enums.Unit.Player, Level = 56 }); // Raw Intuition/Nascent Flash
            hud.AddBar(bar1);

            Bar bar2 = new(hud);
            bar2.Add(new Icon(bar2) { TextureActionId = 7531, CooldownActionId = 7531, StatusId = 1191, MaxStatusDuration = 20, StatusTarget = Enums.Unit.Player, Level = 8 }); // Rampart
            bar2.Add(new Icon(bar2) { TextureActionId = 40, CooldownActionId = 40, StatusId = 87, MaxStatusDuration = 10, StatusTarget = Enums.Unit.Player, Level = 30 }); // Thrill of Battle
            bar2.Add(new Icon(bar2) { TextureActionId = 7388, CooldownActionId = 7388, StatusId = 1457, MaxStatusDuration = 15, StatusTarget = Enums.Unit.Player, GlowBorderStatusIds = new[] { (uint)87, (uint)89, (uint)735 }, Level = 68 }); // Shake It Off
            bar2.Add(new Icon(bar2) { TextureActionId = 44, CooldownActionId = 44, StatusId = 89, MaxStatusDuration = 15, StatusTarget = Enums.Unit.Player, Level = 38 }); // Vengeance
            bar2.Add(new Icon(bar2) { TextureActionId = 43, CooldownActionId = 43, StatusId = 409, MaxStatusDuration = 10, StatusTarget = Enums.Unit.Player, Level = 42 }); // Holmgang
            hud.AddBar(bar2);

            base.Configure(hud);

            Bar roleBar = hud.Bars.Last();
            roleBar.Add(new Icon(roleBar) { TextureActionId = 3552, CooldownActionId = 3552, Level = 58 }, 2); // Equilibrium
        }
    }
}
