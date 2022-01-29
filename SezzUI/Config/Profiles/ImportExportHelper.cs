using System;
using System.IO;
using System.IO.Compression;
using System.Text;
using Newtonsoft.Json;

namespace SezzUI.Config.Profiles
{
	public static class ImportExportHelper
	{
		public static string CompressAndBase64Encode(string jsonString)
		{
			using MemoryStream output = new();

			using (DeflateStream gzip = new(output, CompressionLevel.Optimal))
			{
				using StreamWriter writer = new(gzip, Encoding.UTF8);
				writer.Write(jsonString);
			}

			return Convert.ToBase64String(output.ToArray());
		}

		public static string Base64DecodeAndDecompress(string base64String)
		{
			byte[] base64EncodedBytes = Convert.FromBase64String(base64String);

			using MemoryStream inputStream = new(base64EncodedBytes);
			using DeflateStream gzip = new(inputStream, CompressionMode.Decompress);
			using StreamReader reader = new(gzip, Encoding.UTF8);
			string decodedString = reader.ReadToEnd();

			return decodedString;
		}

		public static string GenerateExportString(object obj)
		{
			JsonSerializerSettings settings = new()
			{
				TypeNameAssemblyFormatHandling = TypeNameAssemblyFormatHandling.Simple,
				TypeNameHandling = TypeNameHandling.Objects
			};

			string jsonString = JsonConvert.SerializeObject(obj, Formatting.Indented, settings);
			return CompressAndBase64Encode(jsonString);
		}
	}
}