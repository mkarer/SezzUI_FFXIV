using System.Collections.Generic;
using SezzUI.Helper;

namespace SezzUI.Configuration.Tree
{
	public abstract class Node
	{
		protected List<Node> _children = new();

		public void Add(Node node)
		{
			_children.Add(node);
		}

		#region reset

		protected Node? _nodeToReset;
		protected string? _nodeToResetName;

		protected void DrawExportResetContextMenu(Node node, string name)
		{
			if (_nodeToReset != null)
			{
				return;
			}

			bool allowExport = node.AllowExport();
			bool allowReset = node.AllowReset();
			if (!allowExport && !allowReset)
			{
				return;
			}

			_nodeToReset = ImGuiHelper.DrawExportResetContextMenu(node, allowExport, allowReset);
			_nodeToResetName = name;
		}

		protected virtual bool AllowExport()
		{
			foreach (Node child in _children)
			{
				if (child.AllowExport())
				{
					return true;
				}
			}

			return false;
		}

		protected virtual bool AllowShare()
		{
			foreach (Node child in _children)
			{
				if (child.AllowShare())
				{
					return true;
				}
			}

			return false;
		}

		protected virtual bool AllowReset()
		{
			foreach (Node child in _children)
			{
				if (child.AllowReset())
				{
					return true;
				}
			}

			return false;
		}

		protected bool DrawResetModal()
		{
			if (_nodeToReset == null || _nodeToResetName == null)
			{
				return false;
			}

			string[] lines = {"Are you sure you want to reset \"" + _nodeToResetName + "\"?"};
			(bool didReset, bool didClose) = ImGuiHelper.DrawConfirmationModal("Reset?", lines);

			if (didReset)
			{
				_nodeToReset.Reset();
				_nodeToReset = null;
			}
			else if (didClose)
			{
				_nodeToReset = null;
			}

			return didReset;
		}


		public virtual void Reset()
		{
			foreach (Node child in _children)
			{
				child.Reset();
			}
		}

		#endregion

		#region save and load

		public virtual void Save(string path)
		{
			foreach (Node child in _children)
			{
				child.Save(path);
			}
		}

		public virtual void Load(string path, string currentVersion, string? previousVersion = null)
		{
			foreach (Node child in _children)
			{
				child.Load(path, currentVersion, previousVersion);
			}
		}

		#endregion

		#region export

		public virtual string? GetBase64String()
		{
			if (_children == null)
			{
				return "";
			}

			string base64String = "";

			foreach (Node child in _children)
			{
				string? childString = child.GetBase64String();

				if (childString != null && childString.Length > 0)
				{
					base64String += "|" + childString;
				}
			}

			return base64String;
		}

		#endregion
	}
}