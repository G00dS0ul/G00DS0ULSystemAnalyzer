using System;
using System.Collections.Generic;
using System.IO;
using System.Threading.Tasks;
using GSSystemAnalyzer.Controllers;
using GSSystemAnalyzer.Interfaces;
using GSSystemAnalyzer.Models;
using Microsoft.AspNetCore.Mvc;
using Moq;
using Xunit;

namespace GSSystemAnalyzer.Tests.Controllers;

public class NukeControllerTests
{
	private readonly Mock<INukeProtocolService> _service = new();
	private readonly Mock<IDriveDetectionService> _drives = new();
	private readonly NukeController _controller;

	public NukeControllerTests()
	{
		_controller = new NukeController(_service.Object);
		DriveReady("C:\\"); // default: C:\ is ready
	}

	private void DriveReady(params string[] roots) =>
		_drives.Setup(d => d.GetReadyDrives())
			   .Returns(roots.Select(r => new DriveMetric { Name = r }).ToList());

	private static int Status(IActionResult r) => ((ObjectResult)r).StatusCode ?? 200;
	private static dynamic Body(IActionResult r) => ((ObjectResult)r).Value!;

	private static NukeOperation Op(bool recoverable) =>
		new("op-1", DateTime.UtcNow, new() { "C:\\a" }, new() { "C:\\a" }, recoverable, 1);

	[Fact]
	public async Task Execute_NoPaths_Returns400()
	{
		var r = await _controller.NukeNode(new NukeExecuteRequest { Paths = new(), PlanToken = "t" }, _drives.Object);
		Assert.Equal(400, Status(r));
		Assert.False((bool)Body(r).Success);
	}

	[Fact]
	public async Task Execute_DriveNotReady_Returns400()
	{
		DriveReady("D:\\");
		var r = await _controller.NukeNode(
			new NukeExecuteRequest { Paths = new() { "C:\\temp\\x" }, PlanToken = "t" }, _drives.Object);
		Assert.Equal(400, Status(r));
		Assert.Contains("Drive not ready", (string)Body(r).Message);
	}

	[Fact]
	public async Task Execute_ProtectedWindowsPath_Returns400()
	{
		var r = await _controller.NukeNode(
			new NukeExecuteRequest { Paths = new() { "C:\\Windows\\System32" }, PlanToken = "t" }, _drives.Object);
		Assert.Equal(400, Status(r));
		Assert.Contains("PROTECTED", (string)Body(r).Message);
	}

	[Fact]
	public async Task Execute_EmptyPlanToken_Returns403_AndNeverCallsService()
	{
		var r = await _controller.NukeNode(
			new NukeExecuteRequest { Paths = new() { "C:\\temp\\x" }, PlanToken = "   " }, _drives.Object);
		Assert.Equal(403, Status(r));
		Assert.Contains("planToken", (string)Body(r).Message);
		_service.Verify(s => s.ObliterateNodeAsync(It.IsAny<List<string>>(), It.IsAny<string>(), It.IsAny<bool>()),
						Times.Never);
	}

	[Fact]
	public async Task Execute_Valid_Returns200_WithResult()
	{
		var dto = new NukeResultDto
		{
			DeletedFiles = 3,
			StagedBytes = 100,
			RecycleBinUsed = true,
			Recoverable = true,
			OperationId = "op-1"
		};
		_service.Setup(s => s.ObliterateNodeAsync(It.IsAny<List<string>>(), "tok", true)).ReturnsAsync(dto);

		var r = await _controller.NukeNode(
			new NukeExecuteRequest { Paths = new() { "C:\\temp\\x" }, PlanToken = "tok", UseRecycleBin = true },
			_drives.Object);

		Assert.Equal(200, Status(r));
		Assert.True((bool)Body(r).Success);
		Assert.Equal("op-1", (string)Body(r).Data.OperationId);
	}

	[Fact]
	public async Task Execute_InvalidOrReusedToken_Returns403()
	{
		_service.Setup(s => s.ObliterateNodeAsync(It.IsAny<List<string>>(), It.IsAny<string>(), It.IsAny<bool>()))
				.ThrowsAsync(new UnauthorizedAccessException("Path 'C:\\temp\\x' was not part of the previewed plan."));

		var r = await _controller.NukeNode(
			new NukeExecuteRequest { Paths = new() { "C:\\temp\\x" }, PlanToken = "stale" }, _drives.Object);

		Assert.Equal(403, Status(r));
		Assert.Contains("previewed plan", (string)Body(r).Message);
	}

	[Fact]
	public async Task Execute_MissingFile_Returns404()
	{
		_service.Setup(s => s.ObliterateNodeAsync(It.IsAny<List<string>>(), It.IsAny<string>(), It.IsAny<bool>()))
				.ThrowsAsync(new FileNotFoundException("gone"));

		var r = await _controller.NukeNode(
			new NukeExecuteRequest { Paths = new() { "C:\\temp\\x" }, PlanToken = "tok" }, _drives.Object);

		Assert.Equal(404, Status(r));
	}

	[Fact]
	public void Peek_None_Returns404()
	{
		_service.Setup(s => s.PeekUndo()).Returns((NukeOperation?)null);
		Assert.Equal(404, Status(_controller.PeekUndo()));
	}

	[Fact]
	public void Peek_Found_Returns200()
	{
		_service.Setup(s => s.PeekUndo()).Returns(Op(recoverable: true));
		var r = _controller.PeekUndo();
		Assert.Equal(200, Status(r));
		Assert.Equal("op-1", (string)Body(r).Data.OperationId);
	}

	[Fact]
	public void Undo_NothingToUndo_Returns404()
	{
		_service.Setup(s => s.UndoNuke(null)).Returns((NukeResultDto?)null);
		Assert.Equal(404, Status(_controller.UndoNuke()));
		_service.Verify(s => s.UndoNuke(null), Times.Once);
	}

	[Fact]
	public void Undo_Recoverable_Returns200()
	{
		_service.Setup(s => s.UndoNuke(null))
				.Returns(new NukeResultDto { DeletedFiles = 1, Recoverable = true, OperationId = "op-1" });

		var r = _controller.UndoNuke();
		Assert.Equal(200, Status(r));
		Assert.True((bool)Body(r).Success);
	}

	[Fact]
	public void History_Returns200_WithList()
	{
		_service.Setup(s => s.GetUndoHistory()).Returns(new List<NukeOperation> { Op(true) });
		Assert.Equal(200, Status(_controller.GetUndoHistory()));
	}

	[Fact]
	public void Clear_Returns200_AndClearsStack()
	{
		var r = _controller.ClearUndoStack();
		Assert.Equal(200, Status(r));
		_service.Verify(s => s.ClearUndoStack(), Times.Once);
	}

	[Fact]
	public void Abort_Returns200_AndSignalsService()
	{
		var r = _controller.AbortNuke();
		Assert.Equal(200, Status(r));
		_service.Verify(s => s.TriggerNukeAbort(), Times.Once);
	}
}
