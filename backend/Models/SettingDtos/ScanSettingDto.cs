namespace GSSystemAnalyzer.Models.SettingDtos
{
	public class ScanSettingDto
	{
		public int Depth { get; set; } = 10;
		public List<string> ExcludedPaths { get; set; } =
			new() { "C:/Windows", "C:/Program Files", "C:/Program Files (x86)" };
		public bool FollowSymlinks { get; set; } = false;
		public bool SkipHiddenFiles { get; set; } = true;
		public bool SkipSystemFiles { get; set; } = true;
		public int? MaxFileSizeMb { get; set; } = null;

	}
}
