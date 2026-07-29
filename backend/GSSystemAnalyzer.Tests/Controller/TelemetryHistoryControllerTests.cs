using GSSystemAnalyzer.Controllers;
using GSSystemAnalyzer.Interfaces;
using GSSystemAnalyzer.Models;
using Microsoft.AspNetCore.Mvc;
using Moq;

namespace GSSystemAnalyzer.Tests.Controller
{
	public class TelemetryHistoryControllerTests
	{
		private readonly Mock<ITelemetryHistoryBuffer> _mockBuffer;
		private readonly TelemetryHistoryController _controller;

		public TelemetryHistoryControllerTests()
		{
			_mockBuffer = new Mock<ITelemetryHistoryBuffer>();
			_controller = new TelemetryHistoryController(_mockBuffer.Object);

			_mockBuffer.Setup(b => b.GetSupportedMetrics())
				.Returns(new List<string> { "cpu", "ram", "ram_percent", "thermal_cpu_package" }.AsReadOnly());
		}

		[Fact]
		public void ValidMetric_ReturnsOkWithData()
		{
			var response = new TelemetryHistoryResponse
			{
				Metric = "cpu",
				Minutes = 5,
				Unit = "%",
				Points = new List<TelemetryPoint>
				{
					new() { Timestamp = DateTime.UtcNow, Value = 42.5 }
				},
				Stats = new TelemetryStats { Min = 42.5, Max = 42.5, Avg = 42.5, Current = 42.5 }
			};

			_mockBuffer.Setup(b => b.GetHistory("cpu", 5)).Returns(response);

			var result = _controller.GetHistory("cpu", 5) as OkObjectResult;

			Assert.NotNull(result);
			Assert.Equal(200, result!.StatusCode);

			var body = result.Value as TelemetryHistoryResponse;
			Assert.NotNull(body);
			Assert.Equal("cpu", body!.Metric);
			Assert.Single(body.Points);
		}

		[Fact]
		public void UnknownMetric_ReturnsBadRequest()
		{
			_mockBuffer.Setup(b => b.GetHistory("fake", 5)).Returns((TelemetryHistoryResponse?)null);

			var result = _controller.GetHistory("fake", 5) as BadRequestObjectResult;

			Assert.NotNull(result);
			Assert.Equal(400, result!.StatusCode);
		}

		[Fact]
		public void EmptyMetricParam_ReturnsBadRequest()
		{
			var result = _controller.GetHistory("", 5) as BadRequestObjectResult;

			Assert.NotNull(result);
			Assert.Equal(400, result!.StatusCode);
		}

		[Fact]
		public void DefaultMinutes_Is5()
		{
			var response = new TelemetryHistoryResponse
			{
				Metric = "cpu",
				Minutes = 5,
				Unit = "%",
				Points = new List<TelemetryPoint>(),
				Stats = new TelemetryStats()
			};

			_mockBuffer.Setup(b => b.GetHistory("cpu", 5)).Returns(response);

			var result = _controller.GetHistory("cpu") as OkObjectResult;

			Assert.NotNull(result);
			_mockBuffer.Verify(b => b.GetHistory("cpu", 5), Times.Once);
		}

		[Theory]
		[InlineData(-5, 1)]   // Below min → clamped to 1
		[InlineData(0, 1)]    // Zero → clamped to 1
		[InlineData(100, 60)] // Above max → clamped to 60
		public void MinutesClamped_ToValidRange(int input, int expected)
		{
			var response = new TelemetryHistoryResponse
			{
				Metric = "cpu",
				Minutes = expected,
				Unit = "%",
				Points = new List<TelemetryPoint>(),
				Stats = new TelemetryStats()
			};

			_mockBuffer.Setup(b => b.GetHistory("cpu", expected)).Returns(response);

			var result = _controller.GetHistory("cpu", input) as OkObjectResult;

			Assert.NotNull(result);
			_mockBuffer.Verify(b => b.GetHistory("cpu", expected), Times.Once);
		}

		[Fact]
		public void MetricIsCaseInsensitive()
		{
			var response = new TelemetryHistoryResponse
			{
				Metric = "cpu",
				Minutes = 5,
				Unit = "%",
				Points = new List<TelemetryPoint>(),
				Stats = new TelemetryStats()
			};

			_mockBuffer.Setup(b => b.GetHistory("cpu", 5)).Returns(response);

			var result = _controller.GetHistory("CPU", 5) as OkObjectResult;

			Assert.NotNull(result);
			Assert.Equal(200, result!.StatusCode);
		}

		[Fact]
		public void GetSupportedMetrics_ReturnsMetricsList()
		{
			var result = _controller.GetSupportedMetrics() as OkObjectResult;

			Assert.NotNull(result);
			Assert.Equal(200, result!.StatusCode);
		}
	}
}
