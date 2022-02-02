using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Numerics;
using System.Runtime.InteropServices;
using System.Text.RegularExpressions;
using Windows.Win32;
using Colourful;
using Dalamud.Game.ClientState.Objects;
using Dalamud.Game.ClientState.Objects.Enums;
using Dalamud.Game.ClientState.Objects.SubKinds;
using Dalamud.Game.ClientState.Objects.Types;
using Dalamud.Game.ClientState.Statuses;
using SezzUI.Config;
using SezzUI.Enums;
using SezzUI.Interface.GeneralElements;

namespace SezzUI.Helpers
{
	public sealed class NaturalStringComparer : IComparer<string>
	{
		public int Compare(string? x, string? y) => PInvoke.StrCmpLogical(x ?? "", y ?? "");
	}

	internal static class Utils
	{
		internal static PluginLogger Logger;

		public static NaturalStringComparer NaturalStringComparer = new();

		static Utils()
		{
			Logger = new("Utils");
		}

		public static unsafe bool IsHostileMemory(BattleNpc? npc) => npc != null && (npc.BattleNpcKind == BattleNpcSubKind.Enemy || (int) npc.BattleNpcKind == 1) && *(byte*) (npc.Address + 0x19C3) != 0;

		//Build our converter objects and store them in a field. This will be used to convert our PluginConfigColors into different color spaces to be used for interpolation
		private static readonly IColorConverter<RGBColor, LabColor> _rgbToLab = new ConverterBuilder().FromRGB().ToLab().Build();
		private static readonly IColorConverter<LabColor, RGBColor> _labToRgb = new ConverterBuilder().FromLab().ToRGB().Build();

		private static readonly IColorConverter<RGBColor, LChabColor> _rgbToLChab = new ConverterBuilder().FromRGB().ToLChab().Build();
		private static readonly IColorConverter<LChabColor, RGBColor> _lchabToRgb = new ConverterBuilder().FromLChab().ToRGB().Build();

		private static readonly IColorConverter<RGBColor, XYZColor> _rgbToXyz = new ConverterBuilder().FromRGB(RGBWorkingSpaces.sRGB).ToXYZ(Illuminants.D65).Build();
		private static readonly IColorConverter<XYZColor, RGBColor> _xyzToRgb = new ConverterBuilder().FromXYZ(Illuminants.D65).ToRGB(RGBWorkingSpaces.sRGB).Build();

		private static readonly IColorConverter<RGBColor, LChuvColor> _rgbToLChuv = new ConverterBuilder().FromRGB().ToLChuv().Build();
		private static readonly IColorConverter<LChuvColor, RGBColor> _lchuvToRgb = new ConverterBuilder().FromLChuv().ToRGB().Build();

		private static readonly IColorConverter<RGBColor, LuvColor> _rgbToLuv = new ConverterBuilder().FromRGB().ToLuv().Build();
		private static readonly IColorConverter<LuvColor, RGBColor> _luvToRgb = new ConverterBuilder().FromLuv().ToRGB().Build();

		private static readonly IColorConverter<RGBColor, JzazbzColor> _rgbToJzazbz = new ConverterBuilder().FromRGB().ToJzazbz().Build();
		private static readonly IColorConverter<JzazbzColor, RGBColor> _jzazbzToRgb = new ConverterBuilder().FromJzazbz().ToRGB().Build();

		private static readonly IColorConverter<RGBColor, JzCzhzColor> _rgbToJzCzhz = new ConverterBuilder().FromRGB().ToJzCzhz().Build();
		private static readonly IColorConverter<JzCzhzColor, RGBColor> _jzCzhzToRgb = new ConverterBuilder().FromJzCzhz().ToRGB().Build();

		//Simple LinearInterpolation method. T = [0 , 1]
		private static float LinearInterpolation(float left, float right, float t) => left + (right - left) * t;

		public static PluginConfigColor GetColorByScale(float i, ColorByHealthValueConfig config) =>
			GetColorByScale(i, config.LowHealthColorThreshold / 100f, config.FullHealthColorThreshold / 100f, config.LowHealthColor, config.FullHealthColor, config.MaxHealthColor, config.UseMaxHealthColor, config.BlendMode);

		//Method used to interpolate two PluginConfigColors
		//i is scale [0 , 1]
		//min and max are used for color thresholds. for instance return colorLeft if i < min or return ColorRight if i > max
		public static PluginConfigColor GetColorByScale(float i, float min, float max, PluginConfigColor colorLeft, PluginConfigColor colorRight, PluginConfigColor colorMax, bool useMaxColor, BlendMode blendMode)
		{
			//Set our thresholds where the ratio is the range of values we will use for interpolation. 
			//Values outside this range will either return colorLeft or colorRight
			float ratio = i;
			if (min > 0 || max < 1)
			{
				if (i < min)
				{
					ratio = 0;
				}
				else if (i > max)
				{
					ratio = 1;
				}
				else
				{
					float range = max - min;
					ratio = (i - min) / range;
				}
			}

			//Convert our PluginConfigColor to RGBColor
			RGBColor rgbColorLeft = new(colorLeft.Vector.X, colorLeft.Vector.Y, colorLeft.Vector.Z);
			RGBColor rgbColorRight = new(colorRight.Vector.X, colorRight.Vector.Y, colorRight.Vector.Z);

			//Interpolate our Alpha now
			float alpha = LinearInterpolation(colorLeft.Vector.W, colorRight.Vector.W, ratio);

			if (ratio >= 1 && useMaxColor)
			{
				return new(new(colorMax.Vector.X, colorMax.Vector.Y, colorMax.Vector.Z, colorMax.Vector.W));
			}

			//Allow the users to select different blend modes since interpolating between two colors can result in different blending depending on the color space
			//We convert our RGBColor values into different color spaces. We then interpolate each channel before converting the color back into RGBColor space
			switch (blendMode)
			{
				case BlendMode.LAB:
				{
					//convert RGB to LAB
					LabColor LabLeft = _rgbToLab.Convert(rgbColorLeft);
					LabColor LabRight = _rgbToLab.Convert(rgbColorRight);

					RGBColor Lab2RGB = _labToRgb.Convert(new(LinearInterpolation((float) LabLeft.L, (float) LabRight.L, ratio), LinearInterpolation((float) LabLeft.a, (float) LabRight.a, ratio), LinearInterpolation((float) LabLeft.b, (float) LabRight.b, ratio)));

					Lab2RGB.NormalizeIntensity();
					return new(new((float) Lab2RGB.R, (float) Lab2RGB.G, (float) Lab2RGB.B, alpha));
				}

				case BlendMode.LChab:
				{
					//convert RGB to LChab
					LChabColor LChabLeft = _rgbToLChab.Convert(rgbColorLeft);
					LChabColor LChabRight = _rgbToLChab.Convert(rgbColorRight);

					RGBColor LChab2RGB = _lchabToRgb.Convert(new(LinearInterpolation((float) LChabLeft.L, (float) LChabRight.L, ratio), LinearInterpolation((float) LChabLeft.C, (float) LChabRight.C, ratio), LinearInterpolation((float) LChabLeft.h, (float) LChabRight.h, ratio)));

					LChab2RGB.NormalizeIntensity();

					return new(new((float) LChab2RGB.R, (float) LChab2RGB.G, (float) LChab2RGB.B, alpha));
				}
				case BlendMode.XYZ:
				{
					//convert RGB to XYZ
					XYZColor XYZLeft = _rgbToXyz.Convert(rgbColorLeft);
					XYZColor XYZRight = _rgbToXyz.Convert(rgbColorRight);

					RGBColor XYZ2RGB = _xyzToRgb.Convert(new(LinearInterpolation((float) XYZLeft.X, (float) XYZRight.X, ratio), LinearInterpolation((float) XYZLeft.Y, (float) XYZRight.Y, ratio), LinearInterpolation((float) XYZLeft.Z, (float) XYZRight.Z, ratio)));

					XYZ2RGB.NormalizeIntensity();

					return new(new((float) XYZ2RGB.R, (float) XYZ2RGB.G, (float) XYZ2RGB.B, alpha));
				}
				case BlendMode.RGB:
				{
					//No conversion needed here because we are already working in RGB space
					RGBColor newRGB = new(LinearInterpolation((float) rgbColorLeft.R, (float) rgbColorRight.R, ratio), LinearInterpolation((float) rgbColorLeft.G, (float) rgbColorRight.G, ratio), LinearInterpolation((float) rgbColorLeft.B, (float) rgbColorRight.B, ratio));

					return new(new((float) newRGB.R, (float) newRGB.G, (float) newRGB.B, alpha));
				}
				case BlendMode.LChuv:
				{
					//convert RGB to LChuv
					LChuvColor LChuvLeft = _rgbToLChuv.Convert(rgbColorLeft);
					LChuvColor LChuvRight = _rgbToLChuv.Convert(rgbColorRight);

					RGBColor LChuv2RGB = _lchuvToRgb.Convert(new(LinearInterpolation((float) LChuvLeft.L, (float) LChuvRight.L, ratio), LinearInterpolation((float) LChuvLeft.C, (float) LChuvRight.C, ratio), LinearInterpolation((float) LChuvLeft.h, (float) LChuvRight.h, ratio)));

					LChuv2RGB.NormalizeIntensity();

					return new(new((float) LChuv2RGB.R, (float) LChuv2RGB.G, (float) LChuv2RGB.B, alpha));
				}

				case BlendMode.Luv:
				{
					//convert RGB to Luv
					LuvColor LuvLeft = _rgbToLuv.Convert(rgbColorLeft);
					LuvColor LuvRight = _rgbToLuv.Convert(rgbColorRight);

					RGBColor Luv2RGB = _luvToRgb.Convert(new(LinearInterpolation((float) LuvLeft.L, (float) LuvRight.L, ratio), LinearInterpolation((float) LuvLeft.u, (float) LuvRight.u, ratio), LinearInterpolation((float) LuvLeft.v, (float) LuvRight.v, ratio)));

					Luv2RGB.NormalizeIntensity();

					return new(new((float) Luv2RGB.R, (float) Luv2RGB.G, (float) Luv2RGB.B, alpha));
				}
				case BlendMode.Jzazbz:
				{
					//convert RGB to Jzazbz
					JzazbzColor JzazbzLeft = _rgbToJzazbz.Convert(rgbColorLeft);
					JzazbzColor JzazbzRight = _rgbToJzazbz.Convert(rgbColorRight);

					RGBColor Jzazbz2RGB = _jzazbzToRgb.Convert(new(LinearInterpolation((float) JzazbzLeft.Jz, (float) JzazbzRight.Jz, ratio), LinearInterpolation((float) JzazbzLeft.az, (float) JzazbzRight.az, ratio), LinearInterpolation((float) JzazbzLeft.bz, (float) JzazbzRight.bz, ratio)));

					Jzazbz2RGB.NormalizeIntensity();

					return new(new((float) Jzazbz2RGB.R, (float) Jzazbz2RGB.G, (float) Jzazbz2RGB.B, alpha));
				}
				case BlendMode.JzCzhz:
				{
					//convert RGB to JzCzhz
					JzCzhzColor JzCzhzLeft = _rgbToJzCzhz.Convert(rgbColorLeft);
					JzCzhzColor JzCzhzRight = _rgbToJzCzhz.Convert(rgbColorRight);

					RGBColor JzCzhz2RGB = _jzCzhzToRgb.Convert(new(LinearInterpolation((float) JzCzhzLeft.Jz, (float) JzCzhzRight.Jz, ratio), LinearInterpolation((float) JzCzhzLeft.Cz, (float) JzCzhzRight.Cz, ratio), LinearInterpolation((float) JzCzhzLeft.hz, (float) JzCzhzRight.hz, ratio)));

					JzCzhz2RGB.NormalizeIntensity();

					return new(new((float) JzCzhz2RGB.R, (float) JzCzhz2RGB.G, (float) JzCzhz2RGB.B, alpha));
				}
			}

			return new(Vector4.One);
		}

		public static PluginConfigColor ColorForActor(GameObject? actor)
		{
			if (actor == null || actor is not Character character)
			{
				return GlobalColors.Instance.NPCNeutralColor;
			}

			if (character.ObjectKind == ObjectKind.Player)
			{
				return GlobalColors.Instance.SafeColorForJobId(character.ClassJob.Id);
			}

			return character switch
			{
				BattleNpc {SubKind: 9} battleNpc when battleNpc.ClassJob.Id > 0 => GlobalColors.Instance.SafeColorForJobId(character.ClassJob.Id), // Trust/Squadron NPCs
				BattleNpc battleNpc when battleNpc.BattleNpcKind is BattleNpcSubKind.Chocobo or BattleNpcSubKind.Pet || !IsHostileMemory(battleNpc) => GlobalColors.Instance.NPCFriendlyColor,
				BattleNpc battleNpc when battleNpc.BattleNpcKind == BattleNpcSubKind.Enemy || (battleNpc.StatusFlags & StatusFlags.InCombat) == StatusFlags.InCombat => GlobalColors.Instance.NPCHostileColor, // I still don't think we should be defaulting to "in combat = hostile", but whatever
				_ => GlobalColors.Instance.NPCNeutralColor
			};
		}

		public static Status? GetTankInvulnerabilityID(BattleChara actor)
		{
			Status? tankInvulnBuff = actor.StatusList.FirstOrDefault(o => o.StatusId is 810 or 811 or 1302 or 409 or 1836 or 82);

			return tankInvulnBuff;
		}

		public static bool IsOnCleanseJob()
		{
			PlayerCharacter? player = Plugin.ClientState.LocalPlayer;

			return player != null && JobsHelper.IsJobWithCleanse(player.ClassJob.Id, player.Level);
		}

		public static GameObject? FindTargetOfTarget(GameObject? target, GameObject? player, ObjectTable actors)
		{
			if (target == null)
			{
				return null;
			}

			if (target.TargetObjectId == 0 && player != null && player.TargetObjectId == 0)
			{
				return player;
			}

			// only the first 200 elements in the array are relevant due to the order in which SE packs data into the array
			// we do a step of 2 because its always an actor followed by its companion
			for (int i = 0; i < 200; i += 2)
			{
				GameObject? actor = actors[i];
				if (actor?.ObjectId == target.TargetObjectId)
				{
					return actor;
				}
			}

			return null;
		}

		public static string UserFriendlyConfigName(string configTypeName) => UserFriendlyString(configTypeName, "Config");

		public static string UserFriendlyString(string str, string? remove)
		{
			string? s = remove != null ? str.Replace(remove, "") : str;

			Regex? regex = new(@"
                    (?<=[A-Z])(?=[A-Z][a-z]) |
                    (?<=[^A-Z])(?=[A-Z]) |
                    (?<=[A-Za-z])(?=[^A-Za-z])", RegexOptions.IgnorePatternWhitespace);

			return regex.Replace(s, " ");
		}

		public static void OpenUrl(string url)
		{
			try
			{
				Process.Start(url);
			}
			catch
			{
				try
				{
					// hack because of this: https://github.com/dotnet/corefx/issues/10361
					if (RuntimeInformation.IsOSPlatform(OSPlatform.Windows))
					{
						url = url.Replace("&", "^&");
						Process.Start(new ProcessStartInfo("cmd", $"/c start {url}") {CreateNoWindow = true});
					}
					else if (RuntimeInformation.IsOSPlatform(OSPlatform.Linux))
					{
						Process.Start("xdg-open", url);
					}
				}
				catch (Exception ex)
				{
					Logger.Error("OpenUrl", "Error trying to open url: " + ex.Message);
				}
			}
		}
	}
}