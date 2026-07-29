using System;
using System.IO;
using System.Threading.Tasks;
using GSSystemAnalyzer.Interfaces;
using GSSystemAnalyzer.Models.SettingDtos;
using Xunit;
using GSSystemAnalyzer.Services;
using Microsoft.Extensions.Logging.Abstractions;
using Moq;

namespace GSSystemAnalyzer.Tests.Services;

public class LargeFileHunterServiceTests : IDisposable
{
	private readonly string _tempRoot;
	private readonly LargeFileHunterService _hunterService;

	public LargeFileHunterServiceTests()
	{
		_tempRoot = Path.Combine(Path.GetTempPath(), Guid.NewGuid().ToString());
		Directory.CreateDirectory(_tempRoot);

		var mockSettings = new Mock<ISettingService>();
		mockSettings.Setup(s => s.Current)
			.Returns(AppSettingDto.GetFactoryDefaults());

		_hunterService = new LargeFileHunterService(mockSettings.Object, NullLogger<LargeFileHunterService>.Instance);
	}

	[Fact]
	public async Task GetTopLargeFilesAsync_ReturnsCorrectTopNFiles_OrderedDescending()
	{
		// Arrange
		File.WriteAllBytes(Path.Combine(_tempRoot, "file_tiny.txt"), new byte[10]);
		File.WriteAllBytes(Path.Combine(_tempRoot, "file_huge.txt"), new byte[5000]);
		File.WriteAllBytes(Path.Combine(_tempRoot, "file_medium.txt"), new byte[500]);
		File.WriteAllBytes(Path.Combine(_tempRoot, "file_large.txt"), new byte[1000]);
		File.WriteAllBytes(Path.Combine(_tempRoot, "file_small.txt"), new byte[100]);

		// Act
		var results = await _hunterService.GetTopLargeFilesAsync(_tempRoot, 3);

		// Assert
		Assert.Equal(3, results.Count);

		// First rank
		Assert.Equal(5000, results[0].SizeBytes);
		Assert.EndsWith("file_huge.txt", results[0].Path);

		// Second
		Assert.Equal(1000, results[1].SizeBytes);
		Assert.EndsWith("file_large.txt", results[1].Path);

		Assert.Equal(500, results[2].SizeBytes);
		Assert.EndsWith("file_medium.txt", results[2].Path);


	}

	public void Dispose()
	{
		// Global Cleanup!
		if (Directory.Exists(_tempRoot))
		{
			Directory.Delete(_tempRoot, true);
		}
	}
}
