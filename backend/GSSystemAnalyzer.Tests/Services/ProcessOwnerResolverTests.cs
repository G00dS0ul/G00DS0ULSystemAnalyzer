using GSSystemAnalyzer.Services;
using Xunit;

namespace GSSystemAnalyzer.Tests.Services;

public class ProcessOwnerResolverTests
{
	private readonly ProcessOwnerResolver _resolver = new();

	[Fact]
	public void Resolve_UnknownPid_ReturnsSYSTEM()
	{
		var result = _resolver.Resolve(int.MaxValue);
		Assert.Equal("SYSTEM", result);
	}

	[Fact]
	public void Resolve_NegativePid_ReturnsSYSTEM()
	{
		var result = _resolver.Resolve(-1);
		Assert.Equal("SYSTEM", result);
	}

	[Fact]
	public void RefreshCache_DoesNotThrowOnAnyPlatform()
	{
		var ex = Record.Exception(() => _resolver.RefreshCache());
		Assert.Null(ex);
	}

	[Fact]
	public void Resolve_AfterRefreshCache_StillReturnsSYSTEMForUnknownPid()
	{
		_resolver.RefreshCache();
		var result = _resolver.Resolve(int.MaxValue);
		Assert.Equal("SYSTEM", result);
	}

	[Fact]
	public void Resolve_CalledRepeatedly_DoesNotThrow()
	{
		var ex = Record.Exception(() =>
		{
			for (int i = 0; i < 10; i++)
				_resolver.Resolve(99_999_000 + i);
		});
		Assert.Null(ex);
	}
}
