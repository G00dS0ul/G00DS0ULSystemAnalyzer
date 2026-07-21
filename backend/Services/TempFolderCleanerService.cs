using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices;
using System.Threading;
using System.Threading.Tasks;
using GSSystemAnalyzer.Interfaces;
using GSSystemAnalyzer.Models;
using Microsoft.Extensions.Logging;

namespace GSSystemAnalyzer.Services;

public class TempFolderCleanerService : ITempFolderCleanerService
{
	private readonly INukeProtocolService _nukeService;
	private readonly ILogger<TempFolderCleanerService> _logger;

	private readonly List<string>? _tempPathsOverride;

	public TempFolderCleanerService(INukeProtocolService nukeService, ILogger<TempFolderCleanerService> logger, IEnumerable<string>? tempPathsOverride = null)
	{
		_nukeService = nukeService;
		_logger = logger;

		// The built-in DI container resolves IEnumerable<string> to an EMPTY collection
		// (never null), so at runtime this arrives as [] and must be treated as "no override".
		var overrides = tempPathsOverride?
			.Where(p => !string.IsNullOrWhiteSpace(p))
			.ToList();
		_tempPathsOverride = overrides is { Count: > 0 } ? overrides : null;
	}

	// Static so unit tests can assert the resolved list directly, and consumers can validate paths.
	public static List<CleanTarget> ResolveCleanTargets()
	{
		var raw = new List<CleanTarget>();

		// Helper: build a path from parts, skip if any part is null/empty.
		void Add(string label, CleanCategory cat, params string[] parts)
		{
			if (parts.Any(string.IsNullOrWhiteSpace)) return;
			raw.Add(new CleanTarget(Path.Combine(parts), label, cat));
		}

		if (RuntimeInformation.IsOSPlatform(OSPlatform.Windows))
		{
			var local = Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData);
			var roaming = Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData);
			var profile = Environment.GetFolderPath(Environment.SpecialFolder.UserProfile);
			var winDir = Environment.GetEnvironmentVariable("SystemRoot") ?? @"C:\Windows";

			// ---- Temp ----
			// GetTempPath() cascades TMP -> TEMP -> USERPROFILE -> C:\Windows\Temp,
			// so it effectively never returns empty (fixes the empty-list bug).
			raw.Add(new CleanTarget(Path.GetTempPath(), "User temp", CleanCategory.Temp));
			Add("User temp (Local)", CleanCategory.Temp, local, "Temp");
			Add("System temp", CleanCategory.Temp, winDir, "Temp");
			Add("OneDrive temp", CleanCategory.Temp, profile, "OneDriveTemp");

			// ---- Developer / package-manager caches (safe: regenerated on demand) ----
			Add("npm cache", CleanCategory.Cache, local, "npm-cache");
			Add("npm cache", CleanCategory.Cache, roaming, "npm-cache");
			Add("Yarn cache", CleanCategory.Cache, local, "Yarn", "Cache");
			Add("pip cache", CleanCategory.Cache, local, "pip", "Cache");
			Add("NuGet HTTP cache", CleanCategory.Cache, local, "NuGet", "v3-cache"); // NOT global packages
			Add("Gradle cache", CleanCategory.Cache, profile, ".gradle", "caches");

			// ---- Browser caches (locked while browser runs -> skipped safely) ----
			Add("Chrome cache", CleanCategory.Cache, local, "Google", "Chrome", "User Data", "Default", "Cache");
			Add("Edge cache", CleanCategory.Cache, local, "Microsoft", "Edge", "User Data", "Default", "Cache");
			Add("Windows INetCache", CleanCategory.Cache, local, "Microsoft", "Windows", "INetCache");

			// Firefox keeps one cache2 folder per profile — expand dynamically.
			var ffProfiles = Path.Combine(local, "Mozilla", "Firefox", "Profiles");
			if (Directory.Exists(ffProfiles))
				foreach (var p in Directory.GetDirectories(ffProfiles))
					Add($"Firefox cache ({Path.GetFileName(p)})", CleanCategory.Cache, p, "cache2");
		}
		else
		{
			// Linux / macOS
			var home = Environment.GetFolderPath(Environment.SpecialFolder.UserProfile);
			raw.Add(new CleanTarget(Path.GetTempPath(), "User temp", CleanCategory.Temp));
			Add("XDG cache", CleanCategory.Cache, home, ".cache");
			Add("npm cache", CleanCategory.Cache, home, ".npm", "_cacache");
			raw.Add(new CleanTarget("/tmp", "System temp", CleanCategory.Temp));
			raw.Add(new CleanTarget("/var/tmp", "System temp", CleanCategory.Temp));
		}

		var comparer = RuntimeInformation.IsOSPlatform(OSPlatform.Windows)
			? StringComparer.OrdinalIgnoreCase : StringComparer.Ordinal;

		// Normalize -> keep existing only -> de-dupe by path (first label wins).
		var seen = new HashSet<string>(comparer);
		var result = new List<CleanTarget>();
		foreach (var t in raw)
		{
			string full;
			try { full = Path.GetFullPath(t.Path); } catch { continue; }
			if (!seen.Add(full)) continue;
			if (!Directory.Exists(full)) continue;
			result.Add(t with { Path = full });
		}
		return result;
	}

	/// <summary>Back-compat shim: existing whitelist + unit tests keep working unchanged.</summary>
	public static List<string> ResolveTempPaths() =>
		ResolveCleanTargets().Select(t => t.Path).ToList();

	public async Task<TempPreviewResponse> PreviewAsync(CancellationToken cancellationToken = default)
	{
		return await Task.Run(() =>
		{
			var response = new TempPreviewResponse();

			// When _tempPathsOverride is set (tests), fall back to string-only mode
			// with generic labels. Otherwise, use full typed discovery.
			List<CleanTarget> targets;
			if (_tempPathsOverride != null)
			{
				targets = _tempPathsOverride
					.Where(p => !string.IsNullOrWhiteSpace(p) && Directory.Exists(p))
					.Select(p => new CleanTarget(Path.GetFullPath(p), Path.GetFileName(p.TrimEnd(Path.DirectorySeparatorChar, Path.AltDirectorySeparatorChar)), CleanCategory.Temp))
					.ToList();
			}
			else
			{
				targets = ResolveCleanTargets();
			}

			_logger.LogInformation("Cleaner resolved {Count} locations: {Targets}",
				targets.Count, string.Join(" | ", targets.Select(t => $"{t.Label}={t.Path}")));

			var options = new EnumerationOptions
			{
				IgnoreInaccessible = true,
				RecurseSubdirectories = true,
				ReturnSpecialDirectories = false,
				AttributesToSkip = FileAttributes.ReparsePoint
			};

			foreach (var target in targets)
			{
				cancellationToken.ThrowIfCancellationRequested();

				long sizeBytes = 0;
				int fileCount = 0;

				try
				{
					foreach (var file in new DirectoryInfo(target.Path).EnumerateFiles("*", options))
					{
						cancellationToken.ThrowIfCancellationRequested();
						try
						{
							sizeBytes += file.Length;
							fileCount++;
						}
						catch
						{
							// Locked or permission-denied on individual file — skip silently.
						}
					}
				}
				catch (Exception ex)
				{
					// Entire directory unreadable (permission denied, etc.) — omit it.
					_logger.LogWarning(ex, "Skipping unreadable location {Path}", target.Path);
					continue;
				}

				response.Locations.Add(new TempLocationPreview
				{
					Path = target.Path,
					Label = target.Label,
					Category = target.Category.ToString(),
					SizeBytes = sizeBytes,
					SizeFormatted = FormatSize(sizeBytes),
					FileCount = fileCount
				});

				response.TotalBytes += sizeBytes;
			}

			response.Locations = response.Locations.OrderByDescending(l => l.SizeBytes).ToList();
			response.TotalFormatted = FormatSize(response.TotalBytes);
			return response;
		}, cancellationToken);
	}

	public async Task<TempCleanResult> CleanAsync(List<string> paths, CancellationToken cancellationToken = default)
	{
		var knownPaths = _tempPathsOverride ?? ResolveTempPaths();
		var comparer = RuntimeInformation.IsOSPlatform(OSPlatform.Windows)
			? StringComparer.OrdinalIgnoreCase
			: StringComparer.Ordinal;
		var knownSet = new HashSet<string>(
			knownPaths.Select(NormalizePath), comparer);

		foreach (var p in paths)
		{
			var normalized = NormalizePath(p);
			if (!knownSet.Contains(normalized))
				throw new UnauthorizedAccessException(
					$"Path '{p}' is not a recognised temp directory. Only known temp locations may be cleaned.");
		}

		int totalDeleted = 0;
		long totalFreed = 0;
		int totalSkipped = 0;

		var options = new EnumerationOptions
		{
			IgnoreInaccessible = true,
			RecurseSubdirectories = true,
			ReturnSpecialDirectories = false,
			AttributesToSkip = FileAttributes.ReparsePoint
		};

		foreach (var p in paths)
		{
			cancellationToken.ThrowIfCancellationRequested();

			var tempDir = NormalizePath(p);
			if (!Directory.Exists(tempDir))
				continue;

			// Collect all file paths inside this temp directory.
			var filePaths = new List<string>();
			try
			{
				foreach (var file in new DirectoryInfo(tempDir).EnumerateFiles("*", options))
				{
					cancellationToken.ThrowIfCancellationRequested();
					filePaths.Add(file.FullName);
				}
			}
			catch (Exception ex)
			{
				_logger.LogWarning(ex, "Failed to enumerate files in temp dir: {Path}", tempDir);
			}

			if (filePaths.Count == 0)
				continue;

			// Delegate to the Nuke service: preview (required for plan token) → obliterate.
			var preview = await _nukeService.PreviewNukeAsync(filePaths, cancellationToken);
			var nukeResult = await _nukeService.ObliterateNodeAsync(filePaths, preview.PlanToken, useRecycleBin: false, cancellationToken);

			totalDeleted += nukeResult.DeletedFiles;
			totalFreed += nukeResult.FreedBytes;
			totalSkipped += nukeResult.SkippedFiles;

			// Clean up empty subdirectories left behind (bottom-up).
			// The temp directory itself is NEVER deleted.
			CleanEmptySubdirectories(tempDir);
		}

		return new TempCleanResult
		{
			DeletedFiles = totalDeleted,
			FreedBytes = totalFreed,
			FreedFormatted = FormatSize(totalFreed),
			SkippedFiles = totalSkipped
		};
	}

	private static string NormalizePath(string path)
	{
		var full = Path.GetFullPath(path);
		return full.TrimEnd(Path.DirectorySeparatorChar, Path.AltDirectorySeparatorChar);
	}

	private void CleanEmptySubdirectories(string tempDir)
	{
		var normalizedRoot = NormalizePath(tempDir);

		try
		{
			// Use a non-recursive first pass to collect only real (non-reparse) subdirectories,
			// then recurse manually. This avoids following symlinks/junctions.
			var options = new EnumerationOptions
			{
				IgnoreInaccessible = true,
				RecurseSubdirectories = true,
				ReturnSpecialDirectories = false,
				AttributesToSkip = FileAttributes.ReparsePoint // skip symlinks & junctions
			};

			var subdirs = new DirectoryInfo(tempDir)
				.EnumerateDirectories("*", options)
				.Select(d => d.FullName)
				.OrderByDescending(d => d.Length) // deepest first
				.ToList();

			foreach (var dir in subdirs)
			{
				// Double-guard: never delete the root temp directory itself.
				if (string.Equals(NormalizePath(dir), normalizedRoot,
					RuntimeInformation.IsOSPlatform(OSPlatform.Windows)
						? StringComparison.OrdinalIgnoreCase
						: StringComparison.Ordinal))
					continue;

				try
				{
					if (Directory.Exists(dir) && !Directory.EnumerateFileSystemEntries(dir).Any())
						Directory.Delete(dir);
				}
				catch
				{
					// Locked or permission-denied — skip silently.
				}
			}
		}
		catch (Exception ex)
		{
			_logger.LogDebug(ex, "Could not clean empty subdirectories in {TempDir}", tempDir);
		}
	}

	private static string FormatSize(long bytes)
	{
		string[] suffixes = { "B", "KB", "MB", "GB", "TB" };
		int counter = 0;
		decimal number = bytes;

		while (Math.Round(number / 1024) >= 1)
		{
			number /= 1024;
			counter++;
		}

		return string.Format("{0:n1} {1}", number, suffixes[counter]);
	}
}
