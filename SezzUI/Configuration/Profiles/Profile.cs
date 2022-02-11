using System;
using System.Collections.Generic;
using SezzUI.Enums;
using SezzUI.Helper;

namespace SezzUI.Configuration.Profiles
{
	public class Profile
	{
		public string Name;

		public bool AutoSwitchEnabled;
		public AutoSwitchData AutoSwitchData = new();
		public int HudLayout;
		public bool AttachHudEnabled;

		public Profile(string name, bool autoSwitchEnabled = false, AutoSwitchData? autoSwitchData = null, bool attachHudEnabled = false, int hudLayout = 0)
		{
			Name = name;

			AutoSwitchEnabled = autoSwitchEnabled;
			AutoSwitchData = autoSwitchData ?? AutoSwitchData;

			AttachHudEnabled = attachHudEnabled;
			HudLayout = hudLayout;
		}
	}

	public class AutoSwitchData
	{
		public Dictionary<JobRoles, List<bool>> Map;

		public AutoSwitchData()
		{
			Map = new();

			JobRoles[] roles = (JobRoles[]) Enum.GetValues(typeof(JobRoles));

			foreach (JobRoles role in roles)
			{
				int count = JobsHelper.JobsByRole[role].Count;
				List<bool> list = new(count);

				for (int i = 0; i < count; i++)
				{
					list.Add(false);
				}

				Map.Add(role, list);
			}
		}

		public bool GetRoleEnabled(JobRoles role)
		{
			foreach (bool value in Map[role])
			{
				if (!value)
				{
					return false;
				}
			}

			return true;
		}

		public void SetRoleEnabled(JobRoles role, bool value)
		{
			for (int i = 0; i < Map[role].Count; i++)
			{
				Map[role][i] = value;
			}
		}

		public bool IsEnabled(JobRoles role, int index)
		{
			if (Map.TryGetValue(role, out List<bool>? list) && list != null)
			{
				if (index >= list.Count)
				{
					return false;
				}

				return list[index];
			}

			return false;
		}

		public bool ValidateRolesData()
		{
			bool changed = false;

			JobRoles[] roles = (JobRoles[]) Enum.GetValues(typeof(JobRoles));

			foreach (JobRoles role in roles)
			{
				int count = JobsHelper.JobsByRole[role].Count;
				List<bool> list = Map[role];

				if (list.Count < count)
				{
					for (int i = 0; i < count - list.Count; i++)
					{
						list.Add(false);
					}

					changed = true;
				}
			}

			return changed;
		}
	}
}