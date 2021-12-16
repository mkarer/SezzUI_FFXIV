using System.Numerics;
using System.Collections.Generic;

namespace SezzUI.Modules.JobHud
{
	public class IconColor
	{
		public Vector4 Icon;
		public Vector4 Border;
		public Vector4 Gloss;
	}

	public enum IconState
	{
		FadedOut = 0,
		Soon = 1,
		Ready = 2,
		ReadyOutOfResources = 3
	}

	public static class Defaults
	{
		// Icon State Colors
		public static readonly Dictionary<IconState, IconColor> StateColors = new()
		{
			{ IconState.FadedOut, new IconColor { Icon = new Vector4(1, 1, 1, 0.4f), Border = new Vector4(1, 1, 1, 0.3f), Gloss = new Vector4(1, 1, 1, 0.25f) } },
			{ IconState.Soon, new IconColor { Icon = new Vector4(0.7f, 0.9f, 1, 0.8f), Border = new Vector4(1, 0, 0, 0.6f), Gloss = new Vector4(1, 1, 1, 0.25f) } },
			{ IconState.Ready, new IconColor { Icon = new Vector4(1, 1, 1, 0.9f), Border = new Vector4(1, 0, 0, 0.6f), Gloss = new Vector4(1, 1, 1, 0.25f) } },
			{ IconState.ReadyOutOfResources, new IconColor { Icon = new Vector4(0.7f, 0.7f, 1, 0.9f), Border = new Vector4(1, 0, 0, 0.6f), Gloss = new Vector4(1, 1, 1, 0.25f) } }
		};

		// Icon Status Progress Bar
		public static readonly Vector4 IconBarColor = new(0.24f, 0.78f, 0.92f, 1);
		public static readonly Vector4 IconBarBGColor = new(0.1f, 0.1f, 0.1f, 0.8f);
		public static readonly Vector4 IconBarSeparatorColor = new(0.2f, 0.2f, 0.2f, 1);

		// Job Colors
		public static readonly Dictionary<uint, Vector4> JobColors = new()
		{
			// Tanks
			{ DelvUI.Helpers.JobIDs.PLD, new(168f / 255f, 210f / 255f, 230f / 255f, 100f / 100f) },
			{ DelvUI.Helpers.JobIDs.DRK, new(209f / 255f, 38f / 255f, 204f / 255f, 100f / 100f) },
			{ DelvUI.Helpers.JobIDs.WAR, new(207f / 255f, 38f / 255f, 33f / 255f, 100f / 100f) },
			{ DelvUI.Helpers.JobIDs.GNB, new(193f / 255f, 106f / 255f, 0f / 255f, 100f / 100f) },
			{ DelvUI.Helpers.JobIDs.GLD, new(168f / 255f, 210f / 255f, 230f / 255f, 100f / 100f) },
			{ DelvUI.Helpers.JobIDs.MRD, new(207f / 255f, 38f / 255f, 33f / 255f, 100f / 100f) },
			// Healers
			{ DelvUI.Helpers.JobIDs.SCH, new(134f / 255f, 87f / 255f, 255f / 255f, 100f / 100f) },
			{ DelvUI.Helpers.JobIDs.WHM, new(255f / 255f, 240f / 255f, 220f / 255f, 100f / 100f) },
			{ DelvUI.Helpers.JobIDs.AST, new(255f / 255f, 231f / 255f, 74f / 255f, 100f / 100f) },
			{ DelvUI.Helpers.JobIDs.SGE, new(144f / 255f, 176f / 255f, 255f / 255f, 100f / 100f) },
			{ DelvUI.Helpers.JobIDs.CNJ, new(255f / 255f, 240f / 255f, 220f / 255f, 100f / 100f) },
			// Melee
			{ DelvUI.Helpers.JobIDs.MNK, new(214f / 255f, 156f / 255f, 0f / 255f, 100f / 100f) },
			{ DelvUI.Helpers.JobIDs.NIN, new(175f / 255f, 25f / 255f, 100f / 255f, 100f / 100f) },
			{ DelvUI.Helpers.JobIDs.DRG, new(65f / 255f, 100f / 255f, 205f / 255f, 100f / 100f) },
			{ DelvUI.Helpers.JobIDs.SAM, new(228f / 255f, 109f / 255f, 4f / 255f, 100f / 100f) },
			{ DelvUI.Helpers.JobIDs.RPR, new(150f / 255f, 90f / 255f, 144f / 255f, 100f / 100f) },
			{ DelvUI.Helpers.JobIDs.PGL, new(214f / 255f, 156f / 255f, 0f / 255f, 100f / 100f) },
			{ DelvUI.Helpers.JobIDs.ROG, new(175f / 255f, 25f / 255f, 100f / 255f, 100f / 100f) },
			{ DelvUI.Helpers.JobIDs.LNC, new(65f / 255f, 100f / 255f, 205f / 255f, 100f / 100f) },
			// Ranged
			{ DelvUI.Helpers.JobIDs.BRD, new(145f / 255f, 186f / 255f, 94f / 255f, 100f / 100f) },
			{ DelvUI.Helpers.JobIDs.MCH, new(110f / 255f, 225f / 255f, 214f / 255f, 100f / 100f) },
			{ DelvUI.Helpers.JobIDs.DNC, new(226f / 255f, 176f / 255f, 175f / 255f, 100f / 100f) },
			{ DelvUI.Helpers.JobIDs.ARC, new(145f / 255f, 186f / 255f, 94f / 255f, 100f / 100f) },
			// Caster
			{ DelvUI.Helpers.JobIDs.BLM, new(165f / 255f, 121f / 255f, 214f / 255f, 100f / 100f) },
			{ DelvUI.Helpers.JobIDs.SMN, new(45f / 255f, 155f / 255f, 120f / 255f, 100f / 100f) },
			{ DelvUI.Helpers.JobIDs.RDM, new(232f / 255f, 123f / 255f, 123f / 255f, 100f / 100f) },
			{ DelvUI.Helpers.JobIDs.BLU, new(0f / 255f, 185f / 255f, 247f / 255f, 100f / 100f) },
			{ DelvUI.Helpers.JobIDs.THM, new(165f / 255f, 121f / 255f, 214f / 255f, 100f / 100f) },
			{ DelvUI.Helpers.JobIDs.ACN, new(45f / 255f, 155f / 255f, 120f / 255f, 100f / 100f) },
		};
	}
}
