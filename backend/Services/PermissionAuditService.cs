using System.Runtime.InteropServices;
using System.Security.Principal;
using GSSystemAnalyzer.Models;
using GSSystemAnalyzer.Hubs;
using GSSystemAnalyzer.Interfaces;
using Microsoft.AspNetCore.SignalR;

#if WINDOWS
using System.Security.AccessControl;
#endif

namespace GSSystemAnalyzer.Services;

public class PermissionAuditService : IPermissionAuditService
{
	private readonly ISettingService _settings;
	private readonly ILogger<PermissionAuditService> _logger;

	// Extensions that should not live in user data directories
	private static readonly HashSet<string> ExecutableExtensions = new(StringComparer.OrdinalIgnoreCase)
	{
		".exe", ".dll", ".bat", ".cmd", ".ps1", ".sh", ".msi", ".scr", ".com"
	};

	// Windows: paths where executables are expected
	private static readonly string[] WindowsSystemPaths =
	{
		@"C:\Windows",
		@"C:\Program Files",
		@"C:\Program Files (x86)",
		@"C:\ProgramData"
	};

	// Linux: paths where executables are expected
	private static readonly string[] LinuxSystemPaths =
	{
		"/usr", "/bin", "/sbin", "/opt", "/snap"
	};

	// Windows: identities that indicate world-accessible permissions
	private static readonly HashSet<string> WorldIdentities = new(StringComparer.OrdinalIgnoreCase)
	{
		"Everyone",
		"Authenticated Users",
		@"BUILTIN\Users",
		"Users"
	};

	private readonly IHubContext<SystemHub> _hubContext;

	public PermissionAuditService(ISettingService settings, ILogger<PermissionAuditService> logger, IHubContext<SystemHub> hubContext)
	{
		_settings = settings;
		_logger = logger;
		_hubContext = hubContext;
	}

	public async Task<PermissionAuditResult> AuditAsync(string rootPath, CancellationToken cancellationToken = default)
	{
		var config = _settings.Current.Scan;

		return await Task.Run(async () =>
		{
			var issues = new List<PermissionIssue>();
			int totalScanned = 0;
			var stopwatch = System.Diagnostics.Stopwatch.StartNew();
			long lastReportTime = 0;

			var excludedPaths = config.ExcludedPaths
				.Select(p => Path.GetFullPath(p).TrimEnd(Path.DirectorySeparatorChar, Path.AltDirectorySeparatorChar))
				.ToList();

			var systemPaths = RuntimeInformation.IsOSPlatform(OSPlatform.Windows) ? WindowsSystemPaths : LinuxSystemPaths;

			var stack = new Stack<string>();
			stack.Push(rootPath);

			var options = new EnumerationOptions
			{
				IgnoreInaccessible = true,
				RecurseSubdirectories = false,
				ReturnSpecialDirectories = false,
				AttributesToSkip = FileAttributes.ReparsePoint
			};

			if (config.SkipHiddenFiles) options.AttributesToSkip |= FileAttributes.Hidden;
			if (config.SkipSystemFiles) options.AttributesToSkip |= FileAttributes.System;

			while (stack.Count > 0)
			{
				cancellationToken.ThrowIfCancellationRequested();
				var currentDir = stack.Pop();

				// Send progress via SignalR every 250ms
				if (stopwatch.ElapsedMilliseconds - lastReportTime > 250)
				{
					lastReportTime = stopwatch.ElapsedMilliseconds;
					_ = _hubContext.Clients.All.SendAsync("AuditProgress", new { scanned = totalScanned, issues = issues.Count }, cancellationToken);
				}

				try
				{
					var dirInfo = new DirectoryInfo(currentDir);

					foreach (var entry in dirInfo.EnumerateFileSystemInfos("*", options))
					{
						cancellationToken.ThrowIfCancellationRequested();

						var fullPath = entry.FullName;

						// Check skip conditions
						if (IsExcluded(fullPath, excludedPaths)) continue;

						bool isDir = entry is DirectoryInfo;

						// Prune system directories from traversal
						if (isDir && IsSystemPath(fullPath, systemPaths)) continue;

						totalScanned++;
						AuditEntry(fullPath, isDir, issues);

						if (isDir)
						{
							// Enforce depth if needed. Simple count of separators works
							int depth = fullPath.Split(Path.DirectorySeparatorChar).Length - rootPath.Split(Path.DirectorySeparatorChar).Length;
							if (depth < config.Depth)
							{
								stack.Push(fullPath);
							}
						}
					}
				}
				catch (UnauthorizedAccessException) { /* Skip inaccessible */ }
				catch (DirectoryNotFoundException) { /* Skip removed */ }
			}

			// Final progress push
			_ = _hubContext.Clients.All.SendAsync("AuditProgress", new { scanned = totalScanned, issues = issues.Count, completed = true }, cancellationToken);

			return new PermissionAuditResult
			{
				Root = rootPath,
				AuditedAt = DateTime.UtcNow,
				TotalScanned = totalScanned,
				Issues = issues
			};
		}, cancellationToken);
	}

	private void AuditEntry(string path, bool isDirectory, List<PermissionIssue> issues)
	{
		if (RuntimeInformation.IsOSPlatform(OSPlatform.Windows))
		{
			AuditWindows(path, isDirectory, issues);
		}
		else if (RuntimeInformation.IsOSPlatform(OSPlatform.Linux))
		{
			AuditLinux(path, isDirectory, issues);
		}

		// Executable-in-data-dir is platform-independent (extension + path check only)
		if (!isDirectory)
		{
			var systemPaths = RuntimeInformation.IsOSPlatform(OSPlatform.Windows)
				? WindowsSystemPaths
				: LinuxSystemPaths;
			CheckExecutableInDataDir(path, systemPaths, issues);
		}
	}

	private void AuditWindows(string path, bool isDirectory, List<PermissionIssue> issues)
	{
#if WINDOWS
		try
		{
			// Check for world-writable ACEs
			FileSystemSecurity security;
			if (isDirectory)
				security = new DirectoryInfo(path).GetAccessControl();
			else
				security = new FileInfo(path).GetAccessControl();

			var rules = security.GetAccessRules(true, true, typeof(SecurityIdentifier));

			foreach (FileSystemAccessRule rule in rules)
			{
				if (rule.AccessControlType != AccessControlType.Allow) continue;

				var sid = rule.IdentityReference as SecurityIdentifier;
				if (sid == null) continue;

				// Check if it's a world identity (Everyone, Authenticated Users, Builtin Users)
				bool isWorldIdentity = sid.IsWellKnown(WellKnownSidType.WorldSid) ||
									   sid.IsWellKnown(WellKnownSidType.AuthenticatedUserSid) ||
									   sid.IsWellKnown(WellKnownSidType.BuiltinUsersSid);

				if (!isWorldIdentity) continue;

				var hasWrite = rule.FileSystemRights.HasFlag(FileSystemRights.Write) ||
							   rule.FileSystemRights.HasFlag(FileSystemRights.FullControl) ||
							   rule.FileSystemRights.HasFlag(FileSystemRights.Modify);

				if (hasWrite)
				{
					issues.Add(new PermissionIssue
					{
						Path = path,
						Severity = "medium",
						Type = "world_writable",
						Description = isDirectory
							? $"Directory is writable by wide group (SID: {sid.Value})"
							: $"File is writable by wide group (SID: {sid.Value})"
					});
					break; // One world-writable flag is enough per entry
				}
			}

			// Check for orphaned / no owner
			try
			{
				var owner = security.GetOwner(typeof(SecurityIdentifier));
				if (owner == null)
				{
					issues.Add(new PermissionIssue
					{
						Path = path,
						Severity = "low",
						Type = "no_owner",
						Description = "File system entry has no identifiable owner SID"
					});
				}
			}
			catch
			{
				issues.Add(new PermissionIssue
				{
					Path = path,
					Severity = "low",
					Type = "no_owner",
					Description = "Could not resolve owner SID — possible orphaned entry"
				});
			}
		}
		catch (UnauthorizedAccessException)
		{
			// Inaccessible — silently skip, not an error
		}
		catch (Exception ex)
		{
			_logger.LogDebug("ACL read failed for {Path}: {Error}", path, ex.Message);
		}
#endif
	}

	private void AuditLinux(string path, bool isDirectory, List<PermissionIssue> issues)
	{
		try
		{
			var mode = File.GetUnixFileMode(path);

			// World-writable: OtherWrite bit set
			if (mode.HasFlag(UnixFileMode.OtherWrite))
			{
				issues.Add(new PermissionIssue
				{
					Path = path,
					Severity = "medium",
					Type = "world_writable",
					Description = isDirectory
						? $"Directory has world-write permission (mode: {mode})"
						: $"File has world-write permission (mode: {mode})"
				});
			}

			// Executable in data dir: execute bit set outside standard paths
			if (!isDirectory && mode.HasFlag(UnixFileMode.UserExecute))
			{
				CheckExecutableInDataDir(path, LinuxSystemPaths, issues);
			}
		}
		catch (UnauthorizedAccessException)
		{
			// Silently skip inaccessible entries
		}
		catch (Exception ex)
		{
			_logger.LogDebug("stat() failed for {Path}: {Error}", path, ex.Message);
		}
	}


	private static void CheckExecutableInDataDir(string path, string[] systemPaths, List<PermissionIssue> issues)
	{
		var ext = Path.GetExtension(path);
		if (string.IsNullOrEmpty(ext)) return;
		if (!ExecutableExtensions.Contains(ext)) return;

		var fullPath = Path.GetFullPath(path);
		var isInSystemPath = systemPaths.Any(sp =>
			fullPath.StartsWith(sp, StringComparison.OrdinalIgnoreCase));

		if (!isInSystemPath)
		{
			// Determine a human-friendly location name
			var parentDir = Path.GetDirectoryName(fullPath) ?? fullPath;
			var folderName = Path.GetFileName(parentDir);

			issues.Add(new PermissionIssue
			{
				Path = path,
				Severity = "high",
				Type = "executable_in_data_dir",
				Description = $"Executable ({ext}) found in {folderName} directory"
			});
		}
	}

	private static bool IsExcluded(string fullPath, List<string> excludedPaths)
	{
		return excludedPaths.Any(ep =>
			fullPath.StartsWith(ep, StringComparison.OrdinalIgnoreCase));
	}
	private static bool IsSystemPath(string fullPath, string[] systemPaths)
	{
		return systemPaths.Any(sp =>
			fullPath.Equals(sp, StringComparison.OrdinalIgnoreCase) ||
			fullPath.StartsWith(sp + Path.DirectorySeparatorChar, StringComparison.OrdinalIgnoreCase));
	}
}
