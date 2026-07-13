using System.Collections;
using System.Resources;
using System.Threading;
using System.Windows.Controls;
using ChronoOverlay.Models;
using ChronoOverlay.Services;
using ChronoOverlay.Views;

namespace ChronoOverlay.Tests;

public sealed class ThirdPartyLicenseTests
{
    private static readonly string[] ExpectedResourceKeys =
    [
        "assets/fonts/licenses/ibmplexmono-ofl-1.1.txt",
        "assets/fonts/licenses/jetbrainsmono-ofl-1.1.txt",
        "assets/fonts/licenses/spacemono-ofl-1.1.txt",
        "assets/licenses/third-party-notices.md",
    ];

    [Fact]
    public void ThirdPartyLicenseResourcesAreEmbedded()
    {
        System.Reflection.Assembly assembly = typeof(ThirdPartyLicenseContentService).Assembly;
        string resourceName = Assert.Single(assembly.GetManifestResourceNames(), name => name.EndsWith(".g.resources", StringComparison.Ordinal));
        using Stream stream = Assert.IsAssignableFrom<Stream>(assembly.GetManifestResourceStream(resourceName));
        using ResourceReader reader = new(stream);
        List<string> resourceKeys = [];
        IDictionaryEnumerator enumerator = reader.GetEnumerator();
        while (enumerator.MoveNext())
        {
            resourceKeys.Add(Assert.IsType<string>(enumerator.Key));
        }

        foreach (string expected in ExpectedResourceKeys)
        {
            Assert.Contains(expected, resourceKeys);
        }
    }

    [Fact]
    public void LicenseContentContainsFontNamesOwnersUpstreamsAndFullOflText()
    {
        ThirdPartyLicenseContentService service = new(path => path.EndsWith("THIRD-PARTY-NOTICES.md", StringComparison.Ordinal)
            ? "ChronoOverlay bundled font notices"
            : "SIL OPEN FONT LICENSE Version 1.1 - 26 February 2007\nComplete license body");

        string content = service.BuildContent();

        foreach (ThirdPartyFontLicense font in ThirdPartyLicenseContentService.FontLicenses)
        {
            Assert.Contains(font.FontName, content, StringComparison.Ordinal);
            Assert.Contains(font.CopyrightHolder, content, StringComparison.Ordinal);
            Assert.Contains(font.UpstreamUrl, content, StringComparison.Ordinal);
        }

        Assert.Equal(3, CountOccurrences(content, "SIL OPEN FONT LICENSE Version 1.1"));
    }

    [Fact]
    public void EmbeddedLicenseContentCanBeReadCompletelyOffline()
    {
        RunOnStaThread(() =>
        {
            string content = new ThirdPartyLicenseContentService().BuildContent();

            Assert.Contains("JetBrains Mono", content, StringComparison.Ordinal);
            Assert.Contains("IBM Plex Mono", content, StringComparison.Ordinal);
            Assert.Contains("Space Mono", content, StringComparison.Ordinal);
            Assert.Equal(3, CountOccurrences(content, "SIL OPEN FONT LICENSE Version 1.1"));
        });
    }

    [Fact]
    public void TrayMenuContainsThirdPartyLicenses()
    {
        Assert.Contains(TrayIconService.MenuText.ThirdPartyLicenses, TrayIconService.MenuItemTexts);
    }

    [Fact]
    public void RepeatedShowReusesTheSameLicenseWindow()
    {
        List<FakeLicenseWindow> created = [];
        using ThirdPartyLicenseWindowService service = new(() =>
        {
            FakeLicenseWindow window = new();
            created.Add(window);
            return window;
        });

        service.Show();
        service.Show();

        FakeLicenseWindow window = Assert.Single(created);
        Assert.Equal(1, window.ShowCount);
        Assert.Equal(2, window.ActivateCount);
        Assert.True(service.IsOpen);
    }

    [Fact]
    public void ShowingLicenseWindowDoesNotChangeLockState()
    {
        AppSettings settings = new() { IsLocked = true };
        using ThirdPartyLicenseWindowService service = new(() => new FakeLicenseWindow());

        service.Show();

        Assert.True(settings.IsLocked);
    }

    [Fact]
    public void ClosingLicenseWindowLeavesApplicationServiceUsable()
    {
        List<FakeLicenseWindow> created = [];
        using ThirdPartyLicenseWindowService service = new(() =>
        {
            FakeLicenseWindow window = new();
            created.Add(window);
            return window;
        });

        service.Show();
        created[0].SimulateUserClose();
        Assert.False(service.IsOpen);

        service.Show();

        Assert.Equal(2, created.Count);
        Assert.True(service.IsOpen);
    }

    [Fact]
    public void LicenseWindowIsLocalReadonlySelectableAndHiddenFromTaskbar()
    {
        RunOnStaThread(() =>
        {
            ThirdPartyLicenseWindow window = new("JetBrains Mono\nSIL OPEN FONT LICENSE Version 1.1");
            TextBox textBox = Assert.IsType<TextBox>(window.FindName("LicenseTextBox"));

            Assert.Equal("ChronoOverlay 第三方许可", window.Title);
            Assert.False(window.ShowInTaskbar);
            Assert.True(textBox.IsReadOnly);
            Assert.Equal("JetBrains Mono\nSIL OPEN FONT LICENSE Version 1.1", window.LicenseContent);

            window.Close();
        });
    }

    private static int CountOccurrences(string value, string search)
    {
        int count = 0;
        int index = 0;
        while ((index = value.IndexOf(search, index, StringComparison.Ordinal)) >= 0)
        {
            count++;
            index += search.Length;
        }

        return count;
    }

    private static void RunOnStaThread(Action action)
    {
        Exception? failure = null;
        Thread thread = new(() =>
        {
            try
            {
                action();
            }
            catch (Exception exception)
            {
                failure = exception;
            }
        });
        thread.SetApartmentState(ApartmentState.STA);
        thread.Start();
        Assert.True(thread.Join(TimeSpan.FromSeconds(10)), "STA window test timed out.");
        if (failure is not null)
        {
            throw failure;
        }
    }

    private sealed class FakeLicenseWindow : IThirdPartyLicenseWindow
    {
        public event EventHandler? Closed;

        public bool IsVisible { get; private set; }

        public int ShowCount { get; private set; }

        public int ActivateCount { get; private set; }

        public void Show()
        {
            ShowCount++;
            IsVisible = true;
        }

        public bool Activate()
        {
            ActivateCount++;
            return true;
        }

        public void Close()
        {
            IsVisible = false;
            Closed?.Invoke(this, EventArgs.Empty);
        }

        public void SimulateUserClose() => Close();
    }
}
