using System.Windows;
using System.Windows.Input;
using ChronoOverlay.Services;

namespace ChronoOverlay.Views;

public partial class LockedHotspotWindow : Window
{
    public LockedHotspotWindow()
    {
        InitializeComponent();
        SourceInitialized += (_, _) => WindowStyleService.ConfigureToolWindow(this, true);
    }

    public event EventHandler? UnlockRequested;

    private void OnMouseDoubleClick(object sender, MouseButtonEventArgs eventArgs)
    {
        if (eventArgs.ChangedButton == MouseButton.Left)
        {
            UnlockRequested?.Invoke(this, EventArgs.Empty);
            eventArgs.Handled = true;
        }
    }
}
