using System.Numerics;
using Dalamud.Game.ClientState.JobGauge.Types;

namespace SezzUI.Modules.JobHud.Jobs
{
    public sealed class SMN : BasePreset
    {
        public override uint JobId => DelvUI.Helpers.JobIDs.SMN;
        
        public override void Configure(JobHud hud)
        {
            using (Bar bar = new(hud))
            {
                bar.Add(new Icon(bar) { TextureActionId = 3581, CooldownActionId = 3581, CustomDuration = GetDemiBahamutDuration, RequiresCombat = true, RequiresPet = true, Level = 58 }); // Dreadwyrm Trance
                bar.Add(new Icon(bar) { TextureActionId = 7429, CooldownActionId = 7429, CustomCondition = IsDemiBahamutSummoned, Level = 70 }); // Enkindle Bahamut
                bar.Add(new Icon(bar) { TextureActionId = 25801, CooldownActionId = 25801, StatusId = 2703, MaxStatusDuration = 30, StatusTarget = Enums.Unit.Player, RequiresCombat = true, CustomCondition = IsCarbuncleSummoned, StatusSourcePlayer = false, Level = 66 }); // Searing Light
                hud.AddBar(bar);
            }

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
        }

        // Pet IDs:
        // https://github.com/xivapi/ffxiv-datamining/blob/50f42f2ff396c7857ac2636e09ffbb4265a25dae/csv/Pet.csv
        // Carbuncle: 23
        // Demi-Bahamut: 10
        // Ifrit-Egi: 27
        // Titan-Egi: 28
        // Garuda-Egi: 29

        public static bool IsCarbuncleSummoned()
        {
            return (Plugin.BuddyList.PetBuddy != null && Plugin.BuddyList.PetBuddy.PetData.Id == 23);
        }

        public static bool IsDemiBahamutSummoned()
        {
            return (Plugin.BuddyList.PetBuddy != null && Plugin.BuddyList.PetBuddy.PetData.Id == 10);
        }

        public static (float, float) GetDemiBahamutDuration()
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
