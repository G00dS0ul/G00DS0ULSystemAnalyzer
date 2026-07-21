using System;
using System.Collections.Generic;
using Microsoft.AspNetCore.Mvc;
using GSSystemAnalyzer.Models;
using GSSystemAnalyzer.Services;
using GSSystemAnalyzer.Interfaces;

namespace GSSystemAnalyzer.Controllers;

[ApiController]
[Route("api/[controller]")]
public class DrivesController : ControllerBase
{
	private readonly IDriveDetectionService _driveService;

	public DrivesController(IDriveDetectionService driveService)
	{
		_driveService = driveService;
	}

	[HttpGet]
	public IActionResult GetAllDrives()
	{
		try
		{
			var drives = _driveService.GetReadyDrives();

			return Ok(new ApiResponse<List<DriveMetric>>
			{
				Success = true,
				Message = $"Successfully retrieved {drives.Count} ready drives.",
				Data = drives
			});
		}
		catch (Exception ex)
		{
			return StatusCode(500, new ApiResponse<object>
			{
				Success = false,
				Message = $"Failed to enumerate drives: {ex.Message}"
			});
		}
	}
}
