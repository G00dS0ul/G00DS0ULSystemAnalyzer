using System;
using System.IO;
using System.Linq;
using System.Security.Cryptography;
using System.Threading.Tasks;
using System.Collections.Concurrent;
using System.Collections.Generic;
using GSSystemAnalyzer.Interfaces;
using GSSystemAnalyzer.Models;

namespace GSSystemAnalyzer.Services
{
	public class DuplicateFileDetector : IDuplicateFileDetector
	{
		private readonly ISettingService _settings;
		private readonly IDiskScannerEngine _scanner;

		public DuplicateFileDetector(ISettingService settings, IDiskScannerEngine scanner)
		{
			_settings = settings;
			_scanner = scanner;
		}
		/// Scans a root directory and returns a dictionary of duplicate files grouped by their SHA256 hash.
		public async Task<List<DuplicateGroup>> FindDuplicatesAsync(string rootPath, Guid scanId)
		{
			try
			{
				var cancellationToken = _scanner.GetScanToken(scanId);

				// 1. Safely gather all files, ignoring protected system folders (This fixes the "Ghost Reading" crash)
				var allFiles = SafeEnumerateFiles(rootPath, cancellationToken);
				var filesToHash = allFiles
					.Where(f => f.Length > 0)
					.GroupBy(f => f.Length)
					.Where(group => group.Count() > 1)
					.SelectMany(group => group)
					.ToList();

				// --- PASS 2: The Parallel Hash ---
				// Thread-safe dictionary to handle multiple CPU cores writing at the same time
				var hashedFiles = new ConcurrentDictionary<string, ConcurrentBag<string>>();

				var parallelOptions = new ParallelOptions
				{
					CancellationToken = cancellationToken,
					MaxDegreeOfParallelism = Math.Max(1, Environment.ProcessorCount / 2)
				};

				// Process the remaining files across all available CPU cores
				await Parallel.ForEachAsync(filesToHash, parallelOptions, async (file, ct) =>
				{
					try
					{
						using var sha256 = SHA256.Create();
						using var stream = File.OpenRead(file.FullName);

						byte[] hashBytes = await sha256.ComputeHashAsync(stream, ct);
						string hashString = BitConverter.ToString(hashBytes).Replace("-", "");

						hashedFiles.AddOrUpdate(
							hashString,
							new ConcurrentBag<string> { file.FullName },
							(key, existingBag) =>
							{
								existingBag.Add(file.FullName);
								return existingBag;
							});
					}
					catch (IOException)
					{
						// Skip files that are currently locked or open in another program
					}
					catch (UnauthorizedAccessException)
					{
						// Skip files we don't have permission to read
					}
				});

				// 3. Final Cleanup: Convert back to standard collections and filter out unique files
				return hashedFiles
					.Where(kvp => kvp.Value.Count > 1)
					.Select(kvp =>
					{

						var paths = kvp.Value.ToList();
						long sizeBytes = new FileInfo(paths.First()).Length;

						return new DuplicateGroup
						{
							FileSizeBytes = sizeBytes,
							FileHash = kvp.Key,
							FilePaths = paths
						};
					})
					.OrderByDescending(d => d.WastedBytes)
					.ToList();
			}
			finally
			{
				_scanner.EndScanSession(scanId);
			}
		}

		/// Helper method to traverse directories without crashing on UnauthorizedAccessException.
		private IEnumerable<FileInfo> SafeEnumerateFiles(string rootPath, CancellationToken token)
		{
			var config = _settings.Current.Scan;

			var rootDir = new DirectoryInfo(rootPath);

			var options = new EnumerationOptions
			{
				IgnoreInaccessible = true,
				RecurseSubdirectories = true,
				ReturnSpecialDirectories = false,
				AttributesToSkip = 0
			};

			if (config.SkipHiddenFiles) options.AttributesToSkip |= FileAttributes.Hidden;
			if (config.SkipSystemFiles) options.AttributesToSkip |= FileAttributes.System;

			var files = Enumerable.Empty<FileInfo>();

			try
			{
				files = rootDir.EnumerateFiles("*.*", options);
			}
			catch (UnauthorizedAccessException)
			{
				/* Ignore protected folders */
			}
			catch (DirectoryNotFoundException)
			{
				/* Ignore deleted folders */
			}
			catch (Exception)
			{

			}

			foreach (var file in files)
			{
				token.ThrowIfCancellationRequested();

				string appDataSegment = $"{Path.DirectorySeparatorChar}AppData{Path.DirectorySeparatorChar}";
				if (!rootPath.Contains(appDataSegment, StringComparison.OrdinalIgnoreCase)
					&& file.FullName.Contains(appDataSegment, StringComparison.OrdinalIgnoreCase))
				{
					continue;
				}

				yield return file;
			}

		}
	}
}
