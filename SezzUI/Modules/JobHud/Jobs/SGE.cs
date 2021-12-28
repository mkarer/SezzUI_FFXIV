using System.Numerics;

namespace SezzUI.Modules.JobHud.Jobs
{
    public sealed class SGE : BasePreset
    {
        public override uint JobId => DelvUI.Helpers.JobIDs.SGE;
        
        public override void Configure(JobHud hud)
        {
            using (Bar bar = new(hud))
            {
                bar.Add(new Icon(bar) { TextureActionId = 24293, StatusIds = new[] { (uint)2614, (uint)2615, (uint)2616 }, MaxStatusDuration = 30, StatusTarget = Enums.Unit.Target, Level = 30 }); // Eukrasian Dosis
                bar.Add(new Icon(bar) { TextureActionId = 24298, CooldownActionId = 24298, StatusIds = new[] { (uint)2618, (uint)2938, (uint)3003 }, MaxStatusDuration = 15, StatusTarget = Enums.Unit.Player, RequiredPowerType = Helpers.JobsHelper.PowerType.Addersgall, RequiredPowerAmount = 1, Level = 50 }); // Kerachole/Holos
                bar.Add(new Icon(bar) { TextureActionId = 24288, CooldownActionId = 24288, StatusActionId = 24288, MaxStatusDuration = 15, StatusTarget = Enums.Unit.Player, Level = 20 }); // Physis
                bar.Add(new Icon(bar) { TextureActionId = 24305, CooldownActionId = 24305, StatusId = 2612, MaxStatusDuration = 15, StatusTarget = Enums.Unit.TargetOrPlayer, Level = 70 }); // Haima
                bar.Add(new Icon(bar) { TextureActionId = 24311, CooldownActionId = 24311, StatusId = 2613, MaxStatusDuration = 15, StatusTarget = Enums.Unit.Player, Level = 80 }); // Panhaima
                hud.AddBar(bar);
            }

            using (Bar bar = new(hud))
            {
                bar.Add(new Icon(bar) { TextureActionId = 24294, CooldownActionId = 24294, StatusId = 2610, MaxStatusDuration = 15, StatusTarget = Enums.Unit.Player, Level = 35 }); // Soteria
                bar.Add(new Icon(bar) { TextureActionId = 24295, CooldownActionId = 24295, Level = 40 }); // Icarus
                bar.Add(new Icon(bar) { TextureActionId = 24300, CooldownActionId = 24300, StatusId = 2611, MaxStatusDuration = 30, StatusTarget = Enums.Unit.Player, Level = 56 }); // Zoe
                bar.Add(new Icon(bar) { TextureActionId = 24309, CooldownActionId = 24309, Level = 74 }); // Rhizomata
                bar.Add(new Icon(bar) { TextureActionId = 24317, CooldownActionId = 24317, StatusId = 2622, MaxStatusDuration = 10, StatusTarget = Enums.Unit.TargetOrPlayer, Level = 86 }); // Krasis
                hud.AddBar(bar);
            }

            // Kardia
            using (AuraAlert aa = new())
            {
                aa.StatusId = 2604;
                aa.InvertCheck = true;
                aa.Size = new Vector2(48, 48);
                aa.Position = new Vector2(100, 20);
                aa.Level = 4;
                aa.BorderSize = 1;
                aa.UseActionIcon(24285);
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
        }
    }
}
