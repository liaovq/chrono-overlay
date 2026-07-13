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
$desktopRuntimeUrl = 'https://dotnet.microsoft.com/download/dotnet/8.0'

$viteConfig = Get-Content -Raw -LiteralPath (Join-Path $PSScriptRoot '..\site\vite.config.mjs')
if ($viteConfig -notmatch 'Directory\.Build\.props') {
    throw 'The website must read its version from Directory.Build.props.'
}

$appSettingsPath = Join-Path $PSScriptRoot '..\src\ChronoOverlay\Models\AppSettings.cs'
$appSettingsSource = Get-Content -Raw -LiteralPath $appSettingsPath
if ($appSettingsSource -match 'AppVersion\s*\{[^}]*\}\s*=\s*"\d+\.\d+\.\d+"') {
    throw 'AppSettings must not hardcode a concrete application version default.'
}

$projectPath = Join-Path $PSScriptRoot '..\src\ChronoOverlay\ChronoOverlay.csproj'
[xml]$project = Get-Content -Raw -LiteralPath $projectPath
$projectProperties = $project.Project.PropertyGroup
if ($projectProperties.SelfContained -contains 'true' -or $projectProperties.SelfContained -notcontains 'false') {
    throw 'ChronoOverlay must publish as a framework-dependent application (SelfContained=false).'
}

$workflowPaths = @(
    (Join-Path $PSScriptRoot '..\.github\workflows\pr.yml'),
    (Join-Path $PSScriptRoot '..\.github\workflows\release.yml')
)
foreach ($workflowPath in $workflowPaths) {
    $workflow = Get-Content -Raw -LiteralPath $workflowPath
    if ($workflow -match '--self-contained\s+true' -or $workflow -notmatch '--self-contained\s+false') {
        throw "Workflow must publish framework-dependent: $workflowPath"
    }
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

if (-not $combined.Contains('.NET 8 Desktop Runtime') -or -not $combined.Contains($desktopRuntimeUrl)) {
    throw 'Website output must disclose and link the .NET 8 Desktop Runtime prerequisite.'
}

Write-Output "Version consistency verified for v$version and $assetName."
