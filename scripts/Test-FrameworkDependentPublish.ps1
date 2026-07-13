[CmdletBinding()]
param(
    [Parameter(Mandatory)]
    [string]$PublishDirectory,

    [Parameter(Mandatory)]
    [string]$ExecutablePath,

    [long]$MaximumExecutableBytes = 10MB
)

$publishPath = (Resolve-Path -LiteralPath $PublishDirectory).Path
$executable = Get-Item -LiteralPath $ExecutablePath -ErrorAction Stop

if ($executable.Length -gt $MaximumExecutableBytes) {
    throw "Published executable is $($executable.Length) bytes; expected a framework-dependent executable below $MaximumExecutableBytes bytes."
}

$unexpectedRuntimeLibraries = Get-ChildItem -LiteralPath $publishPath -File -Filter '*.dll'
if ($unexpectedRuntimeLibraries.Count -gt 0) {
    $names = ($unexpectedRuntimeLibraries.Name | Sort-Object) -join ', '
    throw "Framework-dependent single-file output unexpectedly contains runtime libraries: $names"
}

$desktopRuntimes = @(dotnet --list-runtimes | Where-Object { $_ -match '^Microsoft\.WindowsDesktop\.App 8\.' })
if ($desktopRuntimes.Count -eq 0) {
    throw 'The validation machine does not have the .NET 8 Desktop Runtime required to launch ChronoOverlay.'
}

Write-Output "Framework-dependent publish verified: $($executable.Name), $($executable.Length) bytes."
Write-Output "Desktop runtime: $($desktopRuntimes[-1])"
