using System.Drawing;
using System.Windows;

namespace ChronoOverlay.Services;

public enum ControlPanelPlacement
{
    Below,
    Above,
}

public static class ControlPanelPlacementMath
{
    public static ControlPanelPlacement Resolve(
        Rectangle workArea,
        Rectangle clockBounds,
        int panelHeight,
        int gap)
    {
        ArgumentOutOfRangeException.ThrowIfNegative(panelHeight);
        ArgumentOutOfRangeException.ThrowIfNegative(gap);

        int required = panelHeight + gap;
        int availableBelow = Math.Max(0, workArea.Bottom - clockBounds.Bottom);
        int availableAbove = Math.Max(0, clockBounds.Top - workArea.Top);

        if (availableBelow >= required)
        {
            return ControlPanelPlacement.Below;
        }

        if (availableAbove >= required)
        {
            return ControlPanelPlacement.Above;
        }

        return availableAbove > availableBelow
            ? ControlPanelPlacement.Above
            : ControlPanelPlacement.Below;
    }
}

public static class ControlPanelShadowLayout
{
    public const double HorizontalInset = 12;
    public const double OuterVerticalInset = 20;
    public const double VisiblePanelWidth = 420;
    public const double VisiblePanelHeight = 166;
    public const double OuterWidth = VisiblePanelWidth + (HorizontalInset * 2);

    public static Thickness GetChromeMargin(ControlPanelPlacement placement) => placement switch
    {
        ControlPanelPlacement.Below => new Thickness(HorizontalInset, 0, HorizontalInset, OuterVerticalInset),
        ControlPanelPlacement.Above => new Thickness(HorizontalInset, OuterVerticalInset, HorizontalInset, 0),
        _ => throw new ArgumentOutOfRangeException(nameof(placement), placement, null),
    };
}
