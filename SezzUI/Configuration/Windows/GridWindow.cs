using System.Numerics;
using Dalamud.Interface.Windowing;
using ImGuiNET;
using SezzUI.Configuration.Tree;
using SezzUI.Helper;
using SezzUI.Interface.GeneralElements;

namespace SezzUI.Configuration.Windows
{
	public class GridWindow : Window
	{
		public GridWindow(string name) : base(name)
		{
			Flags = ImGuiWindowFlags.NoScrollbar | ImGuiWindowFlags.NoResize | ImGuiWindowFlags.NoScrollWithMouse;
			Size = new Vector2(340, 300);
		}

		public override void OnClose()
		{
			Singletons.Get<ConfigurationManager>().LockHUD = true;
		}

		public override void PreDraw()
		{
			ImGui.PushStyleColor(ImGuiCol.WindowBg, new Vector4(10f / 255f, 10f / 255f, 10f / 255f, 0.95f));
			ImGui.SetNextWindowFocus();
		}

		public override void Draw()
		{
			ConfigurationManager configurationManager = Singletons.Get<ConfigurationManager>();
			ConfigPageNode? node = configurationManager.GetConfigPageNode<GridConfig>();
			if (node == null)
			{
				return;
			}

			ImGui.PushItemWidth(150);
			bool changed = false;
			node.Draw(ref changed);

			ImGui.SetCursorPos(new(8, 260));
			if (ImGui.Button("Lock HUD", new(ImGui.GetWindowContentRegionMax().X, 30)))
			{
				configurationManager.LockHUD = true;
			}
		}

		public override void PostDraw()
		{
			ImGui.PopStyleColor();
		}
	}
}