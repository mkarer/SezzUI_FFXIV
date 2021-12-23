using System.Numerics;

namespace SezzUI.Modules.JobHud.Jobs
{
    public sealed class PLD : BasePreset
    {
        public override uint JobId => DelvUI.Helpers.JobIDs.PLD;
        
        public override void Configure(JobHud hud)
        {
            byte jobLevel = Plugin.ClientState.LocalPlayer?.Level ?? 0;

            using (Bar bar = new(hud))
            {
                bar.Add(new Icon(bar) { TextureActionId = 20, CooldownActionId = 20, StatusId = 76, MaxStatusDuration = 25, StatusTarget = Enums.Unit.Player, Level = 2 }); // Fight of Flight
                bar.Add(new Icon(bar) { TextureActionId = 3538, StatusId = 725, MaxStatusDuration = 21, StatusTarget = Enums.Unit.Target, Level = 54 }); // Goring Blade
                bar.Add(new Icon(bar) { TextureActionId = 16461, CooldownActionId = 16461, Level = 74 }); // Intervene
                bar.Add(new Icon(bar) { TextureActionId = 3542, CooldownActionId = 3542, StatusId = 1856, MaxStatusDuration = jobLevel >= 74 ? 6 : 4, StatusTarget = Enums.Unit.Player, RequiredPowerType = Helpers.JobsHelper.PowerType.Oath, RequiredPowerAmount = 50, GlowBorderUsable = true, Level = 35 }); // Sheltron
                bar.Add(new Icon(bar) { TextureActionId = 7382, CooldownActionId = 7382, StatusId = 1174, MaxStatusDuration = 6, StatusTarget = Enums.Unit.Target, GlowBorderStatusIds = new[] { (uint)1191, (uint)74 }, RequiredPowerType = Helpers.JobsHelper.PowerType.Oath, RequiredPowerAmount = 50, Level = 62 }); // Intervention
                bar.Add(new Icon(bar) { TextureActionId = 7383, CooldownActionId = 7383, StatusId = 1368, MaxStatusDuration = 30, StatusTarget = Enums.Unit.Player, Level = 68 }); // Requiescat

                hud.AddBar(bar);
            }

            using (Bar bar = new(hud))
            {
                bar.Add(new Icon(bar) { TextureActionId = 7531, CooldownActionId = 7531, StatusId = 1191, MaxStatusDuration = 20, StatusTarget = Enums.Unit.Player, Level = 8 }); // Rampart
                bar.Add(new Icon(bar) { TextureActionId = 17, CooldownActionId = 17, StatusId = 74, MaxStatusDuration = 15, StatusTarget = Enums.Unit.Player, Level = 38 }); // Sentinel
                bar.Add(new Icon(bar) { TextureActionId = 7385, CooldownActionId = 7385, StatusId = 1175, MaxStatusDuration = 18, StatusTarget = Enums.Unit.Player, Level = 70 }); // Passage of Arms
                bar.Add(new Icon(bar) { TextureActionId = 3540, CooldownActionId = 3540, StatusId = 726, MaxStatusDuration = 30, StatusTarget = Enums.Unit.Player, Level = 56, GlowBorderStatusId = 726, Features = IconFeatures.GlowIgnoresState }); // Divine Veil
                bar.Add(new Icon(bar) { TextureActionId = 30, CooldownActionId = 30, StatusId = 82, MaxStatusDuration = 10, StatusTarget = Enums.Unit.Player, Level = 50 }); // Hallowed Ground
                hud.AddBar(bar);
            }

            // Confiteor
            hud.AddAlert(new AuraAlert
            {
                StatusId = 1368,
                ExactStacks = 1,
                Image = Plugin.AssemblyLocation + "Media\\Images\\Overlays\\hand_of_light.png",
                Size = new Vector2(256, 128),
                Position = new Vector2(0, -180),
                Level = 80
            });


            base.Configure(hud);
        }
    }
}
