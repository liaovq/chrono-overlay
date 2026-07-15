[CmdletBinding()]
param(
    [string]$AssemblyPath = 'src/ChronoOverlay/bin/Release/net8.0-windows/win-x64/ChronoOverlay.dll'
)

$expectedHashes = [ordered]@{
    'IBMPlexMono-Regular.ttf' = '7C6FBDDCA4B700BE918F5F6183D9BD4464FA427FE435F0B480D77FE2BB8C5A43'
    'JetBrainsMono-Regular.ttf' = 'A0BF60EF0F83C5ED4D7A75D45838548B1F6873372DFAC88F71804491898D138F'
    'SpaceMono-Regular.ttf' = '95837E182BAEEADA83368F7748DB28357F0A1B75C6B84FF7065B5EDF933C8E18'
    'MajorMonoDisplay-Regular.ttf' = '3F2EE71C2B8464F065BF960A148BE81D68FCC13385DF87737928F948208DFFC6'
    'VT323-Regular.ttf' = 'CF4DE751ADA78CEAC033DBE16A687742939995B77BC2A052AE17A4957958594D'
    'IBMPlexMono-OFL-1.1.txt' = '37784B44044A4FFD9256702B7C0982C37E5C8887BA90C6DCA0479AEA93DC898D'
    'JetBrainsMono-OFL-1.1.txt' = '30F0C136E3C88E422D0791ACD97238870F9054A9729BC34CF2FF0D4ED8CAC4AD'
    'SpaceMono-OFL-1.1.txt' = '8E4EE42B2553E1E01504E61CB0D46D148CD8C9E5EACAA3622A7DF2D4F2955B9F'
    'MajorMonoDisplay-OFL-1.1.txt' = '7D05D13C4DA1C89CC6C5BFF0DF9543DE0835B467750ED44CAA8C5D5CB3C2F394'
    'VT323-OFL-1.1.txt' = '27D9AF34210253E7CA1251FBACE86C6F65B40031D6CE1A75493A1B2093631298'
}
$expectedResourceKeys = @(
    'assets/fonts/ibmplexmono-regular.ttf'
    'assets/fonts/jetbrainsmono-regular.ttf'
    'assets/fonts/majormonodisplay-regular.ttf'
    'assets/fonts/licenses/ibmplexmono-ofl-1.1.txt'
    'assets/fonts/licenses/jetbrainsmono-ofl-1.1.txt'
    'assets/fonts/licenses/majormonodisplay-ofl-1.1.txt'
    'assets/fonts/licenses/spacemono-ofl-1.1.txt'
    'assets/fonts/licenses/vt323-ofl-1.1.txt'
    'assets/fonts/spacemono-regular.ttf'
    'assets/fonts/vt323-regular.ttf'
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
