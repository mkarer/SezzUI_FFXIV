using System.Numerics;
using System.Linq;

namespace SezzUI.Modules.JobHud.Jobs
{
    public sealed class SGE : BasePreset
    {
        public override uint JobId => DelvUI.Helpers.JobIDs.SGE;

        public override void Configure(JobHud hud)
        {
            Bar bar1 = new(hud);
            bar1.Add(new Icon(bar1) { TextureActionId = 24293, StatusIds = new[] { (uint)2614, (uint)2615, (uint)2616 }, MaxStatusDuration = 30, StatusTarget = Enums.Unit.Target, RequiredPowerAmount = 400, RequiredPowerType = Helpers.JobsHelper.PowerType.MP, Level = 30 }); // Eukrasian Dosis
            bar1.Add(new Icon(bar1) { TextureActionId = 24298, CooldownActionId = 24298, StatusIds = new[] { (uint)2618, (uint)2938, (uint)3003 }, MaxStatusDurations = new[] { 15f, 15f, 20f }, StatusTarget = Enums.Unit.Player, RequiredPowerType = Helpers.JobsHelper.PowerType.Addersgall, RequiredPowerAmount = 1, Level = 50 }); // Kerachole/Holos
            bar1.Add(new Icon(bar1) { TextureActionId = 24310, CooldownActionId = 24310, StatusIds = new[] { (uint)2618, (uint)2938, (uint)3003 }, MaxStatusDurations = new[] { 15f, 15f, 20f }, StatusTarget = Enums.Unit.Player, Level = 76 }); // Holos
                                                                                                                                                                                                                                                  //bar.Add(new Icon(bar) { TextureActionId = 24288, CooldownActionId = 24288, StatusActionId = 24288, MaxStatusDuration = 15, StatusTarget = Enums.Unit.Player, Level = 20 }); // Physis
            bar1.Add(new Icon(bar1) { TextureActionId = 24305, CooldownActionId = 24305, StatusId = 2612, MaxStatusDuration = 15, StatusTarget = Enums.Unit.TargetOrPlayer, Level = 70 }); // Haima
            bar1.Add(new Icon(bar1) { TextureActionId = 24311, CooldownActionId = 24311, StatusId = 2613, MaxStatusDuration = 15, StatusTarget = Enums.Unit.Player, Level = 80 }); // Panhaima
            hud.AddBar(bar1);

            Bar bar2 = new(hud);
            bar2.Add(new Icon(bar2) { TextureActionId = 24294, CooldownActionId = 24294, StatusId = 2610, MaxStatusDuration = 15, StatusTarget = Enums.Unit.Player, Level = 35 }); // Soteria
            bar2.Add(new Icon(bar2) { TextureActionId = 24295, CooldownActionId = 24295, Level = 40 }); // Icarus
                                                                                                        //bar.Add(new Icon(bar) { TextureActionId = 24300, CooldownActionId = 24300, StatusId = 2611, MaxStatusDuration = 30, StatusTarget = Enums.Unit.Player, Level = 56 }); // Zoe
            bar2.Add(new Icon(bar2) { TextureActionId = 24317, CooldownActionId = 24317, StatusId = 2622, MaxStatusDuration = 10, StatusTarget = Enums.Unit.TargetOrPlayer, Level = 86 }); // Krasis
            bar2.Add(new Icon(bar2) { TextureActionId = 24289, CooldownActionId = 24289, RequiredPowerAmount = 400, RequiredPowerType = Helpers.JobsHelper.PowerType.MP, Level = 26 }); // Phlegma
            bar2.Add(new Icon(bar2) { TextureActionId = 24318, CooldownActionId = 24318, RequiredPowerAmount = 700, RequiredPowerType = Helpers.JobsHelper.PowerType.MP, Level = 90 }); // Pneuma
            hud.AddBar(bar2);

            // Kardia
            using (AuraAlert aa = new())
            {
                aa.StatusId = 2604;
                aa.InvertCheck = true;
                aa.Size = new Vector2(48, 48);
                aa.Position = new Vector2(0, -220);
                aa.Level = 4;
                aa.BorderSize = 1;
                aa.UseActionIcon(24285);
                aa.GlowBackdrop = true;
                aa.GlowColor = new Vector4(74f / 255f, 137f / 255f, 214f / 255f, 0.5f);
                aa.GlowBackdropSize = 4;
                hud.AddAlert(aa);
            };

            // Eukrasia
            hud.AddAlert(new AuraAlert
            {
                StatusId = 2606,
                Image = Plugin.AssemblyLocation + "Media\\Images\\Overlays\\genericarc_05_90.png",
                Size = new Vector2(256, 128) * 0.9f,
                Position = new Vector2(0, -60),
                Color = new Vector4(0 / 255f, 221f / 255f, 210f / 255f, 1f),
                Level = 30
            });

            base.Configure(hud);

            Bar roleBar = hud.Bars.Last();
            roleBar.Add(new Icon(roleBar) { TextureActionId = 24309, CooldownActionId = 24309, Level = 74 }, 2); // Rhizomata
        }
    }
}
