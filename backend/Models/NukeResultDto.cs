namespace GSSystemAnalyzer.Models
{
	public class NukeResultDto
	{
		public int DeletedFiles { get; set; }
		public long FreedBytes { get; set; }
		public string FreedFormatted { get; set; } = string.Empty;
		public long StagedBytes { get; set; }
		public string StagedFormatted { get; set; } = string.Empty;
		public int SkippedFiles { get; set; }
		public bool RecycleBinUsed { get; set; }
		public bool Recoverable { get; set; }
		public string OperationId { get; set; } = string.Empty;
	}
}
