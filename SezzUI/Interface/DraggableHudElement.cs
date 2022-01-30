using System;
using System.Numerics;
using DelvUI.Helpers;
using ImGuiNET;
using SezzUI.Config;
using SezzUI.Enums;
using DrawHelper = SezzUI.Helpers.DrawHelper;

namespace SezzUI.Interface
{
	public class DraggableHudElement
	{
		/// <summary>
		///     Configuration reference.
		/// </summary>
		public AnchorablePluginConfigObject Config => _config;

		protected AnchorablePluginConfigObject _config;

		/// <summary>
		///     Unique identifier for ImGui.
		/// </summary>
		public readonly string Identifier;

		/// <summary>
		///     Displayed name when dragging is enabled.
		/// </summary>
		public virtual string? DisplayName { get; }

		public DraggableHudElement(AnchorablePluginConfigObject config, string? displayName = null, string? id = null)
		{
			_config = config;
			Identifier = id ?? Guid.NewGuid().ToString();
			DisplayName = displayName;
		}

		#region Size

		public delegate void SizeChangedDelegate(Vector2 size);

		public event SizeChangedDelegate? SizeChanged;

		public Vector2 Size
		{
			get => GetSize();
			set => SetSize(value);
		}

		protected virtual Vector2 GetSize() =>
			// Override
			_config.Size;

		protected virtual void SetSize(Vector2 size)
		{
			// Override
			_config.Size = size;
			SizeChanged?.Invoke(size);
		}

		#endregion

		#region Position

		public delegate void PositionChangedDelegate(Vector2 position);

		public event PositionChangedDelegate? PositionChanged;

		public Vector2 Position
		{
			get => GetPosition();
			set => SetPosition(value);
		}

		protected virtual Vector2 GetPosition() =>
			// Override
			_config.Position;

		protected virtual void SetPosition(Vector2 position)
		{
			// Override
			_config.Position = position;
			PositionChanged?.Invoke(position);
		}

		#endregion

		#region Anchor

		public delegate void AnchorChangedDelegate(DrawAnchor anchor);

		public event AnchorChangedDelegate? AnchorChanged;

		public DrawAnchor Anchor
		{
			get => GetAnchor();
			set => SetAnchor(value);
		}

		protected virtual DrawAnchor GetAnchor() =>
			// Override
			_config.Anchor;

		protected virtual void SetAnchor(DrawAnchor anchor)
		{
			// Override
			_config.Anchor = anchor;
			AnchorChanged?.Invoke(anchor);
		}

		#endregion

		#region Dragging

		public delegate void ElementSelectedDelegate(DraggableHudElement element);

		public event ElementSelectedDelegate? ElementSelected;
		public bool Selected = false;

		private readonly Vector2 _windowPadding = new(4f, 4f); // Additional padding to work around ImGui clipping the draggable area
		private bool _windowPositionSet;
		private Vector2 _lastWindowPos = Vector2.Zero;

		private bool _draggingEnabled;

		public bool DraggingEnabled
		{
			get => _draggingEnabled;
			set
			{
				_draggingEnabled = value;

				if (_draggingEnabled)
				{
					_windowPositionSet = false;
				}
			}
		}

		public bool CanTakeInputForDrag = false;
		public bool NeedsInputForDrag { get; private set; }

		public void DrawDraggableArea()
		{
			if (!DraggingEnabled)
			{
				return;
			}

			Vector2 windowSize = Size + _windowPadding * 2;
			Vector2 windowPosition = DrawHelper.GetAnchoredPosition(Size, Anchor) + Position - _windowPadding;

			ImGuiWindowFlags windowFlags = ImGuiWindowFlags.NoScrollbar | ImGuiWindowFlags.NoTitleBar | ImGuiWindowFlags.NoResize | ImGuiWindowFlags.NoBackground | ImGuiWindowFlags.NoDecoration | ImGuiWindowFlags.NoSavedSettings;

			ImGui.SetNextWindowSize(windowSize, ImGuiCond.Always);
			ImGui.PushStyleVar(ImGuiStyleVar.WindowPadding, Vector2.Zero);
			ImGui.PushStyleVar(ImGuiStyleVar.FrameRounding, 0);
			ImGui.PushStyleVar(ImGuiStyleVar.FrameBorderSize, 0);

			// Check input
			NeedsInputForDrag = CanTakeInputForDrag && CalculateNeedsInput(_lastWindowPos, windowSize);
			if (!NeedsInputForDrag)
			{
				windowFlags |= ImGuiWindowFlags.NoMove;
			}

			// Set initial position
			if (!_windowPositionSet)
			{
				ImGui.SetNextWindowPos(windowPosition);
				_windowPositionSet = true;
			}

			if (!ImGui.Begin(Identifier + "_DragArea", windowFlags))
			{
				ImGui.PopStyleVar(3);
				ImGui.End();
				return;
			}

			_lastWindowPos = ImGui.GetWindowPos();
			Position = DrawHelper.GetAnchoredImGuiPosition(_lastWindowPos + _windowPadding, Size, Anchor);

			// Check selection
			string tooltipText = "X: " + _config.Position.X + "    Y: " + _config.Position.Y;
			if (NeedsInputForDrag && ImGui.IsMouseHoveringRect(_lastWindowPos, _lastWindowPos + windowSize))
			{
				bool clicked = ImGui.IsMouseClicked(ImGuiMouseButton.Left) || ImGui.IsMouseDown(ImGuiMouseButton.Left);
				if (clicked && !Selected)
				{
					ElementSelected?.Invoke(this);
				}

				// Update tooltip
				TooltipsHelper.Instance.ShowTooltipOnCursor(tooltipText);
			}

			// Draw window
			Vector2 dragPosition = DrawHelper.GetAnchoredPosition(_lastWindowPos, windowSize, Size, DrawAnchor.Center);

			uint lineColor = Selected ? 0xffeeffff : 0x4dffffff;
			ImDrawListPtr drawList = ImGui.GetWindowDrawList();
			DrawHelper.DrawPlaceholder("", dragPosition, Size, 0xffffffff, lineColor, 0x7f000000, DrawHelper.PlaceholderLineStyle.Parallel, drawList);

			// Arrows
			if (Selected && DraggablesHelper.DrawArrows(_lastWindowPos, windowSize, tooltipText, out Vector2 movement))
			{
				Position += movement;
				_windowPositionSet = false;
			}

			// Done
			ImGui.PopStyleVar(3);
			ImGui.End();

			// Name
			uint textColor = Selected ? 0xffffffff : 0xeeffffff;
			uint textShadowColor = Selected ? 0xff000000 : 0xee000000;
			DrawHelper.DrawCenteredShadowText("MyriadProLightCond_16", DisplayName?.Length > 0 ? DisplayName : Identifier, dragPosition, Size, textColor, textShadowColor, drawList);
		}

		private bool CalculateNeedsInput(Vector2 pos, Vector2 size)
		{
			if (ImGui.IsMouseHoveringRect(pos, pos + size))
			{
				return true;
			}

			if (!Selected)
			{
				return false;
			}

			Vector2[] arrowsPos = DraggablesHelper.GetArrowPositions(Position, Size);

			foreach (Vector2 arrowPos in arrowsPos)
			{
				if (ImGui.IsMouseHoveringRect(arrowPos, arrowPos + DraggablesHelper.ArrowSize))
				{
					return true;
				}
			}

			return false;
		}

		#endregion
	}
}