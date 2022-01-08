using System.Linq;
using System.Numerics;
using Dalamud.Game.ClientState.JobGauge.Types;
using Dalamud.Game.ClientState.JobGauge.Enums;

namespace SezzUI.Modules.JobHud.Jobs
{
    public sealed class BRD : BasePreset
    {
        public override uint JobId => DelvUI.Helpers.JobIDs.BRD;
        
        public override void Configure(JobHud hud)
        {
            using (Bar bar = new(hud))
            {
                bar.Add(new Icon(bar) { TextureActionId = 100, StatusIds = new[] { (uint)124, (uint)1200 }, MaxStatusDuration = 45, StatusTarget = Enums.Unit.Target, Level = 6 }); // Venomous Bite
                bar.Add(new Icon(bar) { TextureActionId = 113, StatusIds = new[] { (uint)129, (uint)1201 }, MaxStatusDuration = 45, StatusTarget = Enums.Unit.Target, Level = 30 }); // Windbite
                bar.Add(new Icon(bar) { TextureActionId = 107, CooldownActionId = 107, StatusId = 128, MaxStatusDuration = 10, StatusTarget = Enums.Unit.Player, Level = 38 }); // Barrage
                bar.Add(new Icon(bar) { TextureActionId = 101, CooldownActionId = 101, StatusId = 125, MaxStatusDuration = 20, StatusTarget = Enums.Unit.Player, Level = 4 }); // Raging Strikes
                hud.AddBar(bar);
            }

            using (Bar bar = new(hud))
            {
                bar.Add(new Icon(bar) { TextureActionId = 3559, CooldownActionId = 3559, CustomDuration = GetWanderersMinuetDuration, Level = 52 }); // The Wanderer's Minuet
                bar.Add(new Icon(bar) { TextureActionId = 114, CooldownActionId = 114, CustomDuration = GetMagesBalladDuration, Level = 30 }); // Mage's Ballad
                bar.Add(new Icon(bar) { TextureActionId = 116, CooldownActionId = 116, CustomDuration = GetArmysPaeonDuration, Level = 40 }); // Army's Paeon
                bar.Add(new Icon(bar) { TextureActionId = 118, CooldownActionId = 118, StatusId = 141, MaxStatusDuration = 15, StatusTarget = Enums.Unit.Player, StatusSourcePlayer = false, CustomPowerCondition = IsPlaying, Level = 50 }); // Battle Voice
                hud.AddBar(bar);
            }

            // Straight Shot
            hud.AddAlert(new AuraAlert
            {
                StatusId = 122,
                Image = Plugin.AssemblyLocation + "Media\\Images\\Overlays\\focus_fire.png",
                Size = new Vector2(256, 128) * 0.8f,
                Position = new Vector2(0, -180),
                MaxDuration = 30,
                Level = 2,
                TextOffset = new Vector2(0, -15),
            });

            base.Configure(hud);
     
            Bar roleBar = hud.Bars.Last();
            roleBar.Add(new Icon(roleBar) { TextureActionId = 3561, CooldownActionId = 3561, StatusId = 866, MaxStatusDuration = 30, StatusTarget = Enums.Unit.TargetOrPlayer, StatusSourcePlayer = false, Level = 35 }, 1); // The Warden's Paean
            roleBar.Add(new Icon(roleBar) { TextureActionId = 7405, CooldownActionId = 7405, StatusId = 1934, MaxStatusDuration = 15, StatusTarget = Enums.Unit.Player, StatusSourcePlayer = false, Level = 62 }, 2); // Troubadour
            roleBar.Add(new Icon(roleBar) { TextureActionId = 7408, CooldownActionId = 7408, StatusId = 1202, MaxStatusDuration = 15, StatusTarget = Enums.Unit.Player, StatusSourcePlayer = false, Level = 66 }, 1); // Nature's Minne
        }

        private static bool IsPlaying()
        {
            BRDGauge gauge = Plugin.JobGauges.Get<BRDGauge>();
            return (gauge != null && (gauge.Song != Song.NONE));
        }

        private static (float, float) GetSongDuration(Song song)
        {
            BRDGauge gauge = Plugin.JobGauges.Get<BRDGauge>();
            if (gauge != null && gauge.Song == song)
            {
                return (gauge.SongTimer / 1000f, 45f);
            }
            else
            {
                return (0, 0);
            }
        }

        private static (float, float) GetWanderersMinuetDuration()
        {
            return GetSongDuration(Song.WANDERER);
        }

        private static (float, float) GetMagesBalladDuration()
        {
            return GetSongDuration(Song.MAGE);
        }

        private static (float, float) GetArmysPaeonDuration()
        {
            return GetSongDuration(Song.ARMY);
        }

    }
}
