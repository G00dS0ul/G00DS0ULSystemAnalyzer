namespace GSSystemAnalyzer.Models;

public class PermissionIssue
{
	public string Path { get; set; } = string.Empty;
	public string Severity { get; set; } = string.Empty;   // "high" | "medium" | "low"
	public string Type { get; set; } = string.Empty;        // "executable_in_data_dir" | "world_writable" | "no_owner"
	public string Description { get; set; } = string.Empty;
}

public class PermissionAuditResult
{
	public string Root { get; set; } = string.Empty;
	public DateTime AuditedAt { get; set; }
	public int TotalScanned { get; set; }
	public List<PermissionIssue> Issues { get; set; } = new();
}

public class PermissionAuditRequest
{
	public string Root { get; set; } = string.Empty;
}
