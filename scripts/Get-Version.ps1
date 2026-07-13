[CmdletBinding()]
param()

$propsPath = Join-Path $PSScriptRoot '..\Directory.Build.props'
[xml]$props = Get-Content -Raw -LiteralPath $propsPath
$version = [string]$props.Project.PropertyGroup.VersionPrefix

if ([string]::IsNullOrWhiteSpace($version)) {
    throw 'VersionPrefix is missing from Directory.Build.props.'
}

[void][version]$version
Write-Output $version
