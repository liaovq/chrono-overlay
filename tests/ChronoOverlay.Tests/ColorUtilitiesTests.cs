using ChronoOverlay.Utilities;

namespace ChronoOverlay.Tests;

public sealed class ColorUtilitiesTests
{
    [Theory]
    [InlineData(0, 0)]
    [InlineData(360, 0)]
    [InlineData(-30, 330)]
    [InlineData(750, 30)]
    public void NormalizeHueWrapsIntoColorWheel(double input, double expected)
    {
        Assert.Equal(expected, ColorUtilities.NormalizeHue(input));
    }

    [Fact]
    public void HueConversionProducesExpectedPrimaryOrdering()
    {
        var red = ColorUtilities.FromHue(0);
        var green = ColorUtilities.FromHue(120);
        var blue = ColorUtilities.FromHue(240);

        Assert.True(red.R > red.G && red.R > red.B);
        Assert.True(green.G > green.R && green.G > green.B);
        Assert.True(blue.B > blue.R && blue.B > blue.G);
    }

    [Theory]
    [InlineData("abcdef", "#ABCDEF")]
    [InlineData("#123456", "#123456")]
    [InlineData("bad", "#FFFFFF")]
    public void HexValuesAreNormalized(string input, string expected)
    {
        Assert.Equal(expected, ColorUtilities.NormalizeHex(input, "#FFFFFF"));
    }
}
