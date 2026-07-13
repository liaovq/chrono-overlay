[CmdletBinding()]
param(
    [string]$SiteDist
)

if ([string]::IsNullOrWhiteSpace($SiteDist)) {
    $SiteDist = Join-Path $PSScriptRoot '..\site\dist'
}

$version = & (Join-Path $PSScriptRoot 'Get-Version.ps1')
$assetName = "ChronoOverlay-v$version-win-x64.exe"
$expectedUrl = "https://github.com/liaovq/chrono-overlay/releases/download/v$version/$assetName"

$viteConfig = Get-Content -Raw -LiteralPath (Join-Path $PSScriptRoot '..\site\vite.config.mjs')
if ($viteConfig -notmatch 'Directory\.Build\.props') {
    throw 'The website must read its version from Directory.Build.props.'
}

$appSettingsPath = Join-Path $PSScriptRoot '..\src\ChronoOverlay\Models\AppSettings.cs'
$appSettingsSource = Get-Content -Raw -LiteralPath $appSettingsPath
if ($appSettingsSource -match 'AppVersion\s*\{[^}]*\}\s*=\s*"\d+\.\d+\.\d+"') {
    throw 'AppSettings must not hardcode a concrete application version default.'
}

if (-not (Test-Path -LiteralPath $SiteDist)) {
    throw "Website build output does not exist: $SiteDist"
}

$siteText = Get-ChildItem -LiteralPath $SiteDist -Recurse -File |
    Where-Object { $_.Extension -in '.html', '.js', '.css' } |
    ForEach-Object { Get-Content -Raw -LiteralPath $_.FullName }
$combined = $siteText -join "`n"

if (-not $combined.Contains("v$version")) {
    throw "Website output does not contain v$version."
}

if (-not $combined.Contains($expectedUrl)) {
    throw "Website output does not contain the expected Release URL: $expectedUrl"
}

Write-Output "Version consistency verified for v$version and $assetName."
