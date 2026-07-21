using GSSystemAnalyzer.Models;

namespace GSSystemAnalyzer.Interfaces
{
	public interface IPermissionAuditService
	{
		Task<PermissionAuditResult> AuditAsync(string rootPath, CancellationToken cancellationToken = default);
	}
}
