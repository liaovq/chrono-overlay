using System.Drawing;

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
