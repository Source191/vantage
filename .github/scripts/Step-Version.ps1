#Requires -Version 7
<#
.SYNOPSIS
    Raises Version in the tool's csproj by one step and prints the new version.

.DESCRIPTION
    Raises the trailing number of the prerelease label when there is one (0.17.0-preview.2 becomes 0.17.0-preview.3)
    and otherwise the patch (0.17.0 becomes 0.17.1). Only the Version text changes; the file stays UTF-8 without a
    byte order mark and keeps its line endings. A prerelease label with no trailing number is refused, so nothing
    guesses. check.yml runs it for Dependabot pull requests that touch the tool; it works by hand as well.

.EXAMPLE
    .github\scripts\Step-Version.ps1 -WhatIf
#>
[CmdletBinding(SupportsShouldProcess)]
param(
    [string] $Csproj = 'src/Source191.Vantage/Source191.Vantage.csproj'
)

$ErrorActionPreference = 'Stop'

# The same regex Assert-VersionBumped.ps1 and release.yml use.
$content = [System.IO.File]::ReadAllText((Resolve-Path $Csproj))
$match = [regex]::Match($content, '<Version>\s*([^<]+?)\s*</Version>')
if (-not $match.Success) {
    throw "No <Version> in $Csproj."
}

$current = $match.Groups[1].Value
$parsed = $null
if (-not [semver]::TryParse($current, [ref] $parsed)) {
    throw "Version '$current' in $Csproj is not a semantic version (major.minor.patch)."
}

$core = "$($parsed.Major).$($parsed.Minor).$($parsed.Patch)"
$label = [regex]::Match([string] $parsed.PreReleaseLabel, '^(?<label>.*?)(?<number>\d+)$')
if ([string]::IsNullOrEmpty($parsed.PreReleaseLabel)) {
    $next = "$($parsed.Major).$($parsed.Minor).$($parsed.Patch + 1)"
}
elseif ($label.Success) {
    $next = "$core-$($label.Groups['label'].Value)$([int] $label.Groups['number'].Value + 1)"
}
else {
    throw "Version '$current' has a prerelease label with no trailing number to raise; raise it by hand."
}

if ($PSCmdlet.ShouldProcess($Csproj, "Raise Version from $current to $next")) {
    $updated = $content.Substring(0, $match.Groups[1].Index) + $next + $content.Substring($match.Groups[1].Index + $match.Groups[1].Length)
    [System.IO.File]::WriteAllText((Resolve-Path $Csproj), $updated, [System.Text.UTF8Encoding]::new($false))
}

Write-Output $next
