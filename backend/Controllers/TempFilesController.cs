using GSSystemAnalyzer.Interfaces;
using GSSystemAnalyzer.Models;
using Microsoft.AspNetCore.Mvc;

namespace GSSystemAnalyzer.Controllers;

[ApiController]
[Route("api/tempfiles")]
public class TempFilesController : ControllerBase
{
	private readonly ITempFolderCleanerService _cleanerService;

	public TempFilesController(ITempFolderCleanerService cleanerService)
	{
		_cleanerService = cleanerService;
	}

	/// <summary>GET /api/tempfiles/preview — enumerate all temp locations and return size breakdown.</summary>
	[HttpGet("preview")]
	public async Task<IActionResult> Preview(CancellationToken cancellationToken)
	{
		try
		{
			var result = await _cleanerService.PreviewAsync(cancellationToken);

			return Ok(new ApiResponse<TempPreviewResponse>
			{
				Success = true,
				Message = "Temp folder preview completed.",
				Data = result
			});
		}
		catch (OperationCanceledException)
		{
			return StatusCode(499, new ApiResponse<object>
			{
				Success = false,
				Message = "Temp preview cancelled by client."
			});
		}
		catch (Exception)
		{
			return BadRequest(new ApiResponse<object>
			{
				Success = false,
				Message = "Preview failed due to an unexpected error."
			});
		}
	}

	/// <summary>POST /api/tempfiles/clean — delete contents of selected temp directories.</summary>
	[HttpPost("clean")]
	public async Task<IActionResult> Clean(
		[FromBody] TempCleanRequest request,
		CancellationToken cancellationToken)
	{
		try
		{
			if (request?.Paths == null || request.Paths.Count == 0)
			{
				return BadRequest(new ApiResponse<object>
				{
					Success = false,
					Message = "At least one temp path must be specified."
				});
			}

			var result = await _cleanerService.CleanAsync(request.Paths, cancellationToken);

			return Ok(new ApiResponse<TempCleanResult>
			{
				Success = true,
				Message = $"Cleaned {result.DeletedFiles} files, freed {result.FreedFormatted}.",
				Data = result
			});
		}
		catch (UnauthorizedAccessException ex)
		{
			return StatusCode(403, new ApiResponse<object>
			{
				Success = false,
				Message = ex.Message
			});
		}
		catch (OperationCanceledException)
		{
			return StatusCode(499, new ApiResponse<object>
			{
				Success = false,
				Message = "Clean operation cancelled by client."
			});
		}
		catch (Exception)
		{
			return BadRequest(new ApiResponse<object>
			{
				Success = false,
				Message = "Clean failed due to an unexpected error."
			});
		}
	}
}
