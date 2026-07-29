using GSSystemAnalyzer.Interfaces;
using GSSystemAnalyzer.Models;
using Microsoft.AspNetCore.Mvc;

namespace GSSystemAnalyzer.Controllers
{
	[ApiController]
	[Route("api/[controller]")]
	public class AuditController : ControllerBase
	{
		private readonly IPermissionAuditService _auditService;

		public AuditController(IPermissionAuditService auditService)
		{
			_auditService = auditService;
		}

		[HttpPost("permissions")]
		public async Task<IActionResult> AuditPermissions(
			[FromBody] PermissionAuditRequest request,
			CancellationToken cancellationToken)
		{
			if (string.IsNullOrWhiteSpace(request?.Root))
			{
				return BadRequest(new { error = "ROOT_REQUIRED", message = "A root path is required." });
			}

			if (!Directory.Exists(request.Root))
			{
				return BadRequest(new { error = "DIRECTORY_NOT_FOUND", message = $"Directory does not exist: {request.Root}" });
			}

			try
			{
				var result = await _auditService.AuditAsync(request.Root, cancellationToken);
				return Ok(result);
			}
			catch (OperationCanceledException)
			{
				return StatusCode(499, new { error = "AUDIT_CANCELLED", message = "The audit was cancelled." });
			}
			catch (Exception ex)
			{
				return StatusCode(500, new { error = "AUDIT_FAILED", message = $"Audit failed: {ex.Message}" });
			}
		}
	}
}
