using System;
using System.Numerics;
using Dalamud.Bindings.ImGui;
using SezzUI.Configuration;
using SezzUI.Configuration.Attributes;
using SezzUI.Modules;

namespace SezzUI.Helper;

public class TooltipsHelper : IPluginDisposable
{
	private static readonly float MaxWidth = 300;
	private static readonly float Margin = 5;

	private TooltipsConfig _config => Singletons.Get<ConfigurationManager>().GetConfigObject<TooltipsConfig>();

	private string? _currentTooltipText;
	private Vector2 _textSize;
	private string? _currentTooltipTitle;
	private Vector2 _titleSize;
	private string? _previousRawText;

	private Vector2 _position;
	private Vector2 _size;

	private bool _dataIsValid;

	public void ShowTooltipOnCursor(string text, string? title = null)
	{
		ShowTooltip(text, ImGui.GetMousePos(), title);
	}

	public void ShowTooltip(string text, Vector2 position, string? title = null)
	{
		if (string.IsNullOrEmpty(text))
		{
			return;
		}

		// remove styling tags from text
		if (_previousRawText != text)
		{
			_currentTooltipText = text;
			_previousRawText = text;
		}

		// calculate title size
		_titleSize = Vector2.Zero;
		if (title != null)
		{
			_currentTooltipTitle = title;

			using (MediaManager.PushFont(PluginFontSize.Large))
			{
				_titleSize = ImGui.CalcTextSize(_currentTooltipTitle, false, MaxWidth);
				_titleSize.Y += Margin;
			}
		}

		// calculate text size
		using (MediaManager.PushFont())
		{
			_textSize = ImGui.CalcTextSize(_currentTooltipText, false, MaxWidth);
		}

		_size = new(Math.Max(_titleSize.X, _textSize.X) + Margin * 2, _titleSize.Y + _textSize.Y + Margin * 2);

		// position tooltip using the given coordinates as bottom center
		position.X = position.X - _size.X / 2f;
		position.Y = position.Y - _size.Y;

		// correct tooltips off screen
		_position = ConstrainPosition(position, _size);

		_dataIsValid = true;
	}

	public void RemoveTooltip()
	{
		_dataIsValid = false;
	}

	public void Draw()
	{
		if (!_dataIsValid || Singletons.Get<ConfigurationManager>().ShowingModalWindow)
		{
			return;
		}

		// bg
		ImGuiWindowFlags windowFlags = ImGuiWindowFlags.NoTitleBar | ImGuiWindowFlags.NoMove | ImGuiWindowFlags.NoDecoration | ImGuiWindowFlags.NoBackground | ImGuiWindowFlags.NoInputs | ImGuiWindowFlags.NoSavedSettings | ImGuiWindowFlags.NoFocusOnAppearing;

		// imgui clips the left and right borders inside windows for some reason
		// we make the window bigger so the actual drawable size is the expected one
		Vector2 windowMargin = new(4, 0);
		Vector2 windowPos = _position - windowMargin;

		ImGui.SetNextWindowPos(windowPos, ImGuiCond.Always);
		ImGui.SetNextWindowSize(_size + windowMargin * 2);

		ImGui.PushStyleVar(ImGuiStyleVar.WindowRounding, 0);
		ImGui.Begin("SezzUI_Tooltip", windowFlags);
		ImDrawListPtr drawList = ImGui.GetWindowDrawList();

		drawList.AddRectFilled(_position, _position + _size, _config.BackgroundColor.Base);

		if (_config.BorderConfig.Enabled)
		{
			drawList.AddRect(_position, _position + _size, _config.BorderConfig.Color.Base, 0, ImDrawFlags.None, _config.BorderConfig.Thickness);
		}

		if (_currentTooltipTitle != null)
		{
			// title
			using (MediaManager.PushFont(PluginFontSize.Large))
			{
				Vector2 cursorPos = new(windowMargin.X + _size.X / 2f - _titleSize.X / 2f, Margin);
				ImGui.SetCursorPos(cursorPos);
				ImGui.PushTextWrapPos(cursorPos.X + _titleSize.X);
				ImGui.TextColored(_config.TitleColor.Vector, _currentTooltipTitle);
				ImGui.PopTextWrapPos();
			}

			// text
			using (MediaManager.PushFont())
			{
				Vector2 cursorPos = new(windowMargin.X + _size.X / 2f - _textSize.X / 2f, Margin + _titleSize.Y);
				ImGui.SetCursorPos(cursorPos);
				ImGui.PushTextWrapPos(cursorPos.X + _textSize.X);
				ImGui.TextColored(_config.TextColor.Vector, _currentTooltipText);
				ImGui.PopTextWrapPos();
			}
		}
		else
		{
			// text
			using (MediaManager.PushFont())
			{
				Vector2 cursorPos = windowMargin + new Vector2(Margin, Margin);
				float textWidth = _size.X - Margin * 2;

				ImGui.SetCursorPos(cursorPos);
				ImGui.PushTextWrapPos(cursorPos.X + textWidth);
				ImGui.TextColored(_config.TextColor.Vector, _currentTooltipText);
				ImGui.PopTextWrapPos();
			}
		}

		ImGui.End();
		ImGui.PopStyleVar();

		RemoveTooltip();
	}

	private Vector2 ConstrainPosition(Vector2 position, Vector2 size)
	{
		Vector2 screenSize = ImGui.GetWindowViewport().Size;

		if (position.X < 0)
		{
			position.X = Margin;
		}
		else if (position.X + size.X > screenSize.X)
		{
			position.X = screenSize.X - size.X - Margin;
		}

		if (position.Y < 0)
		{
			position.Y = Margin;
		}

		return position;
	}

	bool IPluginDisposable.IsDisposed { get; set; } = false;

	public void Dispose()
	{
		(this as IPluginDisposable).IsDisposed = true;
	}
}

[Section("Misc")]
[SubSection("Tooltips", 0)]
public class TooltipsConfig : PluginConfigObject
{
	public new static TooltipsConfig DefaultConfig() => new();

	[ColorEdit4("Background Color")]
	[Order(15)]
	public PluginConfigColor BackgroundColor = new(new(19f / 255f, 19f / 255f, 19f / 255f, 190f / 250f));

	[ColorEdit4("Title Color")]
	[Order(25)]
	public PluginConfigColor TitleColor = new(new(255f / 255f, 210f / 255f, 31f / 255f, 100f / 100f));

	[ColorEdit4("Text Color")]
	[Order(35)]
	public PluginConfigColor TextColor = new(new(255f / 255f, 255f / 255f, 255f / 255f, 100f / 100f));

	[NestedConfig("Border", 40, separator = false, spacing = true, collapsingHeader = false)]
	public TooltipBorderConfig BorderConfig = new();
}

[Exportable(false)]
public class TooltipBorderConfig : PluginConfigObject
{
	[ColorEdit4("Color")]
	[Order(5)]
	public PluginConfigColor Color = new(new(10f / 255f, 10f / 255f, 10f / 255f, 160f / 255f));

	[DragInt("Thickness", min = 1, max = 100)]
	[Order(10)]
	public int Thickness = 4;

	public TooltipBorderConfig()
	{
	}

	public TooltipBorderConfig(PluginConfigColor color, int thickness)
	{
		Color = color;
		Thickness = thickness;
	}
}