﻿using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Numerics;
using Dalamud.Game.ClientState.Objects.SubKinds;
using Dalamud.Interface;
using Dalamud.Interface.ImGuiFileDialog;
using ImGuiNET;
using Newtonsoft.Json;
using SezzUI.Configuration.Attributes;
using SezzUI.Configuration.Tree;
using SezzUI.Enums;
using SezzUI.Helper;
using SezzUI.Logging;

namespace SezzUI.Configuration.Profiles;

public class ProfilesManager
{
	#region Singleton

	public readonly SectionNode ProfilesNode;
	internal PluginLogger Logger;

	private ProfilesManager()
	{
		Logger = new(GetType().Name);

		// fake nodes
		ProfilesNode = new();
		ProfilesNode.Name = "Profiles";

		NestedSubSectionNode subSectionNode = new();
		subSectionNode.Name = "General";
		subSectionNode.Depth = 0;

		ProfilesConfigPageNode configPageNode = new();

		subSectionNode.Add(configPageNode);
		ProfilesNode.Add(subSectionNode);

		Singletons.Get<ConfigurationManager>().AddExtraSectionNode(ProfilesNode);

		// default profile
		if (!Profiles.ContainsKey(DefaultProfileName))
		{
			Profile defaultProfile = new(DefaultProfileName);
			Profiles.Add(DefaultProfileName, defaultProfile);
		}

		// make sure default profile file is created the first time this runs
		if (!File.Exists(CurrentProfilePath()))
		{
			SaveCurrentProfile();
		}
	}

	public static void Initialize()
	{
		try
		{
			string jsonString = File.ReadAllText(JsonPath);
			ProfilesManager? instance = JsonConvert.DeserializeObject<ProfilesManager>(jsonString);
			if (instance != null)
			{
				Instance = instance;

				bool needsSave = false;
				foreach (Profile profile in Instance.Profiles.Values)
				{
					needsSave |= profile.AutoSwitchData.ValidateRolesData();
				}

				if (needsSave)
				{
					Instance.Save();
				}
			}
		}
		catch
		{
			Instance = new();
		}
	}

	public static ProfilesManager Instance { get; private set; } = null!;

	~ProfilesManager()
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

		Instance = null!;
	}

	#endregion

	private string _currentProfileName = "Default";

	public string CurrentProfileName
	{
		get => _currentProfileName;
		set
		{
			if (_currentProfileName == value)
			{
				return;
			}

			_currentProfileName = value;

			if (_currentProfileName == null || _currentProfileName.Length == 0)
			{
				_currentProfileName = DefaultProfileName;
			}

			_selectedProfileIndex = Math.Max(0, Profiles.Keys.IndexOf(_currentProfileName));
		}
	}

	[JsonIgnore]
	private static string ProfilesPath => Path.Combine(Singletons.Get<ConfigurationManager>().ConfigDirectory, "Profiles");

	[JsonIgnore]
	private static string JsonPath => Path.Combine(ProfilesPath, "Profiles.json");

	[JsonIgnore]
	private readonly string DefaultProfileName = "Default";

	[JsonIgnore]
	private string _newProfileName = "";

	[JsonIgnore]
	private int _copyFromIndex;

	[JsonIgnore]
	private int _selectedProfileIndex;

	[JsonIgnore]
	private string? _errorMessage;

	[JsonIgnore]
	private string? _deletingProfileName;

	[JsonIgnore]
	private string? _resetingProfileName;

	[JsonIgnore]
	private string? _renamingProfileName;

	[JsonIgnore]
	private readonly FileDialogManager _fileDialogManager = new();

	public SortedList<string, Profile> Profiles = new();

	public Profile CurrentProfile()
	{
		if (_currentProfileName == null || _currentProfileName.Length == 0)
		{
			_currentProfileName = DefaultProfileName;
		}

		return Profiles[_currentProfileName];
	}

	public void SaveCurrentProfile()
	{
		if (Singletons.Get<ConfigurationManager>() == null)
		{
			return;
		}

		try
		{
			Save();
			SaveCurrentProfile(Singletons.Get<ConfigurationManager>().ExportCurrentConfigs());
		}
		catch (Exception ex)
		{
			Logger.Error($"Error saving profile: {ex}");
		}
	}

	public void SaveCurrentProfile(string? exportString)
	{
		if (exportString == null)
		{
			return;
		}

		try
		{
			Directory.CreateDirectory(ProfilesPath);

			File.WriteAllText(CurrentProfilePath(), exportString);
		}
		catch (Exception ex)
		{
			Logger.Error($"Error saving profile: {ex}");
		}
	}

	public bool LoadCurrentProfile()
	{
		try
		{
			string importString = File.ReadAllText(CurrentProfilePath());
			return Singletons.Get<ConfigurationManager>().ImportProfile(importString);
		}
		catch (Exception ex)
		{
			Logger.Error($"Error loading profile: {ex}");
		}

		return false;
	}

	public void UpdateCurrentProfile()
	{
		PlayerCharacter? player = Services.ClientState.LocalPlayer;
		if (player == null)
		{
			return;
		}

		uint jobId = player.ClassJob.Id;
		Profile currentProfile = CurrentProfile();
		JobRoles role = JobsHelper.RoleForJob(jobId);
		int index = JobsHelper.JobsByRole[role].IndexOf(jobId);

		if (index < 0)
		{
			return;
		}

		// current profile is enabled for this job, do nothing
		if (currentProfile.AutoSwitchEnabled && currentProfile.AutoSwitchData.IsEnabled(role, index))
		{
			return;
		}

		// find a profile that is enabled for this job
		foreach (Profile profile in Profiles.Values)
		{
			if (!profile.AutoSwitchEnabled || profile == currentProfile)
			{
				continue;
			}

			// found a valid profile, switch to it
			if (profile.AutoSwitchData.IsEnabled(role, index))
			{
				SwitchToProfile(profile.Name);
				return;
			}
		}
	}

	public void CheckUpdateSwitchCurrentProfile(string specifiedProfile)
	{
		// found a valid profile, switch to it
		if (Profiles.ContainsKey(specifiedProfile))
		{
			SwitchToProfile(specifiedProfile);
		}
	}

	private string? SwitchToProfile(string profile, bool save = true)
	{
		// save if needed before switching
		if (save)
		{
			Singletons.Get<ConfigurationManager>().SaveConfigurations();
		}

		string oldProfile = _currentProfileName;
		_currentProfileName = profile;
		Profile currentProfile = CurrentProfile();

		if (currentProfile.AttachHudEnabled && currentProfile.HudLayout != 0)
		{
		}

		if (!LoadCurrentProfile())
		{
			_currentProfileName = oldProfile;
			return "Couldn't load profile \"" + profile + "\"!";
		}

		_selectedProfileIndex = Math.Max(0, Profiles.IndexOfKey(profile));

		try
		{
			Save();
			Services.PluginInterface.UiBuilder.RebuildFonts();
		}
		catch (Exception ex)
		{
			Logger.Error($"Error saving profile: {ex}");
			return "Couldn't load profile \"" + profile + "\"!";
		}

		return null;
	}

	private string CurrentProfilePath() => Path.Combine(ProfilesPath, _currentProfileName + ".sezzui");

	private string? CloneProfile(string profileName, string newProfileName)
	{
		string srcPath = Path.Combine(ProfilesPath, profileName + ".sezzui");
		string dstPath = Path.Combine(ProfilesPath, newProfileName + ".sezzui");

		return CloneProfile(profileName, srcPath, newProfileName, dstPath);
	}

	private string? CloneProfile(string profileName, string srcPath, string newProfileName, string dstPath)
	{
		if (newProfileName.Length == 0)
		{
			return null;
		}

		if (Profiles.Keys.Contains(newProfileName))
		{
			return "A profile with the name \"" + newProfileName + "\" already exists!";
		}

		try
		{
			if (!File.Exists(srcPath))
			{
				return "Couldn't find profile \"" + profileName + "\"!";
			}

			if (File.Exists(dstPath))
			{
				return "A profile with the name \"" + newProfileName + "\" already exists!";
			}

			File.Copy(srcPath, dstPath);
			Profile newProfile = new(newProfileName);
			Profiles.Add(newProfileName, newProfile);

			Save();
		}
		catch (Exception ex)
		{
			Logger.Error($"Error cloning profile: {ex}");
			return "Error trying to clone profile \"" + profileName + "\"!";
		}

		return null;
	}

	private string? RenameCurrentProfile(string newProfileName)
	{
		if (_currentProfileName == newProfileName || newProfileName.Length == 0)
		{
			return null;
		}

		if (Profiles.ContainsKey(newProfileName))
		{
			return "A profile with the name \"" + newProfileName + "\" already exists!";
		}

		string srcPath = Path.Combine(ProfilesPath, _currentProfileName + ".sezzui");
		string dstPath = Path.Combine(ProfilesPath, newProfileName + ".sezzui");

		try
		{
			if (File.Exists(dstPath))
			{
				return "A profile with the name \"" + newProfileName + "\" already exists!";
			}

			File.Move(srcPath, dstPath);

			Profile profile = Profiles[_currentProfileName];
			profile.Name = newProfileName;

			Profiles.Remove(_currentProfileName);
			Profiles.Add(newProfileName, profile);

			_currentProfileName = newProfileName;

			Save();
		}
		catch (Exception ex)
		{
			Logger.Error($"Error renaming profile: {ex}");
			return "Error trying to rename profile \"" + _currentProfileName + "\"!";
		}

		return null;
	}

	private string? Import(string newProfileName, string importString)
	{
		if (newProfileName.Length == 0)
		{
			return null;
		}

		if (Profiles.Keys.Contains(newProfileName))
		{
			return "A profile with the name \"" + newProfileName + "\" already exists!";
		}

		string dstPath = Path.Combine(ProfilesPath, newProfileName + ".sezzui");

		try
		{
			if (File.Exists(dstPath))
			{
				return "A profile with the name \"" + newProfileName + "\" already exists!";
			}

			File.WriteAllText(dstPath, importString);

			Profile newProfile = new(newProfileName);
			Profiles.Add(newProfileName, newProfile);

			string? errorMessage = SwitchToProfile(newProfileName, false);

			if (errorMessage != null)
			{
				Profiles.Remove(newProfileName);
				File.Delete(dstPath);
				Save();

				return errorMessage;
			}
		}
		catch (Exception ex)
		{
			Logger.Error($"Error importing profile: {ex}");
			return "Error trying to import profile \"" + newProfileName + "\"!";
		}

		return null;
	}

	private string? ImportFromClipboard(string newProfileName)
	{
		string importString = ImGui.GetClipboardText();
		if (importString.Length == 0)
		{
			return "Invalid import string!";
		}

		return Import(newProfileName, importString);
	}

	private void ImportFromFile(string newProfileName)
	{
		if (newProfileName.Length == 0)
		{
			return;
		}

		Action<bool, string> callback = (finished, path) =>
		{
			try
			{
				if (finished && path.Length > 0)
				{
					string importString = File.ReadAllText(path);
					_errorMessage = Import(newProfileName, importString);

					if (_errorMessage == null)
					{
						_newProfileName = "";
					}
				}
			}
			catch (Exception ex)
			{
				Logger.Error($"Error reading import file: {ex}");
				_errorMessage = "Error reading the file!";
			}
		};

		_fileDialogManager.OpenFileDialog("Select a SezzUI Profile to import", "SezzUI Profile{.sezzui}", callback);
	}

	private void ExportToFile(string newProfileName)
	{
		if (newProfileName.Length == 0)
		{
			return;
		}

		Action<bool, string> callback = (finished, path) =>
		{
			try
			{
				string src = CurrentProfilePath();
				if (finished && path.Length > 0 && src != path)
				{
					File.Copy(src, path, true);
				}
			}
			catch (Exception ex)
			{
				Logger.Error($"Error copying file: {ex}");
				_errorMessage = "Error exporting the file!";
			}
		};

		_fileDialogManager.SaveFileDialog("Save Profile", "SezzUI Profile{.sezzui}", newProfileName + ".sezzui", ".sezzui", callback);
	}

	private string? DeleteProfile(string profileName)
	{
		if (!Profiles.ContainsKey(profileName))
		{
			return "Couldn't find profile \"" + profileName + "\"!";
		}

		string path = Path.Combine(ProfilesPath, profileName + ".sezzui");

		try
		{
			if (!File.Exists(path))
			{
				return "Couldn't find profile \"" + profileName + "\"!";
			}

			File.Delete(path);
			Profiles.Remove(profileName);

			Save();

			if (_currentProfileName == profileName)
			{
				return SwitchToProfile(DefaultProfileName, false);
			}
		}
		catch (Exception ex)
		{
			Logger.Error($"Error deleting profile: {ex}");
			return "Error trying to delete profile \"" + profileName + "\"!";
		}

		return null;
	}

	private void Save()
	{
		string jsonString = JsonConvert.SerializeObject(this, Formatting.Indented, new JsonSerializerSettings
		{
			TypeNameAssemblyFormatHandling = TypeNameAssemblyFormatHandling.Simple,
			TypeNameHandling = TypeNameHandling.Objects
		});

		Directory.CreateDirectory(ProfilesPath);
		File.WriteAllText(JsonPath, jsonString);
	}

	public bool Draw(ref bool changed)
	{
		string[] profiles = Profiles.Keys.ToArray();

		if (ImGui.BeginChild("Profiles", new(800, 600), false))
		{
			if (Profiles.Count == 0)
			{
				ImGuiHelper.Tab();
				ImGui.Text("Profiles not found in \"%appdata%/Roaming/XIVLauncher/pluginConfigs/SezzUI/Profiles/\"");
				return false;
			}

			ImGui.PushItemWidth(408);
			ImGuiHelper.NewLineAndTab();
			if (ImGui.Combo("Active Profile", ref _selectedProfileIndex, profiles, profiles.Length, 10))
			{
				string newProfileName = profiles[_selectedProfileIndex];

				if (_currentProfileName != newProfileName)
				{
					_errorMessage = SwitchToProfile(newProfileName);
				}
			}

			// reset
			ImGui.SameLine();
			ImGui.PushFont(UiBuilder.IconFont);
			if (ImGui.Button("\uf2f9", new(0, 0)))
			{
				_resetingProfileName = _currentProfileName;
			}

			ImGui.PopFont();
			if (ImGui.IsItemHovered())
			{
				ImGui.SetTooltip("Reset");
			}

			if (_currentProfileName != DefaultProfileName)
			{
				// rename
				ImGui.SameLine();
				ImGui.PushFont(UiBuilder.IconFont);
				if (ImGui.Button(FontAwesomeIcon.Pen.ToIconString()))
				{
					_renamingProfileName = _currentProfileName;
				}

				ImGui.PopFont();
				if (ImGui.IsItemHovered())
				{
					ImGui.SetTooltip("Rename");
				}

				// delete
				ImGui.SameLine();
				ImGui.PushFont(UiBuilder.IconFont);
				if (_currentProfileName != DefaultProfileName && ImGui.Button(FontAwesomeIcon.Trash.ToIconString()))
				{
					_deletingProfileName = _currentProfileName;
				}

				ImGui.PopFont();
				if (ImGui.IsItemHovered())
				{
					ImGui.SetTooltip("Delete");
				}
			}

			// export to string
			ImGuiHelper.Tab();
			ImGui.SameLine();
			if (ImGui.Button("Export to Clipboard", new(200, 0)))
			{
				string? exportString = Singletons.Get<ConfigurationManager>().ExportCurrentConfigs();
				if (exportString != null)
				{
					ImGui.SetClipboardText(exportString);
					ImGui.OpenPopup("export_succes_popup");
				}
			}

			// export success popup
			if (ImGui.BeginPopup("export_succes_popup"))
			{
				ImGui.Text("Profile export string copied to clipboard!");
				ImGui.EndPopup();
			}

			ImGui.SameLine();
			if (ImGui.Button("Export to File", new(200, 0)))
			{
				ExportToFile(_currentProfileName);
			}

			ImGuiHelper.NewLineAndTab();
			DrawAttachHudLayout(ref changed);

			ImGuiHelper.NewLineAndTab();
			DrawAutoSwitchSettings(ref changed);

			ImGuiHelper.DrawSeparator(1, 1);
			ImGuiHelper.Tab();
			ImGui.Text("Create a new profile:");

			ImGuiHelper.Tab();
			ImGui.PushItemWidth(408);
			ImGui.InputText("Profile Name", ref _newProfileName, 200);

			ImGuiHelper.Tab();
			ImGui.PushItemWidth(200);
			ImGui.Combo("", ref _copyFromIndex, profiles, profiles.Length, 10);

			ImGui.SameLine();
			if (ImGui.Button("Copy", new(200, 0)))
			{
				_newProfileName = _newProfileName.Trim();
				if (_newProfileName.Length == 0)
				{
					ImGui.OpenPopup("import_error_popup");
				}
				else
				{
					_errorMessage = CloneProfile(profiles[_copyFromIndex], _newProfileName);

					if (_errorMessage == null)
					{
						_errorMessage = SwitchToProfile(_newProfileName);
						_newProfileName = "";
					}
				}
			}

			ImGuiHelper.NewLineAndTab();
			if (ImGui.Button("Import From Clipboard", new(200, 0)))
			{
				_newProfileName = _newProfileName.Trim();
				if (_newProfileName.Length == 0)
				{
					ImGui.OpenPopup("import_error_popup");
				}
				else
				{
					_errorMessage = ImportFromClipboard(_newProfileName);

					if (_errorMessage == null)
					{
						_newProfileName = "";
					}
				}
			}

			ImGui.SameLine();
			if (ImGui.Button("Import From File", new(200, 0)))
			{
				_newProfileName = _newProfileName.Trim();
				if (_newProfileName.Length == 0)
				{
					ImGui.OpenPopup("import_error_popup");
				}
				else
				{
					ImportFromFile(_newProfileName);
				}
			}

			// no name popup
			if (ImGui.BeginPopup("import_error_popup"))
			{
				ImGui.Text("Please type a name for the new profile!");
				ImGui.EndPopup();
			}
		}

		ImGui.EndChild();

		// error message
		if (_errorMessage != null)
		{
			if (ImGuiHelper.DrawErrorModal(_errorMessage))
			{
				_errorMessage = null;
			}
		}

		// delete confirmation
		if (_deletingProfileName != null)
		{
			string[] lines = {"Are you sure you want to delete the profile:", "\u2002- " + _deletingProfileName};
			(bool didConfirm, bool didClose) = ImGuiHelper.DrawConfirmationModal("Delete?", lines);

			if (didConfirm)
			{
				_errorMessage = DeleteProfile(_deletingProfileName);
				changed = true;
			}

			if (didConfirm || didClose)
			{
				_deletingProfileName = null;
			}
		}

		// reset confirmation
		if (_resetingProfileName != null)
		{
			string[] lines = {"Are you sure you want to reset the profile:", "\u2002- " + _resetingProfileName};
			(bool didConfirm, bool didClose) = ImGuiHelper.DrawConfirmationModal("Reset?", lines);

			if (didConfirm)
			{
				Singletons.Get<ConfigurationManager>().ResetConfig();
				changed = true;
			}

			if (didConfirm || didClose)
			{
				_resetingProfileName = null;
			}
		}

		// rename modal
		if (_renamingProfileName != null)
		{
			(bool didConfirm, bool didClose) = ImGuiHelper.DrawInputModal("Rename", "Type a new name for the profile:", ref _renamingProfileName);

			if (didConfirm)
			{
				_errorMessage = RenameCurrentProfile(_renamingProfileName);

				changed = true;
			}

			if (didConfirm || didClose)
			{
				_renamingProfileName = null;
			}
		}

		_fileDialogManager.Draw();

		return false;
	}

	private void DrawAutoSwitchSettings(ref bool changed)
	{
		Profile profile = CurrentProfile();

		changed |= ImGui.Checkbox("Auto-Switch For Specific Jobs", ref profile.AutoSwitchEnabled);

		if (!profile.AutoSwitchEnabled)
		{
			return;
		}

		AutoSwitchData data = profile.AutoSwitchData;
		Vector2 cursorPos = ImGui.GetCursorPos() + new Vector2(14, 14);
		Vector2 originalPos = cursorPos;
		float maxY = 0;

		JobRoles[] roles = (JobRoles[]) Enum.GetValues(typeof(JobRoles));

		foreach (JobRoles role in roles)
		{
			if (role == JobRoles.Unknown)
			{
				continue;
			}

			if (!data.Map.ContainsKey(role))
			{
				continue;
			}

			bool roleValue = data.GetRoleEnabled(role);
			string roleName = JobsHelper.RoleNames[role];

			ImGui.SetCursorPos(cursorPos);
			if (ImGui.Checkbox(roleName, ref roleValue))
			{
				data.SetRoleEnabled(role, roleValue);
				changed = true;
			}

			cursorPos.Y += 40;
			int jobCount = data.Map[role].Count;

			for (int i = 0; i < jobCount; i++)
			{
				maxY = Math.Max(cursorPos.Y, maxY);
				uint jobId = JobsHelper.JobsByRole[role][i];
				bool jobValue = data.Map[role][i];
				string jobName = JobsHelper.JobNames[jobId];

				ImGui.SetCursorPos(cursorPos);
				if (ImGui.Checkbox(jobName, ref jobValue))
				{
					data.Map[role][i] = jobValue;
					changed = true;
				}

				cursorPos.Y += 30;
			}

			cursorPos.X += 100;
			cursorPos.Y = originalPos.Y;
		}

		ImGui.SetCursorPos(new(originalPos.X, maxY + 30));
	}

	private void DrawAttachHudLayout(ref bool changed)
	{
		Profile profile = CurrentProfile();

		changed |= ImGui.Checkbox("Attach HUD Layout to this profile", ref profile.AttachHudEnabled);

		if (!profile.AttachHudEnabled)
		{
			profile.HudLayout = 0;
			return;
		}

		int hudLayout = profile.HudLayout;

		ImGui.Text("\u2002\u2002\u2514");

		for (int i = 1; i <= 4; i++)
		{
			ImGui.SameLine();
			bool hudLayoutEnabled = hudLayout == i;
			if (ImGui.Checkbox("Hud Layout " + i, ref hudLayoutEnabled))
			{
				profile.HudLayout = i;
				changed = true;
			}
		}
	}
}

// fake config object
[Disableable(false)]
[Exportable(false)]
[Shareable(false)]
[Resettable(false)]
public class ProfilesConfig : PluginConfigObject
{
	public new static ProfilesConfig DefaultConfig() => new();
}

// fake config page node
public class ProfilesConfigPageNode : ConfigPageNode
{
	public ProfilesConfigPageNode()
	{
		ConfigObject = new ProfilesConfig();
	}

	public override bool Draw(ref bool changed) => ProfilesManager.Instance.Draw(ref changed);
}