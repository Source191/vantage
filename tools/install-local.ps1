<#
.SYNOPSIS
    Packs Vantage and installs it as the global dotnet tool `vantage`, for testing a build
    before it is released.

.DESCRIPTION
    The dotnet tool cache keeps the first package it sees for a version for ever, so installing
    a second build of 0.1.18 silently runs the first one. This script sidesteps that by packing
    with a prerelease suffix, 0.1.18-local.<UTC timestamp>, which is always newer than the last
    local build and always older than the released 0.1.18, so a later `dotnet tool update`
    from the real feed still wins.

    A running vantage.exe locks the Release output and makes `dotnet pack` fail, so any running
    instance is stopped first. Safe to re-run.

.PARAMETER Clean
    Uninstall the tool before installing, instead of updating in place. Use it when an update
    misbehaves or to clear the tool's cached versions.

.PARAMETER FromArtifact
    A folder holding a Source191.Vantage.<version>.nupkg, typically one downloaded from a
    GitHub Actions run with `gh run download <run id>`. The newest package in the folder is
    installed as it is; nothing is built or packed.

.EXAMPLE
    .\tools\install-local.ps1

.EXAMPLE
    .\tools\install-local.ps1 -Clean

.EXAMPLE
    gh run download 123456789 --name Source191.Vantage-0.1.18 --dir .\downloaded
    .\tools\install-local.ps1 -FromArtifact .\downloaded
#>
[CmdletBinding()]
param(
    [switch]$Clean,
    [string]$FromArtifact
)

$ErrorActionPreference = 'Stop'

$PackageId = 'Source191.Vantage'
$RepoRoot = Split-Path -Parent $PSScriptRoot
$Project = Join-Path $RepoRoot 'src\Source191.Vantage\Source191.Vantage.csproj'
$Packages = Join-Path $RepoRoot 'artifacts\packages'

$running = @(Get-Process -Name vantage -ErrorAction SilentlyContinue)
if ($running.Count -gt 0) {
    $running | Stop-Process -Force
    Write-Host "Stopped $($running.Count) running vantage.exe process(es)."
}

if ($FromArtifact) {
    $source = (Resolve-Path -LiteralPath $FromArtifact).Path
    $package = Get-ChildItem -LiteralPath $source -Filter "$PackageId.*.nupkg" |
        Sort-Object LastWriteTime |
        Select-Object -Last 1
    if ($null -eq $package) {
        Write-Error "No $PackageId.*.nupkg found in '$source'."
        exit 1
    }
    $version = $package.BaseName.Substring($PackageId.Length + 1)
    Write-Host "Installing $($package.Name) from $source."
}
else {
    $baseVersion = (Select-Xml -Path $Project -XPath '//Version').Node.InnerText
    $stamp = [DateTimeOffset]::UtcNow.ToString('yyyyMMddHHmmss')
    $version = "$baseVersion-local.$stamp"
    $source = $Packages
    Write-Host "Packing $PackageId $version."
    dotnet pack $Project -c Release -p:Version=$version
    if ($LASTEXITCODE -ne 0) {
        Write-Error "dotnet pack failed with exit code $LASTEXITCODE."
        exit $LASTEXITCODE
    }
}

$installed = @(dotnet tool list -g) -match "^$([regex]::Escape($PackageId))\s" | Measure-Object | Select-Object -ExpandProperty Count
if ($Clean -and $installed -gt 0) {
    dotnet tool uninstall -g $PackageId
    if ($LASTEXITCODE -ne 0) {
        Write-Error "dotnet tool uninstall failed with exit code $LASTEXITCODE."
        exit $LASTEXITCODE
    }
    $installed = 0
}

$verb = if ($installed -gt 0) { 'update' } else { 'install' }
dotnet tool $verb -g $PackageId --add-source $source --version $version
if ($LASTEXITCODE -ne 0) {
    Write-Error "dotnet tool $verb failed with exit code $LASTEXITCODE."
    exit $LASTEXITCODE
}

Write-Host "Installed $PackageId $version as 'vantage'." -ForegroundColor Green
Write-Host "A shell that was open before the first install needs restarting to pick up ~\.dotnet\tools."
