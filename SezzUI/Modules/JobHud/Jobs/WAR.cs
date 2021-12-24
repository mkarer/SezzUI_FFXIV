using System.Linq;

namespace SezzUI.Modules.JobHud.Jobs
{
    public sealed class WAR : BasePreset
    {
        public override uint JobId => DelvUI.Helpers.JobIDs.WAR;
        
        public override void Configure(JobHud hud)
        {
            using (Bar bar = new(hud))
            {
                bar.Add(new Icon(bar) { TextureStatusId = 2677, StatusId = 2677, MaxStatusDuration = 60, StatusTarget = Enums.Unit.Player, Level = 50 }); // Surging Tempest
                bar.Add(new Icon(bar) { TextureActionId = 38, CooldownActionId = 38, StatusActionId = 38, MaxStatusDuration = 15, StatusTarget = Enums.Unit.Player, GlowBorderStatusIds = new[] { (uint)1177, (uint)9 }, Features = IconFeatures.GlowIgnoresState, Level = 6 }); // Berserk
                bar.Add(new Icon(bar) { TextureActionId = 7386, CooldownActionId = 7386, Level = 62 }); // Onslaught
                bar.Add(new Icon(bar) { TextureActionId = 52, CooldownActionId = 52, StatusId = 1897, MaxStatusDuration = 30, StatusTarget = Enums.Unit.Player, Level = 50 }); // Infuriate
                bar.Add(new Icon(bar) { TextureActionId = 16464, CooldownActionId = 16464, StatusId = 1857, MaxStatusDuration = 6, StatusTarget = Enums.Unit.Player, Level = 76 }); // Nascent Flash
                bar.Add(new Icon(bar) { TextureActionId = 3551, CooldownActionId = 3551, StatusId = 735, MaxStatusDuration = 6, StatusTarget = Enums.Unit.Player, Level = 56 }); // Raw Intuition
                hud.AddBar(bar);
            }

            using (Bar bar = new(hud))
            {
                bar.Add(new Icon(bar) { TextureActionId = 7531, CooldownActionId = 7531, StatusId = 1191, MaxStatusDuration = 20, StatusTarget = Enums.Unit.Player, Level = 8 }); // Rampart
                bar.Add(new Icon(bar) { TextureActionId = 40, CooldownActionId = 40, StatusId = 87, MaxStatusDuration = 10, StatusTarget = Enums.Unit.Player, Level = 30 }); // Thrill of Battle
                bar.Add(new Icon(bar) { TextureActionId = 7388, CooldownActionId = 7388, StatusId = 1457, MaxStatusDuration = 15, StatusTarget = Enums.Unit.Player, GlowBorderStatusIds = new[] { (uint)87, (uint)89, (uint)735 }, Level = 68 }); // Shake It Off
                bar.Add(new Icon(bar) { TextureActionId = 44, CooldownActionId = 44, StatusId = 89, MaxStatusDuration = 15, StatusTarget = Enums.Unit.Player, Level = 38 }); // Vengeance
                bar.Add(new Icon(bar) { TextureActionId = 43, CooldownActionId = 43, StatusId = 409, MaxStatusDuration = 10, StatusTarget = Enums.Unit.Player, Level = 42 }); // Holmgang
                hud.AddBar(bar);
            }

            base.Configure(hud);

            Bar roleBar = hud.Bars.Last();
            roleBar.Add(new Icon(roleBar) { TextureActionId = 3552, CooldownActionId = 3552, Level = 58 }, 2); // Equilibrium
        }
    }
}
