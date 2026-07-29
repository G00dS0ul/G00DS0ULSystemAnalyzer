using System.Collections.Concurrent;
using System.Runtime.InteropServices;
using GSSystemAnalyzer.Interfaces;

namespace GSSystemAnalyzer.Services
{
	public class ProcessOwnerResolver : IProcessOwnerResolver
	{
		private ConcurrentDictionary<int, string> _cache = new();

		// Linux only — /etc/passwd UID?username map
		private Dictionary<int, string> _passwdCache = new();
		private DateTime _passwdLastRead = DateTime.MinValue;

		public void RefreshCache()
		{
			if (RuntimeInformation.IsOSPlatform(OSPlatform.Windows))
				RefreshWindowsCache();
		}

		public string Resolve(int processId)
		{
			if (_cache.TryGetValue(processId, out var cached)) return cached;
			if (RuntimeInformation.IsOSPlatform(OSPlatform.Linux)) return ResolveLinux(processId);
			return "SYSTEM";
		}

		private void RefreshWindowsCache()
		{
			var newCache = new ConcurrentDictionary<int, string>();
			try
			{
				var searcher = new System.Management.ManagementObjectSearcher(
					"SELECT ProcessId FROM Win32_Process");
				foreach (System.Management.ManagementObject obj in searcher.Get())
				{
					try
					{
						var pid = (int)(uint)obj["ProcessId"];
						var outParams = obj.InvokeMethod("GetOwner", null, null)
							as System.Management.ManagementBaseObject;
						var user = outParams?["User"]?.ToString() ?? "SYSTEM";
						newCache[pid] = user;
					}
					catch { /* protected process — skip */ }
				}
			}
			catch { }
			_cache = newCache;
		}

		private string ResolveLinux(int pid)
		{
			try
			{
				var statusPath = $"/proc/{pid}/status";
				if (!File.Exists(statusPath)) return "SYSTEM";

				foreach (var line in File.ReadLines(statusPath))
				{
					if (!line.StartsWith("Uid:")) continue;
					var parts = line.Split('\t', StringSplitOptions.RemoveEmptyEntries);
					if (parts.Length < 2 || !int.TryParse(parts[1], out var uid)) break;

					RefreshPasswdCacheIfStale();
					var name = _passwdCache.TryGetValue(uid, out var n) ? n : $"uid:{uid}";
					_cache[pid] = name;
					return name;
				}
			}
			catch { }
			return "SYSTEM";
		}

		private void RefreshPasswdCacheIfStale()
		{
			const string path = "/etc/passwd";
			try
			{
				var mtime = File.GetLastWriteTimeUtc(path);
				if (mtime <= _passwdLastRead) return;   // no change

				var map = new Dictionary<int, string>();
				foreach (var line in File.ReadLines(path))
				{
					var p = line.Split(':');
					if (p.Length >= 3 && int.TryParse(p[2], out var uid))
						map[uid] = p[0];
				}
				_passwdCache = map;
				_passwdLastRead = mtime;
			}
			catch { }
		}
	}
}
