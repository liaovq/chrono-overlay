[CmdletBinding()]
param(
    [Parameter(Mandatory)]
    [string]$ExecutablePath,

    [ValidateRange(2, 30)]
    [int]$ObservationSeconds = 5
)

$resolvedExecutable = (Resolve-Path -LiteralPath $ExecutablePath).Path
$process = $null

try {
    $process = Start-Process -FilePath $resolvedExecutable -ArgumentList '--autostart' -PassThru
    $deadline = [DateTime]::UtcNow.AddSeconds($ObservationSeconds)

    while ([DateTime]::UtcNow -lt $deadline) {
        Start-Sleep -Milliseconds 250
        $process.Refresh()
        if ($process.HasExited) {
            throw "Published executable exited during startup smoke test with exit code $($process.ExitCode)."
        }
    }

    Write-Output "Published executable remained running for $ObservationSeconds seconds."
}
finally {
    if ($null -ne $process) {
        $process.Refresh()
        if (-not $process.HasExited) {
            Stop-Process -Id $process.Id -Force
            Wait-Process -Id $process.Id -ErrorAction SilentlyContinue
        }
    }
}
