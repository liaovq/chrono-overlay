namespace ChronoOverlay.Tests;

public sealed class ClockWindowMarkupTests
{
    private static string Markup => File.ReadAllText(
        Path.Combine(AppContext.BaseDirectory, "TestAssets", "ClockWindow.xaml"));

    [Fact]
    public void IconsUseWindows10CompatibleMdl2Font()
    {
        Assert.DoesNotContain("Segoe Fluent Icons", Markup, StringComparison.Ordinal);
        Assert.Equal(2, CountOccurrences(Markup, "Segoe MDL2 Assets"));
        Assert.Contains("&#xE70D;", Markup, StringComparison.Ordinal);
        Assert.Contains("&#xE72E;", Markup, StringComparison.Ordinal);
    }

    [Fact]
    public void VisiblePanelUsesOverlayOutlineInsteadOfInsetBorderLayout()
    {
        Assert.Contains("<Grid x:Name=\"PanelChrome\"", Markup, StringComparison.Ordinal);
        Assert.Contains("<Grid x:Name=\"PanelContent\">", Markup, StringComparison.Ordinal);
        Assert.Contains("<Border x:Name=\"PanelOutline\"", Markup, StringComparison.Ordinal);
        Assert.DoesNotContain("<Border x:Name=\"PanelChrome\"", Markup, StringComparison.Ordinal);
    }

    [Theory]
    [InlineData("纯黑")]
    [InlineData("纯白")]
    public void ColorSwatchesExposeAutomationNames(string accessibleName)
    {
        Assert.Contains(
            $"AutomationProperties.Name=\"{accessibleName}\"",
            Markup,
            StringComparison.Ordinal);
    }

    [Fact]
    public void CustomSlidersExposeVisibleKeyboardFocusRings()
    {
        Assert.Equal(2, CountOccurrences(Markup, "x:Name=\"KeyboardFocusRing\""));
        Assert.Equal(2, CountOccurrences(Markup, "TargetName=\"KeyboardFocusRing\""));
        Assert.Contains("Property=\"IsKeyboardFocused\" Value=\"True\"", Markup, StringComparison.Ordinal);
    }

    private static int CountOccurrences(string value, string token) =>
        value.Split(token, StringSplitOptions.None).Length - 1;
}
