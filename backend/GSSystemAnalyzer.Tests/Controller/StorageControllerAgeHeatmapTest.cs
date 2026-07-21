using GSSystemAnalyzer.Controllers;
using GSSystemAnalyzer.Interfaces;
using GSSystemAnalyzer.Models;
using Microsoft.AspNetCore.Mvc;
using Moq;
using Xunit;

namespace GSSystemAnalyzer.Tests.Controllers
{
	public class StorageControllerAgeHeatmapTests
	{

		private static StorageController MakeController()
		{
			var diskService = new Mock<IDiskOperationService>();
			var duplicateDetector = new Mock<IDuplicateFileDetector>();
			var scopeFactory = new Mock<Microsoft.Extensions.DependencyInjection.IServiceScopeFactory>();
			return new StorageController(diskService.Object, duplicateDetector.Object, scopeFactory.Object);
		}

		private static string ExistingRoot => Path.GetTempPath().TrimEnd(Path.DirectorySeparatorChar);

		[Fact]
		public void GetAgeHeatmap_Returns200_WhenCacheHit()
		{
			var fakeResult = new AgeHeatmapResult
			{
				Root = ExistingRoot,
				Nodes = new List<AgeHeatmapNode>(),
				Summary = new Dictionary<string, AgeBucketSummary>(),
			};
			var engine = new Mock<IAgeHeatmapEngine>();
			engine.Setup(e => e.Analyze(It.IsAny<string>())).Returns(fakeResult);

			// GetAgeHeatmap(string root, [FromServices] IAgeHeatmapEngine heatmap)
			var result = MakeController().GetAgeHeatmap(ExistingRoot, engine.Object);

			Assert.IsType<OkObjectResult>(result);
		}

		[Fact]
		public void GetAgeHeatmap_Returns409_WhenNoCacheExists()
		{
			var engine = new Mock<IAgeHeatmapEngine>();
			engine.Setup(e => e.Analyze(It.IsAny<string>())).Returns((AgeHeatmapResult?)null);

			var result = MakeController().GetAgeHeatmap(ExistingRoot, engine.Object);

			Assert.IsType<ConflictObjectResult>(result);
		}

		[Fact]
		public void GetAgeHeatmap_Returns400_WhenRootIsEmpty()
		{
			var engine = new Mock<IAgeHeatmapEngine>();
			var result = MakeController().GetAgeHeatmap("", engine.Object);

			Assert.IsType<BadRequestObjectResult>(result);
		}

		[Fact]
		public void GetAgeHeatmap_Returns400_WhenRootDoesNotExist()
		{
			var engine = new Mock<IAgeHeatmapEngine>();
			var result = MakeController().GetAgeHeatmap("Z:/nonexistent/path", engine.Object);

			Assert.IsType<BadRequestObjectResult>(result);
		}
	}
}
