using System;
using System.IO;
using Dalamud.Utility;

namespace SezzUI.Helpers
{
	public static class FileSystemHelper
	{
		internal static PluginLogger Logger;

		static FileSystemHelper()
		{
			Logger = new("FileSystemHelper");
		}

		public static bool ValidatePath(string? path, out string validatedPath)
		{
			validatedPath = "";
			if (path.IsNullOrEmpty())
			{
				return false;
			}

			try
			{
				string fullPath = Path.GetFullPath(path!);
				if (fullPath != "" && Directory.Exists(fullPath))
				{
					validatedPath = fullPath.TrimEnd(Path.DirectorySeparatorChar) + Path.DirectorySeparatorChar;
					return true;
				}
			}
			catch (Exception ex)
			{
				Logger.Warning(ex, "ValidatePath", $"Failed validating path: {path} Error: {ex}");
			}

			return false;
		}

		public static bool ValidateFile(string? path, string? file, out string validatedFileName)
		{
			validatedFileName = "";
			if (!ValidatePath(path, out string validatedPath) || file.IsNullOrEmpty())
			{
				return false;
			}

			try
			{
				string filePath = validatedPath + file!.TrimStart(Path.DirectorySeparatorChar);
				if (File.Exists(filePath) && !File.GetAttributes(filePath).HasFlag(FileAttributes.Directory))
				{
					validatedFileName = filePath;
					return true;
				}
			}
			catch (Exception ex)
			{
				Logger.Warning(ex, "ValidateFile", $"Failed validating file: {file} Error: {ex}");
			}

			return false;
		}
	}
}