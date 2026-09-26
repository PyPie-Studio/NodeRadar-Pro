using Avalonia.Platform;

namespace NodeRadarPro.Tests.UI;

public class AssetLoadingTests
{
    private readonly StandardAssetLoader _loader;

    public AssetLoadingTests()
    {
        _loader = new StandardAssetLoader(typeof(NodeRadarPro.App).Assembly);
    }

    [Theory]
    [InlineData("avares://NodeRadarPro/Resources/NodeRadar Pro Icon.ico")]
    [InlineData("avares://NodeRadarPro/Resources/NodeRadar Pro Icon.png")]
    [InlineData("avares://NodeRadarPro/Resources/NodeRadar Pro.png")]
    public void EmbeddedResource_CanBeLoadedSuccessfully(string uriString)
    {
        var uri = new Uri(uriString);
        Assert.True(_loader.Exists(uri), $"Resource does not exist: {uriString}");
        using Stream stream = _loader.Open(uri);
        Assert.NotNull(stream);
        Assert.True(stream.Length > 0, $"Resource stream is empty: {uriString}");
    }

    [Fact]
    public void LegacyResourceUri_WithSpaceInAssemblyName_ThrowsOrFails()
    {
        var ex = Record.Exception(() =>
        {
            var uri = new Uri("avares://NodeRadar Pro/Resources/NodeRadar Pro Icon.ico");
            _loader.Open(uri);
        });
        Assert.NotNull(ex);
    }
}
