namespace GSSystemAnalyzer.Models;

public class DuplicateGroup
{
	public string FileHash { get; set; } = string.Empty;

	public List<string> FilePaths { get; set; } = new List<string>();

	public long FileSizeBytes { get; set; }
	public long WastedBytes => FileSizeBytes * (FilePaths.Count - 1);
}
