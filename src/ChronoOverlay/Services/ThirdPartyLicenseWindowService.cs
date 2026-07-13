using ChronoOverlay.Views;

namespace ChronoOverlay.Services;

public interface IThirdPartyLicenseWindow
{
    event EventHandler? Closed;

    bool IsVisible { get; }

    void Show();

    bool Activate();

    void Close();
}

public sealed class ThirdPartyLicenseWindowService : IDisposable
{
    private readonly Func<IThirdPartyLicenseWindow> _windowFactory;
    private IThirdPartyLicenseWindow? _window;

    public ThirdPartyLicenseWindowService(ThirdPartyLicenseContentService contentService)
        : this(() => new ThirdPartyLicenseWindow(contentService.BuildContent()))
    {
    }

    public ThirdPartyLicenseWindowService(Func<IThirdPartyLicenseWindow> windowFactory)
    {
        _windowFactory = windowFactory;
    }

    public bool IsOpen => _window is not null;

    public void Show()
    {
        if (_window is not null)
        {
            if (!_window.IsVisible)
            {
                _window.Show();
            }

            _window.Activate();
            return;
        }

        IThirdPartyLicenseWindow window = _windowFactory();
        _window = window;
        window.Closed += OnWindowClosed;
        window.Show();
        window.Activate();
    }

    private void OnWindowClosed(object? sender, EventArgs eventArgs)
    {
        if (sender is IThirdPartyLicenseWindow window)
        {
            window.Closed -= OnWindowClosed;
            if (ReferenceEquals(_window, window))
            {
                _window = null;
            }
        }
    }

    public void Dispose()
    {
        if (_window is null)
        {
            return;
        }

        IThirdPartyLicenseWindow window = _window;
        _window = null;
        window.Closed -= OnWindowClosed;
        window.Close();
    }
}
