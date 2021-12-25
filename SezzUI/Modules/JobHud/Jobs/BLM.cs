using System.Linq;
using System.Numerics;
using Dalamud.Game.ClientState.JobGauge.Types;

namespace SezzUI.Modules.JobHud.Jobs
{
    public sealed class BLM : BasePreset
    {
        public override uint JobId => DelvUI.Helpers.JobIDs.BLM;
        
        public override void Configure(JobHud hud)
        {
            using (Bar bar = new(hud))
            {
                bar.Add(new Icon(bar) { TextureActionId = 144, StatusIds = new[] { (uint)161, (uint)162, (uint)163, (uint)1210 }, MaxStatusDurations = new[] { 21f, 18f, 30f, 18f }, StatusTarget = Enums.Unit.Target, GlowBorderStatusId = 164, Features = IconFeatures.GlowIgnoresState, Level = 4 }); // Thunder
                bar.Add(new Icon(bar) { TextureActionId = 25796, CooldownActionId = 25796, CustomPowerCondition = IsInAstralFireOrIsInUmbralIce, GlowBorderUsable = true, Level = 86 }); // Amplifier
                bar.Add(new Icon(bar) { TextureActionId = 3574, CooldownActionId = 3574, StatusId = 867, MaxStatusDuration = 30, StatusTarget = Enums.Unit.Player, Level = 54 }); // Sharpcast
                bar.Add(new Icon(bar) { TextureActionId = 3573, CooldownActionId = 3573, StatusId = 737, MaxStatusDuration = 30, StatusTarget = Enums.Unit.Player, GlowBorderStatusId = 738, GlowBorderInvertCheck = true, GlowBorderStatusIdForced = 737, GlowBorderUsable = true, Level = 52 }); // Ley Lines
                hud.AddBar(bar);
            }

            // Firestarter
            hud.AddAlert(new AuraAlert
            {
                StatusId = 165,
                Image = Plugin.AssemblyLocation + "Media\\Images\\Overlays\\impact.png",
                Size = new Vector2(256, 128) * 0.8f,
                Position = new Vector2(0, -180),
                MaxDuration = 30,
                Level = 42
            });

            // Polyglots: 1
            hud.AddAlert(new AuraAlert
            {
                PowerType = Helpers.JobsHelper.PowerType.PolyglotStacks,
                ExactPowerAmount = 1,
                Image = Plugin.AssemblyLocation + "Media\\Images\\Overlays\\arcane_missiles_1.png",
                Size = new Vector2(128, 256),
                Position = new Vector2(-170, 50),
                Level = 70
            });

            // Polyglots: 2
            hud.AddAlert(new AuraAlert
            {
                PowerType = Helpers.JobsHelper.PowerType.PolyglotStacks,
                ExactPowerAmount = 2,
                Image = Plugin.AssemblyLocation + "Media\\Images\\Overlays\\arcane_missiles_2.png",
                Size = new Vector2(128, 256),
                Position = new Vector2(-190, 50),
                Level = 80
            });

            // Paradox
            hud.AddAlert(new AuraAlert
            {
                CustomCondition = IsParadoxActive,
                Image = Plugin.AssemblyLocation + "Media\\Images\\Overlays\\echo_of_the_elements.png",
                Size = new Vector2(128, 256),
                Position = new Vector2(190, 50),
                FlipImageHorizontally = true,
                Level = 90
            });

            base.Configure(hud);

            Bar roleBar = hud.Bars.Last();
            roleBar.Add(new Icon(roleBar) { TextureActionId = 157, CooldownActionId = 157, StatusId = 168, MaxStatusDuration = 20, StatusTarget = Enums.Unit.Player, Level = 30 }, 1); // Manaward
            roleBar.Add(new Icon(roleBar) { TextureActionId = 7421, CooldownActionId = 7421, StatusId = 1211, MaxStatusDuration = 15, StatusTarget = Enums.Unit.Player, Level = 66 }, 1); // Triplecast
        }

        public static bool IsInAstralFireOrIsInUmbralIce()
        {
            BLMGauge gauge = Plugin.JobGauges.Get<BLMGauge>();
            return (gauge != null && (gauge.InAstralFire || gauge.InUmbralIce));
        }

        public static bool IsParadoxActive()
        {
            BLMGauge gauge = Plugin.JobGauges.Get<BLMGauge>();
            return (gauge != null && gauge.IsParadoxActive);
        }
    }
}
