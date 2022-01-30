using System;
using System.Collections.Generic;
using System.Linq;
using System.Numerics;
using Dalamud.Interface;
using DelvUI.Helpers;
using ImGuiNET;
using ImGuiScene;
using SezzUI.Config.Attributes;

namespace SezzUI.Config.Tree
{
	public delegate void ConfigObjectResetEventHandler(BaseNode sender);

	public class BaseNode : Node
	{
		public event ConfigObjectResetEventHandler? ConfigObjectResetEvent;

		private readonly Dictionary<Type, ConfigPageNode> _configPageNodesMap;

		public bool NeedsSave;
		public string? SelectedOptionName;

		private readonly List<Node> _extraNodes = new();
		private List<Node>? _nodes;

		public BaseNode()
		{
			_configPageNodesMap = new();
		}

		public void AddExtraSectionNode(SectionNode node)
		{
			_extraNodes.Add(node);
			_nodes = null;
		}

		public T? GetConfigObject<T>() where T : PluginConfigObject
		{
			ConfigPageNode? pageNode = GetConfigPageNode<T>();

			return pageNode != null ? (T) pageNode.ConfigObject : null;
		}

		public void RemoveConfigObject<T>() where T : PluginConfigObject
		{
			if (_configPageNodesMap.ContainsKey(typeof(T)))
			{
				_configPageNodesMap.Remove(typeof(T));
			}
		}

		public ConfigPageNode? GetConfigPageNode<T>() where T : PluginConfigObject
		{
			if (_configPageNodesMap.TryGetValue(typeof(T), out ConfigPageNode? node))
			{
				return node;
			}

			ConfigPageNode? configPageNode = GetOrAddConfig<T>();

			if (configPageNode != null && configPageNode.ConfigObject != null)
			{
				_configPageNodesMap.Add(typeof(T), configPageNode);

				return configPageNode;
			}

			return null;
		}

		public void SetConfigPageNode(ConfigPageNode configPageNode)
		{
			if (configPageNode.ConfigObject == null)
			{
				return;
			}

			_configPageNodesMap[configPageNode.ConfigObject.GetType()] = configPageNode;
		}

		public bool SetConfigObject(PluginConfigObject configObject)
		{
			if (_configPageNodesMap.TryGetValue(configObject.GetType(), out ConfigPageNode? configPageNode))
			{
				configPageNode.ConfigObject = configObject;
				return true;
			}

			return false;
		}

		private void PushStyles()
		{
			ImGui.PushStyleColor(ImGuiCol.Button, new Vector4(46f / 255f, 45f / 255f, 46f / 255f, 1f));
			ImGui.PushStyleColor(ImGuiCol.ButtonActive, new Vector4(0f / 255f, 174f / 255f, 255f / 255f, 1f));
			ImGui.PushStyleColor(ImGuiCol.ButtonHovered, new Vector4(0f / 255f, 139f / 255f, 204f / 255f, .7f));

			ImGui.PushStyleColor(ImGuiCol.Separator, new Vector4(48f / 255f, 68f / 255f, 77f / 255f, .8f));

			ImGui.PushStyleColor(ImGuiCol.ScrollbarBg, new Vector4(20f / 255f, 20f / 255f, 20f / 255f, .6f));
			ImGui.PushStyleColor(ImGuiCol.ScrollbarGrab, new Vector4(0f / 255f, 121f / 255f, 177f / 255f, .7f));
			ImGui.PushStyleColor(ImGuiCol.ScrollbarGrabActive, new Vector4(0f / 255f, 174f / 255f, 255f / 255f, 1f));
			ImGui.PushStyleColor(ImGuiCol.ScrollbarGrabHovered, new Vector4(0f / 255f, 139f / 255f, 204f / 255f, .7f));

			ImGui.PushStyleColor(ImGuiCol.Tab, new Vector4(46f / 255f, 45f / 255f, 46f / 255f, 1f));
			ImGui.PushStyleColor(ImGuiCol.TabActive, new Vector4(121f / 255f, 158f / 255f, 0f / 255f, .7f));
			ImGui.PushStyleColor(ImGuiCol.TabHovered, new Vector4(121f / 255f, 158f / 255f, 0f / 255f, .9f));
			ImGui.PushStyleColor(ImGuiCol.TabUnfocused, new Vector4(46f / 255f, 45f / 255f, 46f / 255f, .4f));

			ImGui.PushStyleColor(ImGuiCol.Border, new Vector4(54f / 255f, 75f / 255f, 85f / 255f, 1f));
			ImGui.PushStyleColor(ImGuiCol.CheckMark, new Vector4(0f / 255f, 174f / 255f, 255f / 255f, 1f));

			ImGui.PushStyleColor(ImGuiCol.TableBorderStrong, new Vector4(0f / 255f, 101f / 255f, 148f / 255f, 1f));
			ImGui.PushStyleColor(ImGuiCol.TableBorderLight, new Vector4(48f / 255f, 68f / 255f, 77f / 255f, .8f));
			ImGui.PushStyleColor(ImGuiCol.TableHeaderBg, new Vector4(48f / 255f, 68f / 255f, 77f / 255f, .5f));

			ImGui.PushStyleVar(ImGuiStyleVar.ScrollbarRounding, 0); // Scrollbar Radius
			ImGui.PushStyleVar(ImGuiStyleVar.TabRounding, 0); // Tabs Radius Radius
			ImGui.PushStyleVar(ImGuiStyleVar.FrameRounding, 0); // Intractable Elements Radius
			ImGui.PushStyleVar(ImGuiStyleVar.GrabRounding, 0); // Gradable Elements Radius
			ImGui.PushStyleVar(ImGuiStyleVar.PopupRounding, 0); // Popup Radius
			ImGui.PushStyleVar(ImGuiStyleVar.ScrollbarSize, 16); // Popup Radius
		}

		private void PopStyles()
		{
			ImGui.PopStyleColor(17);
			ImGui.PopStyleVar(6);
		}

		private void CreateNodesIfNeeded()
		{
			if (_nodes != null)
			{
				return;
			}

			_nodes = new();
			_nodes.AddRange(_children);
			_nodes.AddRange(_extraNodes);
		}

		public void Draw(float alpha)
		{
			CreateNodesIfNeeded();
			if (_nodes == null)
			{
				return;
			}

			bool changed = false;
			bool didReset = false;

			PushStyles();

			ImGui.BeginGroup(); // Middle section
			{
				ImGui.BeginGroup(); // Left
				{
					// banner
					TextureWrap? banner = ConfigurationManager.Instance.BannerImage;
					if (banner != null)
					{
						ImGui.SetCursorPos(new(12, 10));
						ImGui.Image(banner.ImGuiHandle, new(banner.Width, banner.Height));
					}
					else
					{
						ImGui.SetCursorPos(new(30, 20));
						ImGui.Text($"SezzUI v{Plugin.Version}");
					}

					// section list
					ImGui.SetCursorPos(new(10, 56));
					ImGui.BeginChild("left pane", new(150, -ImGui.GetFrameHeightWithSpacing() - 15), true);

					// if no section is selected, select the first
					if (_nodes.Any() && _nodes.Find(o => o is SectionNode sectionNode && sectionNode.Selected) == null)
					{
						SectionNode? selectedSection = (SectionNode?) _nodes.Find(o => o is SectionNode sectionNode && sectionNode.Name == SelectedOptionName);
						if (selectedSection != null)
						{
							selectedSection.Selected = true;
							SelectedOptionName = selectedSection.Name;
						}
						else if (_nodes.Count > 0)
						{
							SectionNode node = (SectionNode) _nodes[0];
							node.Selected = true;
							SelectedOptionName = node.Name;
						}
					}

					foreach (SectionNode selectionNode in _nodes)
					{
						if (ImGui.Selectable(selectionNode.Name, selectionNode.Selected))
						{
							selectionNode.Selected = true;
							SelectedOptionName = selectionNode.Name;

							foreach (SectionNode otherNode in _nodes.FindAll(x => x != selectionNode))
							{
								otherNode.Selected = false;
							}
						}

						DrawExportResetContextMenu(selectionNode, selectionNode.Name);
					}

					ImGui.EndChild();
				}

				ImGui.EndGroup(); // Left

				didReset |= DrawResetModal();

				ImGui.SameLine();

				ImGui.BeginGroup(); // Right
				{
					foreach (SectionNode selectionNode in _nodes)
					{
						didReset |= selectionNode.Draw(ref changed, alpha);
					}
				}

				ImGui.EndGroup(); // Right
			}

			ImGui.EndGroup(); // Middle section

			ImGui.BeginGroup();
			{
				ImGuiStylePtr style = ImGui.GetStyle();
				Vector2 buttonSize = new(150f, 0);
				ImGui.SetCursorPosX(2);

				ImGui.PushStyleColor(ImGuiCol.Border, Vector4.Zero);
				ImGui.BeginChild("SezzUI_Settings_Buttons", new(ImGui.GetContentRegionAvail().X + style.FramePadding.X, 0), true, ImGuiWindowFlags.NoScrollWithMouse | ImGuiWindowFlags.NoScrollbar);
				ImGui.PopStyleColor();

				ImGui.PushStyleColor(ImGuiCol.Button, new Vector4(45f / 255f, 45f / 255f, 45f / 255f, alpha));
				ImGui.PushStyleVar(ImGuiStyleVar.FrameBorderSize, 1);

				// Button: Show/Hide HUD
				string hudVisibilityText = (ConfigurationManager.Instance.ShowHUD ? "Hide" : "Show") + " HUD";
				FontAwesomeIcon hudVisibilityIcon = ConfigurationManager.Instance.ShowHUD ? FontAwesomeIcon.EyeSlash : FontAwesomeIcon.Eye;

				if (ImGuiHelper.FontAwesomeIconButton(hudVisibilityText, hudVisibilityIcon, buttonSize))
				{
					ConfigurationManager.Instance.ShowHUD = !ConfigurationManager.Instance.ShowHUD;
				}

				// Button: Lock/Unlock HUD
				string hudLockText = (ConfigurationManager.Instance.LockHUD ? "Unlock" : "Lock") + " HUD";
				FontAwesomeIcon hudLockIcon = ConfigurationManager.Instance.LockHUD ? FontAwesomeIcon.ArrowsAlt : FontAwesomeIcon.Lock;

				ImGui.SameLine();
				ImGui.SetCursorPosX(ImGui.GetCursorPosX() + 2);
				if (ImGuiHelper.FontAwesomeIconButton(hudLockText, hudLockIcon, buttonSize))
				{
					ConfigurationManager.Instance.LockHUD = !ConfigurationManager.Instance.LockHUD;
				}

				// Button: Support
				Vector2 supportButtonPosition = new(ImGui.GetWindowWidth() - buttonSize.X - style.FramePadding.X, style.ItemSpacing.Y + style.FrameBorderSize + style.FramePadding.Y);
				ImGui.SameLine();
				ImGui.SetCursorPosX(supportButtonPosition.X);
				ImGui.PushStyleColor(ImGuiCol.Button, new Vector4(62f / 255f, 41f / 255f, 41f / 255f, alpha));
				ImGui.PushStyleColor(ImGuiCol.ButtonHovered, new Vector4(108f / 255f, 41f / 255f, 41f / 255f, alpha));
				ImGui.PushStyleColor(ImGuiCol.ButtonActive, new Vector4(142f / 255f, 41f / 255f, 41f / 255f, alpha));

				if (ImGuiHelper.FontAwesomeIconButton("Support Me", FontAwesomeIcon.KissWinkHeart, buttonSize))
				{
					Utils.OpenUrl("https://ko-fi.com/sezzat");
				}

				ImGui.PopStyleColor(3);

				// Done
				ImGui.PopStyleVar();
				ImGui.PopStyleColor();
				ImGui.EndChild();
			}
			ImGui.EndGroup();

			// close button
			ImGui.SetCursorPos(new(ImGui.GetWindowWidth() - 28, 5));
			ImGui.PushFont(UiBuilder.IconFont);
			ImGui.PushStyleColor(ImGuiCol.Text, new Vector4(0f / 255f, 174f / 255f, 255f / 255f, 1 * alpha));
			if (ImGui.Button(FontAwesomeIcon.Times.ToIconString(), new(22, 22)))
			{
				ConfigurationManager.Instance.CloseConfigWindow();
			}

			ImGui.PopStyleColor();
			ImGui.PopFont();

			PopStyles();

			if (didReset)
			{
				ConfigObjectResetEvent?.Invoke(this);
			}

			NeedsSave |= changed | didReset;
		}

		public ConfigPageNode? GetOrAddConfig<T>() where T : PluginConfigObject
		{
			object[] attributes = typeof(T).GetCustomAttributes(true);

			foreach (object attribute in attributes)
			{
				if (attribute is SectionAttribute sectionAttribute)
				{
					foreach (SectionNode sectionNode in _children)
					{
						if (sectionNode.Name == sectionAttribute.SectionName)
						{
							return sectionNode.GetOrAddConfig<T>();
						}
					}

					SectionNode newNode = new();
					newNode.Name = sectionAttribute.SectionName;
					newNode.ForceAllowExport = sectionAttribute.ForceAllowExport;
					_children.Add(newNode);

					return newNode.GetOrAddConfig<T>();
				}
			}

			Type type = typeof(T);
			throw new ArgumentException("The provided configuration object does not specify a section: " + type.Name);
		}
	}
}