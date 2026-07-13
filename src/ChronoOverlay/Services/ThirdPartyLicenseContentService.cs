using System.IO;
using System.Windows;

namespace ChronoOverlay.Services;

public sealed record ThirdPartyFontLicense(
    string FontName,
    string CopyrightHolder,
    string UpstreamUrl,
    string ResourcePath);

public sealed class ThirdPartyLicenseContentService
{
    public static IReadOnlyList<ThirdPartyFontLicense> FontLicenses { get; } =
    [
        new(
            "JetBrains Mono",
            "Copyright 2020 The JetBrains Mono Project Authors",
            "https://github.com/JetBrains/JetBrainsMono",
            "Assets/Fonts/Licenses/JetBrainsMono-OFL-1.1.txt"),
        new(
            "IBM Plex Mono",
            "Copyright © 2017 IBM Corp. — Reserved Font Name: Plex",
            "https://github.com/IBM/plex",
            "Assets/Fonts/Licenses/IBMPlexMono-OFL-1.1.txt"),
        new(
            "Space Mono",
            "Copyright 2016 The Space Mono Project Authors",
            "https://github.com/googlefonts/spacemono",
            "Assets/Fonts/Licenses/SpaceMono-OFL-1.1.txt"),
    ];

    private const string NoticesResourcePath = "Assets/Licenses/THIRD-PARTY-NOTICES.md";
    private readonly Func<string, string> _resourceReader;

    public ThirdPartyLicenseContentService(Func<string, string>? resourceReader = null)
    {
        _resourceReader = resourceReader ?? ReadApplicationResource;
    }

    public string BuildContent()
    {
        List<string> sections =
        [
            "ChronoOverlay 第三方许可",
            "",
            "以下字体作为应用资源离线随 ChronoOverlay 分发。每一节均包含版权所有者、上游地址和完整 SIL Open Font License 1.1 文本。",
            "",
            "第三方声明",
            "============",
            _resourceReader(NoticesResourcePath).Trim(),
        ];

        foreach (ThirdPartyFontLicense font in FontLicenses)
        {
            sections.AddRange(
            [
                "",
                "",
                font.FontName,
                new string('=', font.FontName.Length),
                font.CopyrightHolder,
                $"上游地址：{font.UpstreamUrl}",
                "",
                _resourceReader(font.ResourcePath).Trim(),
            ]);
        }

        return string.Join(Environment.NewLine, sections) + Environment.NewLine;
    }

    private static string ReadApplicationResource(string path)
    {
        _ = System.IO.Packaging.PackUriHelper.CreatePartUri(new Uri("/license-bootstrap", UriKind.Relative));
        Uri resourceUri = new($"pack://application:,,,/ChronoOverlay;component/{path}", UriKind.Absolute);
        System.Windows.Resources.StreamResourceInfo resource = System.Windows.Application.GetResourceStream(resourceUri)
            ?? throw new InvalidOperationException($"Embedded license resource was not found: {path}");
        using StreamReader reader = new(resource.Stream);
        return reader.ReadToEnd();
    }
}
