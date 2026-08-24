using GSSystemAnalyzer.Models.SettingDtos;

namespace GSSystemAnalyzer.Interfaces
{
	public interface ISettingService
	{
		AppSettingDto Current { get; }
		string AppDataFolder { get; }
		Task<AppSettingDto> LoadAsync();
		Task SaveAsync(AppSettingDto settings);
		event EventHandler<AppSettingDto> OnSettingsChanged;
	}
}
