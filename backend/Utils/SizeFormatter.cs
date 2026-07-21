using System;

namespace GSSystemAnalyzer.Utils
{
	/// <summary>
	/// Single shared byte-size formatter. Replaces the per-service FormatSize
	/// implementations, one of which could walk past the suffix array for
	/// petabyte-scale inputs.
	/// </summary>
	public static class SizeFormatter
	{
		private static readonly string[] Suffixes = { "B", "KB", "MB", "GB", "TB", "PB" };

		public static string Format(long bytes)
		{
			if (bytes < 0) bytes = 0;

			decimal number = bytes;
			int counter = 0;

			while (Math.Round(number / 1024) >= 1 && counter < Suffixes.Length - 1)
			{
				number /= 1024;
				counter++;
			}

			return string.Format("{0:n1} {1}", number, Suffixes[counter]);
		}
	}
}
