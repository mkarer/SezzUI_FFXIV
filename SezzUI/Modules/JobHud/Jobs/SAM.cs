using System.Linq;

namespace SezzUI.Modules.JobHud.Jobs
{
    public sealed class SAM : BasePreset
    {
        public override uint JobId => DelvUI.Helpers.JobIDs.SAM;

        public override void Configure(JobHud hud)
        {
            using (Bar bar = new(hud))
            {
                bar.Add(new Icon(bar) { TextureStatusId = 1299, StatusId = 1299, MaxStatusDuration = 40, StatusTarget = Enums.Unit.Player, Level = 18 }); // Fuka
                bar.Add(new Icon(bar) { TextureStatusId = 1298, StatusId = 1298, MaxStatusDuration = 40, StatusTarget = Enums.Unit.Player, Level = 4 }); // Fugetsu
                bar.Add(new Icon(bar) { TextureActionId = 7489, StatusIds = new[] { (uint)1228, (uint)1319 }, MaxStatusDuration = 60, StatusTarget = Enums.Unit.Target, CustomPowerCondition = CanUseHiganbana, Level = 30 }); // Higanbana
                hud.AddBar(bar);
            }

            using (Bar bar = new(hud))
            {
                bar.Add(new Icon(bar) { TextureActionId = 16482, CooldownActionId = 16482, RequiredPowerType = Helpers.JobsHelper.PowerType.Kenki, RequiredPowerAmountMax = 50, Level = 68 }); // Ikishoten
                bar.Add(new Icon(bar) { TextureActionId = 7497, CooldownActionId = 7497, StatusId = 1231, MaxStatusDuration = 15, StatusTarget = Enums.Unit.Player, RequiredPowerType = Helpers.JobsHelper.PowerType.MeditationStacks, RequiredPowerAmountMax = 2, StacksPowerType = Helpers.JobsHelper.PowerType.MeditationStacks, Level = 60 }); // Meditate
                bar.Add(new Icon(bar) { TextureActionId = 7494, StatusId = 1229, MaxStatusDuration = 10, StatusTarget = Enums.Unit.Player, RequiredPowerType = Helpers.JobsHelper.PowerType.Kenki, RequiredPowerAmount = 20, Level = 52 }); // Hissatu: Kaiten
                bar.Add(new Icon(bar) { TextureActionId = 7499, CooldownActionId = 7499, StatusId = 1233, MaxStatusDuration = 15, StatusTarget = Enums.Unit.Player, Level = 50 }); // Meikyo Shisui
                hud.AddBar(bar);
            }

            base.Configure(hud);

            Bar roleBar = hud.Bars.Last();
            roleBar.Add(new Icon(roleBar) { TextureActionId = 7498, CooldownActionId = 7498, StatusId = 1232, MaxStatusDuration = 3, StatusTarget = Enums.Unit.Player, Level = 6 }, 1); // Third Eye
        }

        private static bool CanUseHiganbana()
        {
            return Helpers.JobsHelper.GetPower(Helpers.JobsHelper.PowerType.Sen).Item1 == 1;
        }
    }
}
