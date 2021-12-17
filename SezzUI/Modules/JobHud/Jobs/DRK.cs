namespace SezzUI.Modules.JobHud.Jobs
{
    public sealed class DRK : BasePreset
    {
        public override uint JobId => 32;

        public override void Configure(JobHud hud)
        {
            using (Bar bar = new())
            {
                bar.Add(new Icon(bar) { TextureActionId = 3625, CooldownActionId = 3625, StatusActionId = 3625, MaxStatusDuration = 10, StatusTarget = Enums.Unit.Player, Level = 35 }); // Blood Weapon
                bar.Add(new Icon(bar) { TextureActionId = 7390, CooldownActionId = 7390, StatusActionId = 7390, MaxStatusDuration = 15, StatusTarget = Enums.Unit.Player, Level = 68 }); // Delirium
                bar.Add(new Icon(bar) { TextureActionId = 3640, CooldownActionId = 3640, Level = 56 }); // Plunge
                bar.Add(new Icon(bar) { TextureActionId = 7393, CooldownActionId = 7393, StatusActionId = 7393, MaxStatusDuration = 7, StatusTarget = Enums.Unit.Player, Level = 70 }); // The Blackest Night
                bar.Add(new Icon(bar) { TextureActionId = 7548, CooldownActionId = 7548, StatusId = 1209, MaxStatusDuration = 6, StatusTarget = Enums.Unit.Player, Level = 32 }); // Arm's Length
                hud.AddBar(bar);
            }

            using (Bar bar = new())
            {
                bar.Add(new Icon(bar) { TextureActionId = 7531, CooldownActionId = 7531, StatusId = 1191, MaxStatusDuration = 20, StatusTarget = Enums.Unit.Player, Level = 8 }); // Rampart
                bar.Add(new Icon(bar) { TextureActionId = 3636, CooldownActionId = 3636, StatusActionId = 3636, MaxStatusDuration = 15, StatusTarget = Enums.Unit.Player, Level = 38 }); // Shadow Wall
                bar.Add(new Icon(bar) { TextureActionId = 3634, CooldownActionId = 3634, StatusActionId = 3634, MaxStatusDuration = 10, StatusTarget = Enums.Unit.Player, Level = 45 }); // Dark Mind
                bar.Add(new Icon(bar) { TextureActionId = 16471, CooldownActionId = 16471, StatusActionId = 16471, MaxStatusDuration = 15, StatusTarget = Enums.Unit.Player, Level = 76 }); // Dark Missionary
                bar.Add(new Icon(bar) { TextureActionId = 3638, CooldownActionId = 3638, StatusActionId = 3638, MaxStatusDuration = 10, StatusTarget = Enums.Unit.Player, Level = 50 }); // Living Dead
                hud.AddBar(bar);
            }
        }
    }
}
