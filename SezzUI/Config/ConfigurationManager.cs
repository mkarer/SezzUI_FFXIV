using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using Dalamud.Interface.Windowing;
using DelvUI.Helpers;
using ImGuiScene;
using SezzUI.Config.Profiles;
using SezzUI.Config.Tree;
using SezzUI.Config.Windows;
using SezzUI.Interface;
using SezzUI.Interface.GeneralElements;

namespace SezzUI.Config
{
	public delegate void ConfigurationManagerEventHandler(ConfigurationManager configurationManager);

	public delegate void ConfigObjectResetDelegate(ConfigurationManager configurationManager, PluginConfigObject config);

	public class ConfigurationManager : IDisposable
	{
		public static ConfigurationManager Instance { get; private set; } = null!;
		internal PluginLogger Logger;

		public readonly TextureWrap? BannerImage;

		private BaseNode _configBaseNode;

		public BaseNode ConfigBaseNode
		{
			get => _configBaseNode;
			set
			{
				_configBaseNode = value;
				_mainConfigWindow.node = value;
			}
		}

		private readonly WindowSystem _windowSystem;
		private readonly MainConfigWindow _mainConfigWindow;
		private readonly GridWindow _gridWindow;

		public bool IsConfigWindowOpened => _mainConfigWindow.IsOpen;
		public bool ShowingModalWindow = false;

		public GradientDirection GradientDirection
		{
			get
			{
				MiscColorConfig? config = Instance.GetConfigObject<MiscColorConfig>();
				return config != null ? config.GradientDirection : GradientDirection.None;
			}
		}

		public string ConfigDirectory;

		public string CurrentVersion => Plugin.Version;
		public string? PreviousVersion { get; private set; }

		private bool _needsProfileUpdate;
		private bool _lockHUD = true;

		public bool LockHUD
		{
			get => _lockHUD;
			set
			{
				if (_lockHUD == value)
				{
					return;
				}

				_lockHUD = value;
				_mainConfigWindow.IsOpen = value;
				_gridWindow.IsOpen = !value;

				LockEvent?.Invoke(this);

				if (_lockHUD)
				{
					ConfigBaseNode.NeedsSave = true;
				}
			}
		}

		public delegate void HUDVisibilityChangedDelegate(bool visible);

		public event HUDVisibilityChangedDelegate? HUDVisibilityChanged;

		private bool _showHUD = true;

		public bool ShowHUD
		{
			get => _showHUD;
			set
			{
				if (_showHUD == value)
				{
					return;
				}

				_showHUD = value;
				HUDVisibilityChanged?.Invoke(value);
			}
		}

		/// <summary>
		///     Triggers when a specific PluginConfigObject is reset.
		/// </summary>
		public event ConfigObjectResetDelegate? Reset;

		public event ConfigurationManagerEventHandler? ResetEvent;
		public event ConfigurationManagerEventHandler? LockEvent;
		public event ConfigurationManagerEventHandler? ConfigClosedEvent;

		public ConfigurationManager()
		{
			Logger = new(GetType().Name);
			BannerImage = Plugin.BannerTexture;
			ConfigDirectory = Plugin.PluginInterface.GetPluginConfigDirectory();

			_configBaseNode = new();
			InitializeBaseNode(_configBaseNode);
			_configBaseNode.ConfigObjectResetEvent += OnConfigNodeReset;

			_mainConfigWindow = new("SezzUI Settings");
			_mainConfigWindow.node = _configBaseNode;
			_mainConfigWindow.CloseAction = () =>
			{
				ConfigClosedEvent?.Invoke(this);

				if (ConfigBaseNode.NeedsSave)
				{
					SaveConfigurations();
				}

				if (_needsProfileUpdate)
				{
					UpdateCurrentProfile();
					_needsProfileUpdate = false;
				}
			};

			_gridWindow = new("Grid ##SezzUI");

			_windowSystem = new("SezzUI_Windows");
			_windowSystem.AddWindow(_mainConfigWindow);
			_windowSystem.AddWindow(_gridWindow);

			CheckVersion();

			Plugin.ClientState.Logout += OnLogout;
		}

		~ConfigurationManager()
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

			ConfigBaseNode.ConfigObjectResetEvent -= OnConfigNodeReset;
			Plugin.ClientState.Logout -= OnLogout;
			BannerImage?.Dispose();

			Instance = null!;
		}

		public static void Initialize()
		{
			Instance = new();
		}

		private void OnConfigNodeReset(BaseNode sender)
		{
			// TODO: Test what a profile reset actually does.
			Logger.Debug("OnConfigNodeReset", $"Node: {sender.GetType()}");
			ResetEvent?.Invoke(this);
		}

		public void OnConfigObjectReset(PluginConfigObject config)
		{
			// TODO: Test what a profile reset actually does.
			Logger.Debug("OnConfigObjectReset", $"ConfigObject: {config.GetType()}");
			Reset?.Invoke(this, config);
		}

		private void OnLogout(object? sender, EventArgs? args)
		{
			SaveConfigurations();
			ProfilesManager.Instance.SaveCurrentProfile();
		}

		private string LoadChangelog()
		{
			string path = Path.Combine(Plugin.AssemblyLocation, "changelog.md");

			try
			{
				string fullChangelog = File.ReadAllText(path);
				string versionChangelog = fullChangelog.Split("#", StringSplitOptions.RemoveEmptyEntries)[0];
				return versionChangelog.Replace(Plugin.Version, "");
			}
			catch (Exception e)
			{
				Logger.Error("LoadChangelog", "Error loading changelog: " + e.Message);
			}

			return "";
		}

		private void CheckVersion()
		{
			string path = Path.Combine(ConfigDirectory, "version");

			try
			{
				bool needsWrite = false;

				if (!File.Exists(path))
				{
					needsWrite = true;
				}
				else
				{
					PreviousVersion = File.ReadAllText(path);
					if (PreviousVersion != Plugin.Version)
					{
						needsWrite = true;
					}
				}

				if (needsWrite)
				{
					File.WriteAllText(path, Plugin.Version);
				}
			}
			catch (Exception e)
			{
				Logger.Error("CheckVersion", "Error checking version: " + e.Message);
			}
		}

		#region windows

		public void ToggleConfigWindow()
		{
			_mainConfigWindow.Toggle();
		}

		public void OpenConfigWindow()
		{
			_mainConfigWindow.IsOpen = false;
		}

		public void CloseConfigWindow()
		{
			_mainConfigWindow.IsOpen = false;
		}

		public void Draw()
		{
			_windowSystem.Draw();
		}

		public void AddExtraSectionNode(SectionNode node)
		{
			ConfigBaseNode.AddExtraSectionNode(node);
		}

		#endregion

		#region config getters and setters

		public PluginConfigObject GetConfigObjectForType(Type type)
		{
			MethodInfo? genericMethod = GetType().GetMethod("GetConfigObject");
			MethodInfo? method = genericMethod?.MakeGenericMethod(type);
			return (PluginConfigObject) method?.Invoke(this, null)!;
		}

		public T GetConfigObject<T>() where T : PluginConfigObject => ConfigBaseNode.GetConfigObject<T>()!;

		public static PluginConfigObject GetDefaultConfigObjectForType(Type type)
		{
			MethodInfo? method = type.GetMethod("DefaultConfig", BindingFlags.Public | BindingFlags.Static);
			return (PluginConfigObject) method?.Invoke(null, null)!;
		}

		public ConfigPageNode GetConfigPageNode<T>() where T : PluginConfigObject => ConfigBaseNode.GetConfigPageNode<T>()!;

		public void SetConfigObject(PluginConfigObject configObject) => ConfigBaseNode.SetConfigObject(configObject);

		#endregion

		#region load / save / profiles

		public void LoadOrInitializeFiles()
		{
			try
			{
				// detect if we need to create the config files (fresh install)
				if (Directory.GetDirectories(ConfigDirectory).Length == 0)
				{
					SaveConfigurations(true);
				}
				else
				{
					LoadConfigurations();

					// gotta save after initial load store possible version update changes right away
					SaveConfigurations(true);
				}
			}
			catch (Exception e)
			{
				Logger.Error("LoadOrInitializeFiles", "Error initializing configurations: " + e.Message);

				if (e.StackTrace != null)
				{
					Logger.Error(e.StackTrace);
				}
			}
		}

		public void ForceNeedsSave()
		{
			ConfigBaseNode.NeedsSave = true;
		}

		public void LoadConfigurations()
		{
			ConfigBaseNode.Load(ConfigDirectory, CurrentVersion, PreviousVersion);
		}

		public void SaveConfigurations(bool forced = false)
		{
			if (!forced && !ConfigBaseNode.NeedsSave)
			{
				return;
			}

			ConfigBaseNode.Save(ConfigDirectory);

			if (ProfilesManager.Instance != null)
			{
				ProfilesManager.Instance.SaveCurrentProfile();
			}

			ConfigBaseNode.NeedsSave = false;
		}

		public void UpdateCurrentProfile()
		{
			// dont update the profile on job change when the config window is opened
			if (_mainConfigWindow.IsOpen)
			{
				_needsProfileUpdate = true;
				return;
			}

			ProfilesManager.Instance.UpdateCurrentProfile();
		}

		public string? ExportCurrentConfigs() => ConfigBaseNode.GetBase64String();

		public bool ImportProfile(string rawString)
		{
			List<string> importStrings = new(rawString.Trim().Split(new[] {"|"}, StringSplitOptions.RemoveEmptyEntries));
			ImportData[] imports = importStrings.Select(str => new ImportData(str)).ToArray();

			BaseNode node = new();
			InitializeBaseNode(node);

			Dictionary<Type, PluginConfigObject> OldConfigObjects = new();

			foreach (ImportData importData in imports)
			{
				PluginConfigObject? config = importData.GetObject();
				if (config == null)
				{
					return false;
				}

				if (!node.SetConfigObject(config))
				{
					OldConfigObjects.Add(config.GetType(), config);
				}
			}

			try
			{
				// handle imports for breaking changes in the config
				if (UnmergeableConfigTypesPerVersion.TryGetValue(CurrentVersion, out List<Type>? types) && types != null)
				{
					foreach (Type type in types)
					{
						MethodInfo? genericMethod = node.GetType().GetMethod("GetConfigObject");
						MethodInfo? method = genericMethod?.MakeGenericMethod(type);
						PluginConfigObject? config = (PluginConfigObject?) method?.Invoke(node, null);

						if (config != null)
						{
							config.ImportFromOldVersion(OldConfigObjects, CurrentVersion, PreviousVersion);
							node.SetConfigObject(config); // needed to refresh nodes
						}
					}
				}

				node.Save(ConfigDirectory);
			}
			catch
			{
				return false;
			}

			string? oldSelection = ConfigBaseNode.SelectedOptionName;
			node.SelectedOptionName = oldSelection;
			node.AddExtraSectionNode(ProfilesManager.Instance.ProfilesNode);

			ConfigBaseNode.ConfigObjectResetEvent -= OnConfigNodeReset;
			ConfigBaseNode = node;
			ConfigBaseNode.ConfigObjectResetEvent += OnConfigNodeReset;

			ResetEvent?.Invoke(this);

			return true;
		}

		public void ResetConfig()
		{
			ConfigBaseNode.Reset();
			ResetEvent?.Invoke(this);
		}

		#endregion

		#region initialization

		private static void InitializeBaseNode(BaseNode node)
		{
			// creates node tree in the right order...
			foreach (Type type in ConfigObjectTypes)
			{
				MethodInfo? genericMethod = node.GetType().GetMethod("GetConfigPageNode");
				MethodInfo? method = genericMethod?.MakeGenericMethod(type);
				method?.Invoke(node, null);
			}
		}

		private static readonly Type[] ConfigObjectTypes =
		{
			// Core
			typeof(GeneralConfig),
#if DEBUG
			typeof(GeneralDebugConfig),
#endif

			// Modules
			typeof(JobHudConfig),
#if DEBUG
			typeof(JobHudDebugConfig),
#endif
			typeof(CooldownHudConfig),
#if DEBUG
			typeof(CooldownHudDebugConfig),
#endif
			typeof(ElementHiderConfig),
#if DEBUG
			typeof(ElementHiderDebugConfig),
#endif
			typeof(ActionBarConfig),
#if DEBUG
			typeof(ActionBarDebugConfig),
#endif
			typeof(PluginMenuConfig),
#if DEBUG
			typeof(PluginMenuDebugConfig),
#endif

			typeof(TanksColorConfig),
			typeof(HealersColorConfig),
			typeof(MeleeColorConfig),
			typeof(RangedColorConfig),
			typeof(CastersColorConfig),
			typeof(RolesColorConfig),
			typeof(MiscColorConfig),

			typeof(FontsConfig),
			typeof(HUDOptionsConfig),
			typeof(GridConfig),

			// Credits
			typeof(CreditsConfig),

			// Profiles
			typeof(ImportConfig)
		};

		private static readonly Dictionary<string, List<Type>> UnmergeableConfigTypesPerVersion = new()
		{
			["0.0.0.3"] = new()
		};

		#endregion
	}
}