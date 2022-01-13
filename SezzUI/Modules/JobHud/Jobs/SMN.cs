using System.Numerics;
using System.Linq;
using Dalamud.Game.ClientState.JobGauge.Types;

namespace SezzUI.Modules.JobHud.Jobs
{
    public sealed class SMN : BasePreset
    {
        public override uint JobId => DelvUI.Helpers.JobIDs.SMN;

        public override void Configure(JobHud hud)
        {
            Bar bar1 = new(hud);
            bar1.Add(new Icon(bar1) { TextureActionId = 3581, CooldownActionId = 3581, CustomDuration = GetDemiBahamutDuration, RequiresCombat = true, RequiresPet = true }); // Dreadwyrm Trance
            bar1.Add(new Icon(bar1) { TextureActionId = 7429, CooldownActionId = 7429, CustomCondition = IsDemiBahamutSummoned }); // Enkindle Bahamut
            bar1.Add(new Icon(bar1) { TextureActionId = 25801, CooldownActionId = 25801, StatusId = 2703, MaxStatusDuration = 30, StatusTarget = Enums.Unit.Player, RequiresCombat = true, CustomCondition = IsCarbuncleSummoned, StatusSourcePlayer = false }); // Searing Light
            hud.AddBar(bar1);

            // Further Rain
            hud.AddAlert(new AuraAlert
            {
                StatusId = 2701,
                MaxDuration = 60,
                Image = Plugin.AssemblyLocation + "Media\\Images\\Overlays\\demonic_core_vertical.png",
                Size = new Vector2(128, 256),
                Position = new Vector2(200, 50),
                Level = 62,
                FlipImageHorizontally = true
            });

            base.Configure(hud);

            Bar roleBar = hud.Bars.Last();
            roleBar.Add(new Icon(roleBar) { TextureActionId = 25799, CooldownActionId = 25799, StatusId = 2702, MaxStatusDuration = 30, StatusTarget = Enums.Unit.Player, CustomCondition = IsCarbuncleSummoned, StatusSourcePlayer = false }, 1); // Radiant Aegis
        }

        // Pet IDs:
        // https://github.com/xivapi/ffxiv-datamining/blob/50f42f2ff396c7857ac2636e09ffbb4265a25dae/csv/Pet.csv
        // Carbuncle: 23
        // Demi-Bahamut: 10
        // Ifrit-Egi: 27
        // Titan-Egi: 28
        // Garuda-Egi: 29

        private static bool IsCarbuncleSummoned()
        {
            return (Plugin.BuddyList.PetBuddy != null && Plugin.BuddyList.PetBuddy.PetData.Id == 23);
        }

        private static bool IsDemiBahamutSummoned()
        {
            return (Plugin.BuddyList.PetBuddy != null && Plugin.BuddyList.PetBuddy.PetData.Id == 10);
        }

        private static (float, float) GetDemiBahamutDuration()
        {
            if (IsDemiBahamutSummoned())
            {
                SMNGauge gauge = Plugin.JobGauges.Get<SMNGauge>();
                if (gauge != null)
                {
                    return (gauge.SummonTimerRemaining / 1000f, 15);
                }
            }

            return (0, 0);
        }
    }
}
