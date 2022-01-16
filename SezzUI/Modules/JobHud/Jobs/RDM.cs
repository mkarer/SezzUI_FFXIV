using System.Numerics;
using System.Linq;
using Dalamud.Game.ClientState.JobGauge.Types;

namespace SezzUI.Modules.JobHud.Jobs
{
    public sealed class RDM : BasePreset
    {
        public override uint JobId => DelvUI.Helpers.JobIDs.RDM;

        public override void Configure(JobHud hud)
        {
            Bar bar1 = new(hud);
            bar1.Add(new Icon(bar1) { TextureActionId = 7518, CooldownActionId = 7518, StatusId = 1238, MaxStatusDuration = 20, Level = 50 }); // Acceleration
            bar1.Add(new Icon(bar1) { TextureActionId = 7506, CooldownActionId = 7506, Level = 6 }); // Corps-a-corps
            bar1.Add(new Icon(bar1) { TextureActionId = 7515, CooldownActionId = 7515, Level = 40 }); // Displacement
            bar1.Add(new Icon(bar1) { TextureActionId = 7521, CooldownActionId = 7521, CustomPowerCondition = IsManaficationNotOvercapping, RequiresCombat = true, Level = 60 }); // Manafication
            bar1.Add(new Icon(bar1) { TextureActionId = 7520, CooldownActionId = 7520, StatusId = 1239, MaxStatusDuration = 20, StatusSourcePlayer = false, Level = 58 }); // Embolden
            hud.AddBar(bar1);

            // Dualcast
            hud.AddAlert(new AuraAlert
            {
                StatusId = 1249,
                Image = Plugin.AssemblyLocation + "Media\\Images\\Overlays\\genericarc_05_90.png",
                Size = new Vector2(256, 128) * 0.9f,
                Position = new Vector2(0, -180),
                Color = new Vector4(249f / 255f, 51f / 255f, 243f / 255f, 1f),
                MaxDuration = 15,
                TextOffset = new Vector2(0, -28),
            });

            // Verstone
            hud.AddAlert(new AuraAlert
            {
                StatusId = 1235,
                Image = Plugin.AssemblyLocation + "Media\\Images\\Overlays\\genericarc_01.png",
                Size = new Vector2(128, 256) * 0.8f,
                Position = new Vector2(-160, 0),
                Color = new Vector4(255f / 255f, 250f / 255f, 174f / 255f, 1f),
                MaxDuration = 30,
                Level = 30,
                TextOffset = new Vector2(-8, 0),
            });

            // Verfire
            hud.AddAlert(new AuraAlert
            {
                StatusId = 1234,
                Image = Plugin.AssemblyLocation + "Media\\Images\\Overlays\\genericarc_01.png",
                Size = new Vector2(128, 256) * 0.8f,
                Position = new Vector2(160, 0),
                Color = new Vector4(246f / 255f, 176f / 255f, 64f / 255f, 1f),
                MaxDuration = 30,
                Level = 26,
                FlipImageHorizontally = true,
                TextOffset = new Vector2(8, 0),
            });

            base.Configure(hud);

            Bar roleBar = hud.Bars.Last();
            roleBar.Add(new Icon(roleBar) { TextureActionId = 25857, CooldownActionId = 25857, StatusId = 2707, MaxStatusDuration = 10, StatusSourcePlayer = false, Level = 86 }, 1); // Magick Barrier
        }

        private static bool IsManaficationNotOvercapping()
        {
            RDMGauge gauge = Plugin.JobGauges.Get<RDMGauge>();
            return (gauge != null && gauge.BlackMana <= 50 && gauge.WhiteMana <= 50);
        }
    }
}
