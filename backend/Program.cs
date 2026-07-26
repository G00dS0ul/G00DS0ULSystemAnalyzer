using GSSystemAnalyzer;
using GSSystemAnalyzer.BackgroundWorkers;
using GSSystemAnalyzer.Engine;
using GSSystemAnalyzer.Hubs;
using GSSystemAnalyzer.Interfaces;
using GSSystemAnalyzer.Services;
using GSSystemAnalyzer.Services.Oem.Dell;
using LibreHardwareMonitor.Hardware;
using System.Runtime.InteropServices;


var builder = WebApplication.CreateBuilder(args);

builder.Services.AddCors(options =>
{
	options.AddPolicy("AllowFlutterApp", policy =>
	{
		policy.AllowAnyOrigin()
			.AllowAnyHeader()
			.AllowAnyMethod();
	});
});

builder.Services.AddControllers();
builder.Services.AddMemoryCache();

// Engine singletons
builder.Services.AddSingleton<DiskScannerEngine>();
builder.Services.AddSingleton<IDiskScannerEngine>(sp =>
	sp.GetRequiredService<DiskScannerEngine>());
builder.Services.AddSingleton<RamMonitoringEngine>();

// Service singletons (interface → implementation)
builder.Services.AddSingleton<ILargeFileHunterService, LargeFileHunterService>();
builder.Services.AddSingleton<ITempFolderCleanerService, TempFolderCleanerService>();
builder.Services.AddSingleton<INukeProtocolService, NukeProtocolService>();
builder.Services.AddSingleton<IDriveDetectionService, DriveDetectionService>();
builder.Services.AddSingleton<ISettingService, SettingsServices>();
builder.Services.AddSingleton<IProcessOwnerResolver, ProcessOwnerResolver>();
builder.Services.AddSingleton<IFileTypeScanner, FileTypeScanner>();
builder.Services.AddSingleton<IAgeHeatmapEngine, AgeHeatmapEngine>();
builder.Services.AddSingleton<IScanSnapshotStore, ScanSnapshotStore>();
builder.Services.AddSingleton<IScanDiffService, ScanDiffService>();

builder.Services.AddSingleton<ITelemetryHistoryBuffer, TelemetryHistoryBuffer>();

// Platform-specific CPU provider
if (RuntimeInformation.IsOSPlatform(OSPlatform.Windows))
{
	builder.Services.AddSingleton<ICpuMetricsProvider, WindowsCpuProvider>();
}
else if (RuntimeInformation.IsOSPlatform(OSPlatform.Linux))
{
	builder.Services.AddSingleton<ICpuMetricsProvider, LinuxCpuProvider>();
}
else
{
	throw new PlatformNotSupportedException("OS not supported for CPU telemetry");
}

if (RuntimeInformation.IsOSPlatform(OSPlatform.Windows))
{
    builder.Services.AddScoped<IStartupManager, WindowsStartupManager>();
}
else if (RuntimeInformation.IsOSPlatform(OSPlatform.Linux))
{
    builder.Services.AddScoped<IStartupManager, LinuxStartupManager>();
}
else
{
    throw new PlatformNotSupportedException("OS not supported for startup management");
}

// Platform-specific thermal provider
if (RuntimeInformation.IsOSPlatform(OSPlatform.Windows))
{
	builder.Services.AddSingleton<IThermalProvider, LibreThermalProvider>();
	builder.Services.AddSingleton<IWmiThermalFallback, WmiThermalFallback>();
	builder.Services.AddSingleton<IDellOemTelemetry, DellOemTelemetry>(); // User needs to have Dell OEM telemetry installed for this to work
}
else if (RuntimeInformation.IsOSPlatform(OSPlatform.Linux))
{
	builder.Services.AddSingleton<IThermalProvider, LinuxThermalProvider>();
}
else
{
	throw new PlatformNotSupportedException("OS not supported for thermal telemetry");
}

// Background services
builder.Services.AddHostedService<CpuSamplerEngine>();
builder.Services.AddHostedService<ThermalMonitoringEngine>();
builder.Services.AddHostedService<DriveMonitorService>();

// Schedule services
builder.Services.AddSingleton<IScheduleStore, ScheduleStore>();
builder.Services.AddSingleton<IScheduleService, ScheduleService>();
builder.Services.AddHostedService<ScheduledScanWorker>();

// Scoped services (per-request)
builder.Services.AddScoped<IDiskOperationService, DiskOperationsService>();
builder.Services.AddScoped<IDuplicateFileDetector, DuplicateFileDetector>();
builder.Services.AddScoped<IPermissionAuditService, PermissionAuditService>();

builder.Services.AddSignalR();
var app = builder.Build();

app.UseCors("AllowFlutterApp");
app.UseAuthorization();
app.MapControllers();
app.MapHub<SystemHub>("/systemHub");

// Health check endpoints
app.MapGet("/", () => new { status = "Server is running", timestamp = DateTime.UtcNow });
app.MapGet("/health", () => Results.Ok(new { status = "healthy" }));



var lifetime = app.Services.GetRequiredService<IHostApplicationLifetime>();
var engine = app.Services.GetRequiredService<DiskScannerEngine>();

lifetime.ApplicationStopping.Register(() =>
{
	var shutdownLogger = app.Services.GetRequiredService<ILogger<Program>>();
	shutdownLogger.LogInformation("Server shutting down: backing up memory to disk");
	engine.SaveMemoryToDisk();
});

app.Run();
