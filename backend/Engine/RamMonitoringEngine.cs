using System.Collections.Concurrent;
using System.Diagnostics;
using System.Runtime.InteropServices;
using GSSystemAnalyzer.Hubs;
using GSSystemAnalyzer.Interfaces;
using GSSystemAnalyzer.Models;
using GSSystemAnalyzer.Services;
using Microsoft.AspNetCore.SignalR;
using Microsoft.Extensions.Logging;

namespace GSSystemAnalyzer.Engine
{
	public class RamMonitoringEngine
	{
		private CancellationTokenSource? _radarCts;
		private readonly IHubContext<SystemHub> _hub;
		private readonly ISettingService _settings;
		private readonly IProcessOwnerResolver _ownerResolver;
		private readonly ITelemetryHistoryBuffer _historyBuffer;
		private readonly ILogger<RamMonitoringEngine> _logger;
		private readonly Func<DateTimeOffset> _timeProvider;
		private readonly ThresholdAlertTracker _alertTracker;
		private readonly object _lock = new object();
		private TimeSpan _pollInterval;

		/// <summary>True while the RAM alert is active (debounce passed, not yet cleared).</summary>
		public bool IsRamAlerting => _alertTracker.IsAlerting;

		private const long CriticalAvailableBytes = 256L * 1024L * 1024L; // 256 MB

		// Per-process CPU baseline from the previous tick
		private readonly ConcurrentDictionary<int, (TimeSpan CpuTime, DateTime Timestamp)> _prevCpuSnapshot = new();

		// Consecutive zero-CPU-tick counter per PID for status heuristic
		private readonly ConcurrentDictionary<int, int> _zeroTickCounts = new();

		public RamMonitoringEngine(
			IHubContext<SystemHub> hub,
			ISettingService settings,
			IProcessOwnerResolver ownerResolver,
			ITelemetryHistoryBuffer historyBuffer,
			ILogger<RamMonitoringEngine> logger,
			Func<DateTimeOffset>? timeProvider = null,
			ThresholdAlertTracker? alertTracker = null)
		{
			_hub = hub;
			_settings = settings;
			_ownerResolver = ownerResolver;
			_historyBuffer = historyBuffer;
			_logger = logger;
			_timeProvider = timeProvider ?? (() => DateTimeOffset.UtcNow);
			_alertTracker = alertTracker ?? new ThresholdAlertTracker(
				requiredConsecutive: 5,
				hysteresisBand: 5.0,
				reAlertIncrease: 5.0,
				cooldownInterval: TimeSpan.FromMinutes(60));
			_pollInterval = TimeSpan.FromMilliseconds(settings.Current.Monitoring.RamPollIntervalMs);
			settings.OnSettingsChanged += (_, s) =>
				_pollInterval = TimeSpan.FromMilliseconds(s.Monitoring.RamPollIntervalMs);

			// Refresh owner cache on its own slow cadence — never blocks the poll loop
			_ = Task.Run(async () =>
			{
				while (true)
				{
					try { _ownerResolver.RefreshCache(); }
					catch { /* ignore WMI errors */ }
					await Task.Delay(TimeSpan.FromSeconds(5));
				}
			});
		}

		public void StartRadar()
		{
			lock (_lock)
			{
				if (_radarCts != null && !_radarCts.IsCancellationRequested) return;

				_radarCts = new CancellationTokenSource();
				_ = RadarLoopAsync(_radarCts.Token);
				_logger.LogInformation("RAM radar started");
			}
		}

		public void StopRadar()
		{
			_radarCts?.Cancel();
			_logger.LogInformation("RAM radar stopped");
		}

		private async Task RadarLoopAsync(CancellationToken token)
		{
			try
			{
				while (!token.IsCancellationRequested)
				{
					var nextTick = Task.Delay(_pollInterval, token);

					try
					{
						var snapshot = GetTopProcesses(100);
						var globalMetrics = SystemMemoryMetrics.GetLiveMetrics();

						var payload = new
						{
							Global = globalMetrics ?? new SystemMemoryMetrics.GlobalMemoryMetrics
							{
								ActiveGb = 0.0,
								CacheGb = 0.0,
								SwapGb = 0.0,
								TotalSwapGb = 0.0,
								TotalGb = 16.0,
							},
							Processes = snapshot
						};

						await _hub.Clients.All.SendAsync("RamUpdate", payload, cancellationToken: token);

						// Record to history buffer for historical charts
						if (globalMetrics != null)
						{
							double activeGb = globalMetrics.ActiveGb;
							double totalGb = globalMetrics.TotalGb;
							double percent = totalGb > 0 ? Math.Round((activeGb / totalGb) * 100, 1) : 0;

							_historyBuffer.Record("ram", activeGb);
							_historyBuffer.Record("ram_percent", percent);

							// ── RAM pressure alert evaluation ─────────────────────
							await EvaluateRamPressureAsync(globalMetrics, snapshot, token);
						}

						_logger.LogDebug("RAM sweep at {Time} — sent {ProcessCount} processes", DateTime.Now.ToString("HH:mm:ss"), snapshot.Count);
					}
					catch (Exception ex) when (ex is not OperationCanceledException)
					{
						_logger.LogWarning(ex, "RAM engine tick error");
					}
					await nextTick;
				}
			}
			catch (OperationCanceledException) { }
			catch (Exception ex)
			{
				_logger.LogError(ex, "RAM engine fatal radar error");
			}
		}

		private List<ProcessTelemetry> GetTopProcesses(int limit)
		{
			var activeProcesses = Process.GetProcesses();
			var telemetryList = new List<ProcessTelemetry>(activeProcesses.Length);
			var seenPids = new HashSet<int>(activeProcesses.Length);
			var now = DateTime.UtcNow;

			foreach (var p in activeProcesses)
			{
				try
				{
					seenPids.Add(p.Id);

					// CPU% — delta between two consecutive ticks
					double cpuPercent = 0.0;
					try
					{
						var currentCpuTime = p.TotalProcessorTime;

						if (_prevCpuSnapshot.TryGetValue(p.Id, out var prev))
						{
							var elapsedMs = (now - prev.Timestamp).TotalMilliseconds;
							if (elapsedMs > 0)
							{
								var cpuMs = (currentCpuTime - prev.CpuTime).TotalMilliseconds;
								cpuPercent = cpuMs / (elapsedMs * Environment.ProcessorCount) * 100.0;
								cpuPercent = Math.Clamp(cpuPercent, 0.0, 100.0);
								cpuPercent = Math.Round(cpuPercent, 1);
							}
						}
						// else: first tick for this PID — emit 0

						_prevCpuSnapshot[p.Id] = (currentCpuTime, now);
					}
					catch
					{
						// TotalProcessorTime throws on some system processes — emit 0
						_prevCpuSnapshot[p.Id] = (TimeSpan.Zero, now);
					}

					// STATUS — heuristic using zero-tick counter
					string status;
					if (RuntimeInformation.IsOSPlatform(OSPlatform.Linux))
					{
						status = GetLinuxProcessStatus(p.Id);
					}
					else
					{
						status = GetStatusHeuristic(p, cpuPercent);
					}

					// USER — resolved from the batch-refreshed cache
					var user = _ownerResolver.Resolve(p.Id);

					telemetryList.Add(new ProcessTelemetry
					{
						ProcessId = p.Id,
						Name = p.ProcessName,
						WorkingSetBytes = p.WorkingSet64,
						CpuPercent = cpuPercent,
						Status = status,
						User = user
					});
				}
				catch
				{

				}
				finally
				{
					p.Dispose();
				}
			}

			// Prune stale entries — PIDs that no longer exist
			foreach (var stalePid in _prevCpuSnapshot.Keys.Where(pid => !seenPids.Contains(pid)).ToList())
			{
				_prevCpuSnapshot.TryRemove(stalePid, out _);
				_zeroTickCounts.TryRemove(stalePid, out _);
			}

			// Sort: primary by CPU% descending, secondary by RAM descending for ties
			var sortedList = telemetryList
				.OrderByDescending(p => p.CpuPercent)
				.ThenByDescending(p => p.WorkingSetBytes)
				.Take(limit)
				.ToList();

			return sortedList;
		}

		private string GetStatusHeuristic(Process p, double cpuPercent)
		{
			try
			{
				if (p.HasExited) return "STOPPED";
			}
			catch
			{
				// HasExited throws on access-denied processes — treat as running
			}

			if (cpuPercent == 0.0)
			{
				var count = _zeroTickCounts.AddOrUpdate(p.Id, 1, (_, prev) => prev + 1);
				if (count >= 5) return "SLEEPING";
			}
			else
			{
				_zeroTickCounts[p.Id] = 0;
			}

			return "RUNNING";
		}

		private string GetLinuxProcessStatus(int pid)
		{
#if !WINDOWS
            try
            {
                var statPath = $"/proc/{pid}/stat";
                if (!File.Exists(statPath)) return "STOPPED";

                var content = File.ReadAllText(statPath);

                // The state character is field 3, but field 2 (comm) can contain spaces and parens
                // Find the last ')' to skip the comm field safely
                var lastParen = content.LastIndexOf(')');
                if (lastParen < 0 || lastParen + 2 >= content.Length) return "RUNNING";

                var stateChar = content[lastParen + 2]; // space + state character

                return stateChar switch
                {
                    'R' => "RUNNING",
                    'S' or 'D' or 'I' => "SLEEPING",
                    'T' or 't' => "STOPPED",
                    'Z' => "ZOMBIE",
                    _ => "RUNNING"
                };
            }
            catch
            {
                return "RUNNING";
            }
#else
			return "RUNNING";
#endif
		}

		// ── RAM pressure alert evaluation ──────────────────────────────
		internal async Task EvaluateRamPressureAsync(
			SystemMemoryMetrics.GlobalMemoryMetrics globalMetrics,
			List<ProcessTelemetry> processList,
			CancellationToken token)
		{
			double totalGb = globalMetrics.TotalGb;
			double activeGb = globalMetrics.ActiveGb;
			double availableGb = globalMetrics.CacheGb; // CacheGb holds available physical RAM (ullAvailPhys)
			double usedPercent = totalGb > 0 ? Math.Round((activeGb / totalGb) * 100, 1) : 0;
			long availableBytes = (long)(availableGb * 1024 * 1024 * 1024);

			var alerts = _settings.Current.Alerts;
			double thresholdPercent = alerts.RamThresholdPercent;
			long minimumFreeBytes = alerts.RamMinimumFreeMb * 1024L * 1024L;

			// Dual condition: percentage AND absolute free-memory floor
			bool underPressure =
				usedPercent >= thresholdPercent &&
				availableBytes <= minimumFreeBytes;

			var now = _timeProvider();
			var evaluation = _alertTracker.Evaluate(underPressure, usedPercent, thresholdPercent, now);

			switch (evaluation.Action)
			{
				case AlertAction.Fire:
					_alertTracker.RecordAlertFired(usedPercent, now);
					await SendRamAlertAsync(usedPercent, availableBytes, thresholdPercent, alerts.RamMinimumFreeMb,
						evaluation.FirstDetectedAt!.Value, processList, token);
					break;

				case AlertAction.Clear:
					_alertTracker.RecordCleared();
					await SendRamAlertClearedAsync(usedPercent, availableBytes, token);
					break;
			}
		}

		private async Task SendRamAlertAsync(
			double usedPercent,
			long availableBytes,
			double thresholdPercent,
			int minimumFreeMb,
			DateTimeOffset firstDetectedAt,
			List<ProcessTelemetry> processList,
			CancellationToken token)
		{
			var now = _timeProvider();
			string severity = availableBytes < CriticalAvailableBytes ? "critical" : "warning";
			int sustainedForSeconds = (int)(now - firstDetectedAt).TotalSeconds;

			// Top 3 consumers by working set from the same tick's process list
			var topConsumers = processList
				.OrderByDescending(p => p.WorkingSetBytes)
				.Take(3)
				.Select(p => new TopConsumerEntry
				{
					Pid = p.ProcessId,
					Name = p.Name,
					RamMb = p.RamMb
				})
				.ToList();

			_logger.LogWarning(
				"RAM Alert [{Severity}]: {UsedPercent}% used, {Available} available (threshold: {Threshold}%, min free: {MinFreeMb} MB)",
				severity, usedPercent, FormatSize(availableBytes), thresholdPercent, minimumFreeMb);

			var payload = new RamAlertPayload
			{
				UsedPercent = usedPercent,
				AvailablePhysicalBytes = availableBytes,
				AvailableFormatted = FormatSize(availableBytes),
				ThresholdPercent = thresholdPercent,
				MinimumFreeMb = minimumFreeMb,
				Severity = severity,
				SustainedForSeconds = sustainedForSeconds,
				TopConsumers = topConsumers
			};

			await _hub.Clients.All.SendAsync("RamAlert", payload, cancellationToken: token);
		}

		private async Task SendRamAlertClearedAsync(
			double usedPercent,
			long availableBytes,
			CancellationToken token)
		{
			_logger.LogInformation(
				"RAM Alert Cleared: usage recovered to {UsedPercent}%, {Available} available.",
				usedPercent, FormatSize(availableBytes));

			var payload = new
			{
				usedPercent,
				availableFormatted = FormatSize(availableBytes)
			};

			await _hub.Clients.All.SendAsync("RamAlertCleared", payload, cancellationToken: token);
		}

		private static string FormatSize(long bytes)
		{
			string[] suffixes = { "B", "KB", "MB", "GB", "TB", "PB", "EB" };
			int counter = 0;
			decimal number = bytes;
			while (Math.Round(number / 1024) >= 1 && counter < suffixes.Length - 1)
			{
				number /= 1024;
				counter++;
			}
			return string.Format("{0:n1} {1}", number, suffixes[counter]);
		}

		public int ExecuteOrder66(List<int> processIds)
		{
			int killCount = 0;
			foreach (var pid in processIds)
			{
				try
				{
					var process = Process.GetProcessById(pid);
					process.Kill();
					killCount++;
					_logger.LogDebug("Process killed: PID {Pid}", pid);
				}
				catch
				{

				}
			}
			_logger.LogInformation("Process termination completed: {KillCount} processes terminated", killCount);
			return killCount;
		}
	}
}
