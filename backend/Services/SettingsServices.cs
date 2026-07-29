using System.Text.Json;
using GSSystemAnalyzer.Interfaces;
using GSSystemAnalyzer.Models.SettingDtos;
using Microsoft.Extensions.Logging;

namespace GSSystemAnalyzer.Services
{
	public class SettingsServices : ISettingService
	{
		private readonly string _settingsFilePath;
		private readonly JsonSerializerOptions _jsonOptions;
		private readonly ILogger<SettingsServices> _logger;
		private readonly object _fileLoack = new();

		public AppSettingDto Current { get; private set; }
		public event EventHandler<AppSettingDto>? OnSettingsChanged;


		public SettingsServices(string? testFilePath = null, ILogger<SettingsServices>? logger = null)
		{
			_logger = logger ?? Microsoft.Extensions.Logging.Abstractions.NullLogger<SettingsServices>.Instance;
			if (testFilePath != null)
			{
				_settingsFilePath = testFilePath;
			}
			else
			{
				var appData = Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData);
				var appFolder = Path.Combine(appData, "GSAnalyzer");
				Directory.CreateDirectory(appFolder);

				_settingsFilePath = Path.Combine(appFolder, "appsettings.user.json");
			}

			_jsonOptions = new JsonSerializerOptions
			{ WriteIndented = true, PropertyNamingPolicy = JsonNamingPolicy.CamelCase };

			Current = LoadAsync().GetAwaiter().GetResult();
		}

		public async Task<AppSettingDto> LoadAsync()
		{
			if (!File.Exists(_settingsFilePath))
			{
				var defaults = AppSettingDto.GetFactoryDefaults();
				await SaveAsync(defaults);
				return defaults;
			}

			try
			{
				var json = await File.ReadAllTextAsync(_settingsFilePath);
				var settings = JsonSerializer.Deserialize<AppSettingDto>(json, _jsonOptions);
				return settings ?? AppSettingDto.GetFactoryDefaults();
			}
			catch (Exception ex)
			{
				_logger.LogWarning(ex, "Corrupt config file detected, restoring defaults");
				var defaults = AppSettingDto.GetFactoryDefaults();
				await SaveAsync(defaults);
				return defaults;
			}
		}

		public async Task SaveAsync(AppSettingDto settings)
		{
			Current = settings;
			var json = JsonSerializer.Serialize(settings, _jsonOptions);
			var tempPath = _settingsFilePath + "." + Guid.NewGuid().ToString("N") + ".tmp";

			lock (_fileLoack)
			{
				File.WriteAllText(tempPath, json);
				int retries = 5;
				while (true)
				{
					try
					{
						File.Move(tempPath, _settingsFilePath, overwrite: true);
						break;
					}
					catch (Exception ex) when (ex is UnauthorizedAccessException || ex is IOException)
					{
						retries--;
						if (retries == 0) throw;
						Thread.Sleep(20);
					}
				}
			}

			OnSettingsChanged?.Invoke(this, Current);

			await Task.CompletedTask;
		}
	}
}
