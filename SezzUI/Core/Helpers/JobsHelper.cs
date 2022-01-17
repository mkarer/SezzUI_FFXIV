using System;
using Dalamud.Game.ClientState.JobGauge.Types;
using Dalamud.Game.ClientState.Objects.SubKinds;
using FFXIVClientStructs.FFXIV.Client.Game.UI;
using LuminaAction = Lumina.Excel.GeneratedSheets.Action;

namespace SezzUI.Helpers
{
    public static class JobsHelper
    {
        public enum PowerType
        {
            MP,
            Oath,
            WhiteMana,
            BlackMana,
            ManaStacks,
            Blood,
            Ammo,
            Heat,
            Battery,
            Chakra,
            Ninki,
            Soul,
            Shroud,
            Lemure,
            Addersgall,
            Addersting,
            Kenki,
            MeditationStacks,
            Sen,
            Beast,
            Lily,
            BloodLily,
            PolyglotStacks,
            FirstmindsFocus,
            EyeOfTheDragon
        }

        public static unsafe byte GetUnsyncedLevel()
        {
            if (Plugin.ClientState.LocalPlayer == null) { return 0; }

            UIState* uiState = UIState.Instance();
            if (uiState != null && uiState->PlayerState.SyncedLevel != 0 && Plugin.ClientState.LocalPlayer.ClassJob != null && Plugin.ClientState.LocalPlayer.ClassJob.GameData != null)
            {
                int index = Plugin.ClientState.LocalPlayer.ClassJob.GameData.ExpArrayIndex & 0xff;
                return (byte)uiState->PlayerState.ClassJobLevelArray[index];
            }

            return Plugin.ClientState.LocalPlayer.Level;
        }

        public static bool IsActionUnlocked(uint actionId)
        {
            LuminaAction? action = SpellHelper.Instance.GetAction(actionId);
            if (action != null)
            {
                byte jobLevel = action.IsRoleAction ? GetUnsyncedLevel() : Plugin.ClientState.LocalPlayer?.Level ?? 0;
                return (action.ClassJobLevel <= jobLevel);
            }

            return false;
        }

        public static (int, int) GetPower(PowerType ptype)
        {
            PlayerCharacter? player = Plugin.ClientState.LocalPlayer;
            byte jobLevel = player?.Level ?? 0;

            switch (ptype)
            {
                case PowerType.MP:
                    if (player != null)
                    {
                        return ((int)player.CurrentMp, (int)player.MaxMp);
                    }
                    return (0, 0);

                case PowerType.Oath:
                    if (jobLevel >= 35)
                    {
                        return (Plugin.JobGauges.Get<PLDGauge>()?.OathGauge ?? 0, 100);
                    }
                    return (0, 0);

                case PowerType.WhiteMana:
                    return (Plugin.JobGauges.Get<RDMGauge>()?.WhiteMana ?? 0, 100);

                case PowerType.BlackMana:
                    return (Plugin.JobGauges.Get<RDMGauge>()?.BlackMana ?? 0, 100);

                case PowerType.ManaStacks:
                    return (Plugin.JobGauges.Get<RDMGauge>()?.ManaStacks ?? 0, 3);

                case PowerType.Blood:
                    if (jobLevel >= 62)
                    {
                        return (Plugin.JobGauges.Get<DRKGauge>()?.Blood ?? 0, 100);
                    }
                    return (0, 0);

                case PowerType.Ammo:
                    if (jobLevel >= 30)
                    {
                        return (Plugin.JobGauges.Get<GNBGauge>()?.Ammo ?? 0, jobLevel >= 88 ? 3 : 2);
                    }
                    return (0, 0);

                case PowerType.Heat:
                    return (Plugin.JobGauges.Get<MCHGauge>()?.Heat ?? 0, 100);

                case PowerType.Battery:
                    return (Plugin.JobGauges.Get<MCHGauge>()?.Battery ?? 0, 100);

                case PowerType.Chakra:
                    return (Plugin.JobGauges.Get<MNKGauge>()?.Chakra ?? 0, 100);

                case PowerType.Ninki:
                    return (Plugin.JobGauges.Get<NINGauge>()?.Ninki ?? 0, 100);

                case PowerType.Soul:
                    return (Plugin.JobGauges.Get<RPRGauge>()?.Soul ?? 0, 100);

                case PowerType.Shroud:
                    return (Plugin.JobGauges.Get<RPRGauge>()?.Shroud ?? 0, 100);

                case PowerType.Lemure:
                    return (Plugin.JobGauges.Get<RPRGauge>()?.LemureShroud ?? 0, 5);

                case PowerType.Addersgall:
                    {
                        if (jobLevel >= 45)
                        {
                            SGEGauge gauge = Plugin.JobGauges.Get<SGEGauge>();
                            if (gauge != null)
                            {
                                const float addersgallCooldown = 20000f;
                                float GetScale(int num, float timer) => num + (timer / addersgallCooldown);
                                float adderScale = GetScale(gauge.Addersgall, gauge.AddersgallTimer);
                                return ((int)Math.Floor(adderScale), 3);
                            }
                        }
                        return (0, 0);
                    }

                case PowerType.Addersting:
                    if (jobLevel >= 66)
                    {
                        return (Plugin.JobGauges.Get<SGEGauge>()?.Addersting ?? 0, 3);

                    }
                    return (0, 0);

                case PowerType.Kenki:
                    return (Plugin.JobGauges.Get<SAMGauge>()?.Kenki ?? 0, 100);

                case PowerType.MeditationStacks:
                    return (Plugin.JobGauges.Get<SAMGauge>()?.MeditationStacks ?? 0, 100);

                case PowerType.Sen:
                    {
                        SAMGauge gauge = Plugin.JobGauges.Get<SAMGauge>();
                        if (gauge != null)
                        {
                            return (0 + (gauge.HasSetsu ? 1 : 0) + (gauge.HasGetsu ? 1 : 0) + (gauge.HasKa ? 1 : 0), 3);
                        }
                        return (0, 3);
                    }

                case PowerType.Beast:
                    if (jobLevel >= 35)
                    {
                        return (Plugin.JobGauges.Get<WARGauge>()?.BeastGauge ?? 0, 100);
                    }
                    return (0, 0);

                case PowerType.Lily:
                    {
                        WHMGauge gauge = Plugin.JobGauges.Get<WHMGauge>();
                        if (gauge != null)
                        {
                            const float lilyCooldown = 30000f;
                            float GetScale(int num, float timer) => num + (timer / lilyCooldown);
                            float lilyScale = GetScale(gauge.Lily, gauge.LilyTimer);
                            return ((int)Math.Floor(lilyScale), 3);
                        }
                        return (0, 0);
                    }

                case PowerType.BloodLily:
                    return (Plugin.JobGauges.Get<WHMGauge>()?.BloodLily ?? 0, 3);

                case PowerType.PolyglotStacks:
                    {
                        if (jobLevel >= 70)
                        {
                            BLMGauge gauge = Plugin.JobGauges.Get<BLMGauge>();
                            if (gauge != null)
                            {
                                return (gauge.PolyglotStacks, jobLevel >= 80 ? 2 : 1);
                            }
                        }
                        return (0, 0);
                    }

                case PowerType.FirstmindsFocus:
                    if (jobLevel >= 90)
                    {
                        DRGGauge gauge = Plugin.JobGauges.Get<DRGGauge>();
                        if (gauge != null)
                        {
                            return (gauge.FirstmindsFocusCount, 2);
                        }
                    }
                    return (0, 0);

                case PowerType.EyeOfTheDragon:
                    if (jobLevel >= 60)
                    {
                        DRGGauge gauge = Plugin.JobGauges.Get<DRGGauge>();
                        if (gauge != null)
                        {
                            return (gauge.EyeCount, 2);
                        }
                    }
                    return (0, 0);
            }

            return (0, 0);
        }
    }
}
