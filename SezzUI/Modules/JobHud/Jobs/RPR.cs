using System.Numerics;
using System.Linq;
using Dalamud.Game.ClientState.Statuses;
using Dalamud.Game.ClientState.JobGauge.Types;

namespace SezzUI.Modules.JobHud.Jobs
{
    public sealed class RPR : BasePreset
    {
        public override uint JobId => DelvUI.Helpers.JobIDs.RPR;
        
        public override void Configure(JobHud hud)
        {
            using (Bar bar = new(hud))
            {
                bar.Add(new Icon(bar) { TextureStatusId = 2586, StatusId = 2586, MaxStatusDuration = 60, StatusTarget = Enums.Unit.Target, Level = 10 }); // Death's Design
                bar.Add(new Icon(bar) { TextureActionId = 24394, StatusIds = new[] { (uint)2593, (uint)2863 }, MaxStatusDuration = 30, StatusTarget = Enums.Unit.Player, RequiredPowerAmount = 50, RequiredPowerType = Helpers.JobsHelper.PowerType.Shroud, GlowBorderUsable = true, Level = 80 }); // Enshroud
                bar.Add(new Icon(bar) { TextureActionId = 24398, CustomCondition = IsEnshrouded, CustomPowerCondition = HasOneLemureLeft, GlowBorderUsable = true, Level = 90 }); // Communio
                bar.Add(new Icon(bar) { TextureActionId = 24393, CooldownActionId = 24393, RequiredPowerAmount = 50, RequiredPowerType = Helpers.JobsHelper.PowerType.Soul, GlowBorderUsable = true, Level = 76 }); // Gluttony
                bar.Add(new Icon(bar) { TextureActionId = 24405, CooldownActionId = 24405, StatusId = 2599, MaxStatusDuration = 20, StatusTarget = Enums.Unit.Player, StacksStatusId = 2592, Level = 72 }); // Arcane Circle
                hud.AddBar(bar);
            }

            // Soulsow/Harvest Moon
            hud.AddAlert(new AuraAlert
            {
                StatusId = 2594,
                InvertCheck = true,
                EnableInCombat = false,
                EnableOutOfCombat = true,
                TreatWeaponOutAsCombat = false,
                Image = Plugin.AssemblyLocation + "Media\\Images\\Overlays\\surge_of_darkness.png",
                Size = new Vector2(128, 256),
                Position = new Vector2(-140, 50),
                Level = 82
            });

            // Immortal Sacrifice (Plentiful Harvest)
            hud.AddAlert(new AuraAlert
            {
                StatusId = 2592,
                MaxDuration = 30,
                CustomCondition = HasNoBloodsownCircle,
                Image = Plugin.AssemblyLocation + "Media\\Images\\Overlays\\predatory_swiftness.png",
                Size = new Vector2(256, 128) * 1.1f,
                Position = new Vector2(0, -180),
                Level = 88,
            });

            // Enhanced Gallows
            hud.AddAlert(new AuraAlert
            {
                CustomCondition = ShouldUseGallows,
                Image = Plugin.AssemblyLocation + "Media\\Images\\Overlays\\genericarc_05_90.png",
                Size = new Vector2(256, 128) * 0.5f,
                Position = new Vector2(0, -80),
                Color = new Vector4(0 / 255f, 221f / 255f, 210f / 255f, 1f),
                Level = 70
            });

            // Enhanced Gibbet
            hud.AddAlert(new AuraAlert
            {
                CustomCondition = ShouldUseGibbet,
                Image = Plugin.AssemblyLocation + "Media\\Images\\Overlays\\genericarc_05.png",
                Size = new Vector2(128, 256) * 0.5f,
                Position = new Vector2(-120, 20),
                Color = new Vector4(0 / 255f, 221f / 255f, 210f / 255f, 1f),
                Level = 70
            });

            // Enhanced Gibbet
            hud.AddAlert(new AuraAlert
            {
                CustomCondition = ShouldUseGibbet,
                Image = Plugin.AssemblyLocation + "Media\\Images\\Overlays\\genericarc_05.png",
                Size = new Vector2(128, 256) * 0.5f,
                Position = new Vector2(120, 20),
                Color = new Vector4(0 / 255f, 221f / 255f, 210f / 255f, 1f),
                Level = 70,
                FlipImageHorizontally = true
            });

            base.Configure(hud);

            Bar roleBar = hud.Bars.Last();
            roleBar.Add(new Icon(roleBar) { TextureActionId = 24404, CooldownActionId = 24404, StatusIds = new[] { (uint)2596, (uint)2597 }, MaxStatusDuration = 5, StatusTarget = Enums.Unit.Player, Level = 40 }, 1); // Arcane Crest
        }

        private static bool ShouldUseGibbet()
        {
            Status? statusSoulReaver = Helpers.SpellHelper.GetStatus(2587, Enums.Unit.Player);
            if (statusSoulReaver == null) { return false; }

            Status? statusEnhancedGallows = Helpers.SpellHelper.GetStatus(2589, Enums.Unit.Player);
            Status? statusEnhancedGibbet = Helpers.SpellHelper.GetStatus(2588, Enums.Unit.Player);

            return (statusEnhancedGibbet != null || statusEnhancedGallows == null);
        }

        private static bool ShouldUseGallows()
        {
            Status? statusSoulReaver = Helpers.SpellHelper.GetStatus(2587, Enums.Unit.Player);
            if (statusSoulReaver == null) { return false; }

            Status? statusEnhancedGallows = Helpers.SpellHelper.GetStatus(2589, Enums.Unit.Player); // 2856
            Status? statusEnhancedGibbet = Helpers.SpellHelper.GetStatus(2588, Enums.Unit.Player);

            return (statusEnhancedGallows != null || statusEnhancedGibbet == null);
        }

        private static bool IsEnshrouded()
        {
            RPRGauge gauge = Plugin.JobGauges.Get<RPRGauge>();
            return (gauge != null && gauge.EnshroudedTimeRemaining > 0);
        }

        private static bool HasOneLemureLeft()
        {
            RPRGauge gauge = Plugin.JobGauges.Get<RPRGauge>();
            return (gauge != null && gauge.EnshroudedTimeRemaining > 0 && gauge.LemureShroud == 1 && gauge.VoidShroud == 0);
        }

        private static bool HasNoBloodsownCircle()
        {
            return Helpers.SpellHelper.GetStatus(2972, Enums.Unit.Player) == null;
        }
    }
}
