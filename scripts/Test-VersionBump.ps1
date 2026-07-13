[CmdletBinding()]
param(
    [Parameter(Mandatory = $true)]
    [string]$BaseRef
)

$repositoryRoot = (Resolve-Path (Join-Path $PSScriptRoot '..')).Path
Push-Location $repositoryRoot
try {
    $changedFiles = @(git diff --name-only "$BaseRef...HEAD")
    if ($LASTEXITCODE -ne 0) {
        throw "Unable to compare HEAD with $BaseRef."
    }

    $releaseFiles = @($changedFiles | Where-Object {
        $_ -and
        $_ -ne 'LICENSE' -and
        $_ -ne '.github/pull_request_template.md' -and
        $_ -notmatch '(^|/)[^/]+\.md$' -and
        $_ -notlike 'docs/*'
    })

    if ($releaseFiles.Count -eq 0) {
        Write-Output 'releaseNeeded=false'
        return
    }

    [version]$currentVersion = & (Join-Path $PSScriptRoot 'Get-Version.ps1')
    $baseSpec = '{0}:Directory.Build.props' -f $BaseRef
    $baseContent = @(git show $baseSpec 2>$null)
    if ($LASTEXITCODE -eq 0) {
        $baseMatch = [regex]::Match(($baseContent -join "`n"), '<VersionPrefix>([^<]+)</VersionPrefix>')
        if (-not $baseMatch.Success) {
            throw "VersionPrefix is missing from $baseSpec."
        }

        [version]$baseVersion = $baseMatch.Groups[1].Value
        if ($currentVersion -le $baseVersion) {
            $files = $releaseFiles -join ', '
            throw "Release-impacting changes require a version bump above $baseVersion. Files: $files"
        }
    }

    Write-Output 'releaseNeeded=true'
}
finally {
    Pop-Location
}
