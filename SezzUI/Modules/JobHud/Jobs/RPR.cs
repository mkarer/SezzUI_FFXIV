﻿using System.Numerics;

namespace SezzUI.Modules.JobHud.Jobs
{
    public sealed class RPR : BasePreset
    {
        public override uint JobId => 39;
        
        public override void Configure(JobHud hud)
        {
            using (Bar bar = new())
            {
                bar.Add(new Icon(bar) { TextureActionId = 24393, CooldownActionId = 24393, Level = 76 }); // Gluttony
                bar.Add(new Icon(bar) { TextureActionId = 24405, CooldownActionId = 24405, StatusId = 2599, MaxStatusDuration = 20, StatusTarget = Enums.Unit.Player, Level = 72 }); // Arcane Circle
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
                Position = new Vector2(-140, 50)
            });
        }
    }
}
