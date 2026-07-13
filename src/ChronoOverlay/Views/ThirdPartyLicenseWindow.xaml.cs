using System.Windows;
using ChronoOverlay.Services;

namespace ChronoOverlay.Views;

public partial class ThirdPartyLicenseWindow : Window, IThirdPartyLicenseWindow
{
    public ThirdPartyLicenseWindow(string content)
    {
        InitializeComponent();
        LicenseTextBox.Text = content;
    }

    public string LicenseContent => LicenseTextBox.Text;

    private void OnCloseClick(object sender, RoutedEventArgs eventArgs) => Close();
}
