using System;
using System.Collections.Generic;
using System.IO;
using System.Threading;
using System.Threading.Tasks;
using GSSystemAnalyzer.Hubs;
using GSSystemAnalyzer.Interfaces;
using GSSystemAnalyzer.Services;
using Microsoft.AspNetCore.SignalR;
using Microsoft.Extensions.Logging.Abstractions;
using Moq;
using Xunit;

namespace GSSystemAnalyzer.Tests.Services;

public class NukeProtocolServiceTests : IDisposable
{
	private readonly string _workDir;
	private readonly string _stagingBase;
	private readonly Mock<IDiskScannerEngine> _scanner = new();

	public NukeProtocolServiceTests()
	{
		_workDir = Path.Combine(Path.GetTempPath(), "gsnuke_work_" + Guid.NewGuid().ToString("N"));
		_stagingBase = Path.Combine(Path.GetTempPath(), "gsnuke_stage_" + Guid.NewGuid().ToString("N"));
		Directory.CreateDirectory(_workDir);
		Directory.CreateDirectory(_stagingBase);

		_scanner.Setup(s => s.NukeToken()).Returns(CancellationToken.None);
		_scanner.Setup(s => s.InvalidatePaths(It.IsAny<IEnumerable<string>>()));
	}

	public void Dispose()
	{
		foreach (var d in new[] { _workDir, _stagingBase })
			try { if (Directory.Exists(d)) Directory.Delete(d, true); } catch { /* ignore */ }
	}

	private NukeProtocolService CreateService()
	{
		var hub = new Mock<IHubContext<SystemHub>>();
		var clients = new Mock<IHubClients>();
		var proxy = new Mock<IClientProxy>();
		clients.Setup(c => c.All).Returns(proxy.Object);
		hub.Setup(h => h.Clients).Returns(clients.Object);
		proxy.Setup(p => p.SendCoreAsync(It.IsAny<string>(), It.IsAny<object?[]>(), It.IsAny<CancellationToken>()))
			 .Returns(Task.CompletedTask);

		return new NukeProtocolService(_scanner.Object, hub.Object, NullLogger<NukeProtocolService>.Instance, runStartupCleanup: false)
		{
			StagingBaseResolver = _ => _stagingBase
		};
	}

	private string NewFile(string content = "hello world")
	{
		var path = Path.Combine(_workDir, $"f_{Guid.NewGuid():N}.txt");
		File.WriteAllText(path, content);
		return path;
	}

	private static async Task<string> TokenFor(NukeProtocolService svc, params string[] paths)
		=> (await svc.PreviewNukeAsync(new List<string>(paths))).PlanToken;

	[Fact]
	public async Task Obliterate_EmptyToken_Throws()
	{
		var svc = CreateService();
		await Assert.ThrowsAsync<UnauthorizedAccessException>(
			() => svc.ObliterateNodeAsync(new() { NewFile() }, "", useRecycleBin: false));
	}

	[Fact]
	public async Task Obliterate_UnknownToken_Throws()
	{
		var svc = CreateService();
		await Assert.ThrowsAsync<UnauthorizedAccessException>(
			() => svc.ObliterateNodeAsync(new() { NewFile() }, "not-a-real-token", useRecycleBin: false));
	}

	[Fact]
	public async Task Obliterate_PathNotInPlan_Throws_AndDoesNotDelete()
	{
		var svc = CreateService();
		var planned = NewFile();
		var notPlanned = NewFile();
		var token = await TokenFor(svc, planned); // token bound to `planned` only

		await Assert.ThrowsAsync<UnauthorizedAccessException>(
			() => svc.ObliterateNodeAsync(new() { planned, notPlanned }, token, useRecycleBin: false));

		Assert.True(File.Exists(planned)); // nothing deleted — validation runs first
		Assert.True(File.Exists(notPlanned));
	}

	[Fact]
	public async Task Obliterate_TokenIsSingleUse()
	{
		var svc = CreateService();
		var file = NewFile();
		var token = await TokenFor(svc, file);

		await svc.ObliterateNodeAsync(new() { file }, token, useRecycleBin: true); // consumes token

		var file2 = NewFile();
		await Assert.ThrowsAsync<UnauthorizedAccessException>(
			() => svc.ObliterateNodeAsync(new() { file2 }, token, useRecycleBin: true));
	}

	[Fact]
	public async Task RecycleBin_Stages_SetsStagedBytes_AndIsRecoverable()
	{
		var svc = CreateService();
		var file = NewFile("123456"); // 6 bytes
		var token = await TokenFor(svc, file);

		var result = await svc.ObliterateNodeAsync(new() { file }, token, useRecycleBin: true);

		Assert.False(File.Exists(file));
		Assert.True(result.RecycleBinUsed);
		Assert.True(result.Recoverable);
		Assert.Equal(1, result.DeletedFiles);
		Assert.Equal(6, result.StagedBytes);
		Assert.Equal(0, result.FreedBytes);
		Assert.NotNull(svc.PeekUndo()); // recoverable op is peekable
		Assert.Single(svc.GetUndoHistory());
	}

	[Fact]
	public async Task Permanent_Deletes_SetsFreedBytes_NotRecoverable_NotPeekable()
	{
		var svc = CreateService();
		var file = NewFile("123456");
		var token = await TokenFor(svc, file);

		var result = await svc.ObliterateNodeAsync(new() { file }, token, useRecycleBin: false);

		Assert.False(File.Exists(file));
		Assert.False(result.Recoverable);
		Assert.Equal(6, result.FreedBytes);
		Assert.Equal(0, result.StagedBytes);
		Assert.Null(svc.PeekUndo()); // permanent op is filtered out of peek
		Assert.Single(svc.GetUndoHistory()); // still recorded in history
	}

	[Fact]
	public async Task GhostPath_IsSkipped()
	{
		var svc = CreateService();
		var ghost = Path.Combine(_workDir, "does_not_exist.txt");
		var token = await TokenFor(svc, ghost);

		var result = await svc.ObliterateNodeAsync(new() { ghost }, token, useRecycleBin: false);

		Assert.Equal(1, result.SkippedFiles);
		Assert.Equal(0, result.DeletedFiles);
	}

	[Fact]
	public async Task Undo_RestoresFile_ToOriginalPath()
	{
		var svc = CreateService();
		var file = NewFile("payload");
		var token = await TokenFor(svc, file);
		await svc.ObliterateNodeAsync(new() { file }, token, useRecycleBin: true);

		var undo = svc.UndoNuke();

		Assert.NotNull(undo);
		Assert.Equal(1, undo!.DeletedFiles); // reused field = restored count
		Assert.Equal(0, undo.SkippedFiles);

		// Allow a brief moment for file system propagation in CI
		for (int i = 0; i < 10 && !File.Exists(file); i++)
			await Task.Delay(20);

		if (!File.Exists(file))
		{
			var files = Directory.GetFiles(Path.GetDirectoryName(file)!);
			Assert.Fail($"File not found at {file}. Files in dir: {string.Join(", ", files)}");
		}

		Assert.True(File.Exists(file));
		Assert.Equal("payload", File.ReadAllText(file));
		Assert.Null(svc.PeekUndo()); // op consumed
	}

	[Fact]
	public async Task Undo_SkipsPermanentOnTop_RestoresRecoverableBeneath()
	{
		var svc = CreateService();
		var recoverable = NewFile("keepme");
		var permanent = NewFile("byebye");

		var t1 = await TokenFor(svc, recoverable);
		await svc.ObliterateNodeAsync(new() { recoverable }, t1, useRecycleBin: true); // op A (bottom)

		var t2 = await TokenFor(svc, permanent);
		await svc.ObliterateNodeAsync(new() { permanent }, t2, useRecycleBin: false); // op B (top, permanent)

		var peeked = svc.PeekUndo();
		Assert.NotNull(peeked);
		Assert.True(peeked!.UsedRecycleBin); // skipped the permanent op

		svc.UndoNuke();
		Assert.True(File.Exists(recoverable)); // recoverable restored
		Assert.False(File.Exists(permanent)); // permanent stays gone
	}

	[Fact]
	public async Task Undo_RestoreCollision_RestoresToUniquePath()
	{
		var svc = CreateService();
		var file = NewFile("original");
		var token = await TokenFor(svc, file);
		await svc.ObliterateNodeAsync(new() { file }, token, useRecycleBin: true);

		// Recreate a different file at the original path before undo
		File.WriteAllText(file, "new occupant");

		svc.UndoNuke();

		Assert.Equal("new occupant", File.ReadAllText(file)); // current file untouched
		var dir = Path.GetDirectoryName(file)!;
		var name = Path.GetFileNameWithoutExtension(file);
		var restored = Path.Combine(dir, $"{name}_Restored_1.txt");
		Assert.True(File.Exists(restored));
		Assert.Equal("original", File.ReadAllText(restored));
	}

	[Fact]
	public async Task Clear_EmptiesStack_AndRemovesStaging()
	{
		var svc = CreateService();
		var file = NewFile();
		var token = await TokenFor(svc, file);
		var result = await svc.ObliterateNodeAsync(new() { file }, token, useRecycleBin: true);

		svc.ClearUndoStack();

		Assert.Empty(svc.GetUndoHistory());
		Assert.False(Directory.Exists(Path.Combine(_stagingBase, ".gsanalyzer_trash", result.OperationId)));
	}

	[Fact]
	public async Task UndoStack_CapsAtFive_EvictsOldest_AndCleansItsStaging()
	{
		var svc = CreateService();
		string? oldestOpId = null;

		for (int i = 0; i < 6; i++)
		{
			var file = NewFile();
			var token = await TokenFor(svc, file);
			var result = await svc.ObliterateNodeAsync(new() { file }, token, useRecycleBin: true);
			oldestOpId ??= result.OperationId; // first one = oldest, should be evicted
		}

		Assert.Equal(5, svc.GetUndoHistory().Count);
		Assert.False(Directory.Exists(Path.Combine(_stagingBase, ".gsanalyzer_trash", oldestOpId!)));
	}
}
