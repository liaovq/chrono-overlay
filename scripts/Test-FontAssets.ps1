[CmdletBinding()]
param(
    [string]$AssemblyPath = 'src/ChronoOverlay/bin/Release/net8.0-windows/win-x64/ChronoOverlay.dll'
)

$expectedHashes = [ordered]@{
    'IBMPlexMono-Regular.ttf' = '7C6FBDDCA4B700BE918F5F6183D9BD4464FA427FE435F0B480D77FE2BB8C5A43'
    'JetBrainsMono-Regular.ttf' = 'A0BF60EF0F83C5ED4D7A75D45838548B1F6873372DFAC88F71804491898D138F'
    'SpaceMono-Regular.ttf' = '95837E182BAEEADA83368F7748DB28357F0A1B75C6B84FF7065B5EDF933C8E18'
    'IBMPlexMono-OFL-1.1.txt' = '7E6B2818EDBD8F6A01AE80641CC8F16A51080D08FB4E532BE3A0B6F74ADB07DA'
    'JetBrainsMono-OFL-1.1.txt' = '30F0C136E3C88E422D0791ACD97238870F9054A9729BC34CF2FF0D4ED8CAC4AD'
    'SpaceMono-OFL-1.1.txt' = '8E4EE42B2553E1E01504E61CB0D46D148CD8C9E5EACAA3622A7DF2D4F2955B9F'
}
$expectedResourceKeys = @(
    'assets/fonts/ibmplexmono-regular.ttf'
    'assets/fonts/jetbrainsmono-regular.ttf'
    'assets/fonts/licenses/ibmplexmono-ofl-1.1.txt'
    'assets/fonts/licenses/jetbrainsmono-ofl-1.1.txt'
    'assets/fonts/licenses/spacemono-ofl-1.1.txt'
    'assets/fonts/spacemono-regular.ttf'
    'assets/licenses/third-party-notices.md'
)

$fontDirectory = Resolve-Path 'src/ChronoOverlay/Assets/Fonts'
$actualFiles = @(
    Get-ChildItem -LiteralPath $fontDirectory -File -Filter '*.ttf'
    Get-ChildItem -LiteralPath (Join-Path $fontDirectory 'Licenses') -File -Filter '*.txt'
)
$actualFileNames = @($actualFiles.Name | Sort-Object)
$expectedFileNames = @($expectedHashes.Keys | Sort-Object)
if (Compare-Object $expectedFileNames $actualFileNames) {
    throw "Bundled font or license files differ from the expected set: $($actualFileNames -join ', ')."
}

foreach ($file in $actualFiles | Sort-Object Name) {
    [string]$actualHash = (Get-FileHash -LiteralPath $file.FullName -Algorithm SHA256).Hash
    [string]$expectedHash = $expectedHashes[$file.Name]
    Write-Output "$($file.Name) SHA-256 $actualHash"
    if ($actualHash -ne $expectedHash) {
        throw "$($file.Name) SHA-256 mismatch. Expected $expectedHash, actual $actualHash."
    }
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
            Where-Object {
                $_ -like 'assets/fonts/*.ttf' -or
                $_ -like 'assets/fonts/licenses/*.txt' -or
                $_ -eq 'assets/licenses/third-party-notices.md'
            } |
            Sort-Object
    )
}
finally {
    $reader.Dispose()
}

if (Compare-Object $expectedResourceKeys $actualResourceKeys) {
    throw "Embedded font/license resources differ from the expected set: $($actualResourceKeys -join ', ')."
}

Write-Output "Embedded font and license resources verified: $($expectedResourceKeys -join ', ')."
