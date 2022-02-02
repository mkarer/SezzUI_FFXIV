using System;
using System.IO;

namespace SezzUI.Helpers
{
	public static class FileSystemHelper
	{
		internal static PluginLogger Logger;

		static FileSystemHelper()
		{
			Logger = new("FileSystemHelper");
		}

		public static string ValidatePath(string path)
		{
			try
			{
				string fullPath = Path.GetFullPath(path);
				if (fullPath != "" && Directory.Exists(fullPath))
				{
					return fullPath.TrimEnd(Path.DirectorySeparatorChar) + Path.DirectorySeparatorChar;
				}
			}
			catch (Exception ex)
			{
				Logger.Error(ex, "ValidatePath", "Error: {ex}");
			}

			return "";
		}
	}
}