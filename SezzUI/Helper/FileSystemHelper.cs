using System;
using System.IO;
using Dalamud.Utility;
using SezzUI.Logging;

namespace SezzUI.Helper;

public static class FileSystemHelper
{
	internal static PluginLogger Logger;

	static FileSystemHelper()
	{
		Logger = new("FileSystemHelper");
	}

	private static bool Validate(string? path, out string validatedPath, bool expectFile, bool expectDirectory)
	{
		validatedPath = "";
		if (!path.IsNullOrEmpty())
		{
			try
			{
				string fullPath = Path.GetFullPath(path!);
				if ((expectFile && File.Exists(fullPath)) || (expectDirectory && Directory.Exists(fullPath)))
				{
					validatedPath = fullPath;
					return true;
				}
			}
			catch (Exception ex)
			{
				Logger.Warning($"Failed to validate path: {path} (expectFile: {expectFile} expectDirectory: {expectDirectory})");
				Logger.Warning(ex);
			}
		}

		return false;
	}

	public static bool ValidatePath(string? path, out string validatedPath) => Validate(path, out validatedPath, false, true);
	public static bool ValidateFile(string? file, out string validatedFileName) => Validate(file, out validatedFileName, true, false);
}