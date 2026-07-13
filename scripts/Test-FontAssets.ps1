[CmdletBinding()]
param(
    [string]$AssemblyPath = 'src/ChronoOverlay/bin/Release/net8.0-windows/win-x64/ChronoOverlay.dll'
)

$expectedFontFiles = @(
    'IBMPlexMono-Regular.ttf'
    'JetBrainsMono-Regular.ttf'
    'SpaceMono-Regular.ttf'
)
$expectedResourceKeys = @(
    'assets/fonts/ibmplexmono-regular.ttf'
    'assets/fonts/jetbrainsmono-regular.ttf'
    'assets/fonts/spacemono-regular.ttf'
)
$expectedLicenses = @(
    'IBMPlexMono-OFL-1.1.txt'
    'JetBrainsMono-OFL-1.1.txt'
    'SpaceMono-OFL-1.1.txt'
)

$fontDirectory = Resolve-Path 'src/ChronoOverlay/Assets/Fonts'
$actualFontFiles = @(
    Get-ChildItem -LiteralPath $fontDirectory -File -Filter '*.ttf' |
        Select-Object -ExpandProperty Name |
        Sort-Object
)
$actualLicenses = @(
    Get-ChildItem -LiteralPath (Join-Path $fontDirectory 'Licenses') -File -Filter '*.txt' |
        Select-Object -ExpandProperty Name |
        Sort-Object
)

if (Compare-Object $expectedFontFiles $actualFontFiles) {
    throw "Bundled font files differ from the expected set: $($actualFontFiles -join ', ')."
}

if (Compare-Object $expectedLicenses $actualLicenses) {
    throw "Bundled font licenses differ from the expected set: $($actualLicenses -join ', ')."
}

$resolvedAssembly = (Resolve-Path -LiteralPath $AssemblyPath).Path
$assembly = [System.Reflection.Assembly]::LoadFile($resolvedAssembly)
$resourceName = @($assembly.GetManifestResourceNames() | Where-Object { $_ -like '*.g.resources' })
if ($resourceName.Count -ne 1) {
    throw "Expected one WPF .g.resources manifest in $resolvedAssembly."
}

$reader = [System.Resources.ResourceReader]::new($assembly.GetManifestResourceStream($resourceName[0]))
try {
    $actualResourceKeys = @(
        $reader.GetEnumerator() |
            ForEach-Object { $_.Key } |
            Where-Object { $_ -like 'assets/fonts/*.ttf' } |
            Sort-Object
    )
}
finally {
    $reader.Dispose()
}

if (Compare-Object $expectedResourceKeys $actualResourceKeys) {
    throw "Embedded font resources differ from the expected set: $($actualResourceKeys -join ', ')."
}

Write-Output "Bundled font assets verified: $($expectedFontFiles -join ', ')."
