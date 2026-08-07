using GSSystemAnalyzer.Engine;
using GSSystemAnalyzer.Interfaces;
using Microsoft.AspNetCore.Mvc;

namespace GSSystemAnalyzer.Controllers;

[ApiController]
[Route("api/[controller]")]
public class CacheController : ControllerBase
{
	private readonly IScanCacheService _cacheService;
	private readonly IDiskScannerEngine _scanner;

	public CacheController(IScanCacheService cacheService, IDiskScannerEngine scanner)
	{
		_cacheService = cacheService;
		_scanner = scanner;
	}

	[HttpGet("stats")]
	public IActionResult GetStats()
	{
		var stats = _cacheService.GetStats();
		return Ok(new
		{
			success = true,
			data = stats
		});
	}

	[HttpDelete]
	public IActionResult EvictCache([FromQuery] string? root = null)
	{
		if (!string.IsNullOrWhiteSpace(root))
		{
			_cacheService.EvictRoot(root);
			return Ok(new
			{
				success = true,
				message = $"Cache for root '{root}' evicted successfully."
			});
		}

		_cacheService.Clear();
		_scanner.ClearCache();
		return Ok(new
		{
			success = true,
			message = "Scan cache completely cleared. Run a new Directory Scan to repopulate."
		});
	}
}
