using System;
using SezzUI.GameEvents;

namespace SezzUI
{
	internal class EventManager : IDisposable
	{
		internal static Game Game => Game.Instance;
		internal static Player Player => Player.Instance;
		internal static Combat Combat => Combat.Instance;
		internal static Cooldown Cooldown => Cooldown.Instance;

		#region Singleton

		public static void Initialize()
		{
			Instance = new();
		}

		public static EventManager Instance { get; private set; } = null!;

		~EventManager()
		{
			Dispose(false);
		}

		public void Dispose()
		{
			Dispose(true);
			GC.SuppressFinalize(this);
		}

		protected void Dispose(bool disposing)
		{
			if (!disposing)
			{
				return;
			}

			if (Game.Initialized)
			{
				Game.Dispose();
			}

			if (Player.Initialized)
			{
				Player.Dispose();
			}

			if (Combat.Initialized)
			{
				Combat.Dispose();
			}

			if (Cooldown.Initialized)
			{
				Cooldown.Dispose();
			}

			Instance = null!;
		}

		#endregion
	}
}