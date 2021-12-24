namespace SezzUI.Modules.JobHud.Jobs
{
    public sealed class GNB : BasePreset
    {
		public override uint JobId => DelvUI.Helpers.JobIDs.GNB;

		public override void Configure(JobHud hud)
		{
            using (Bar bar = new(hud))
            {
                bar.Add(new Icon(bar) { TextureActionId = 16138, CooldownActionId = 16138, StatusId = 1831, MaxStatusDuration = 20, StatusTarget = Enums.Unit.Player, Level = 2 }); // No Mercy
                bar.Add(new Icon(bar) { TextureActionId = 16154, CooldownActionId = 16154, Level = 56 }); // Rough Divide
                bar.Add(new Icon(bar) { TextureActionId = 16164, CooldownActionId = 16164, Level = 76 }); // Bloodfest
                bar.Add(new Icon(bar) { TextureActionId = 16151, CooldownActionId = 16151, StatusId = 1835, MaxStatusDuration = 18, StatusTarget = Enums.Unit.Player, Level = 45 }); // Aurora
                bar.Add(new Icon(bar) { TextureActionId = 16160, CooldownActionId = 16160, StatusId = 1839, MaxStatusDuration = 15, StatusTarget = Enums.Unit.Player, Level = 64 }); // Heart of Light
                hud.AddBar(bar);
            }

            using (Bar bar = new(hud))
            {
                bar.Add(new Icon(bar) { TextureActionId = 7531, CooldownActionId = 7531, StatusId = 1191, MaxStatusDuration = 20, StatusTarget = Enums.Unit.Player, Level = 8 }); // Rampart
                bar.Add(new Icon(bar) { TextureActionId = 16140, CooldownActionId = 16140, StatusId = 1832, MaxStatusDuration = 20, StatusTarget = Enums.Unit.Player, Level = 6 }); // Camouflage
                bar.Add(new Icon(bar) { TextureActionId = 16148, CooldownActionId = 16148, StatusId = 1834, MaxStatusDuration = 15, StatusTarget = Enums.Unit.Player, Level = 38 }); // Nebula
                bar.Add(new Icon(bar)
                {
                    // Heart of Stone
                    // @82: 28758 Heart of Corundum
                    TextureActionId = 16161, CooldownActionId = 16161, StatusId = 1840, MaxStatusDuration = 7, StatusTarget = Enums.Unit.Player, Level = 68,
                    GlowBorderStatusId = 1898
                });
                bar.Add(new Icon(bar) { TextureActionId = 16152, CooldownActionId = 16152, StatusId = 1836, MaxStatusDuration = 10, StatusTarget = Enums.Unit.Player, Level = 50 }); // Superbolide
                hud.AddBar(bar);
            }

            base.Configure(hud);
        }
    }
}
