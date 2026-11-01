#Requires -Version 7
<#
.SYNOPSIS
    Fails when a change to the packaged tool does not raise Version in the tool's csproj.

.DESCRIPTION
    Compares two commits: check.yml passes the two parents of a pull request's merge ref, so Base is the
    tip of the base branch the merge was computed against (main, or the parent of a stacked pull request) and
    Head is what that branch would become. Run it locally with any two refs, for example -Base main -Head HEAD. Exit code 0 means no bump is needed or the version
    was raised; 1 means a bump is missing, lower, or not a semantic version.

.EXAMPLE
    .github\scripts\Assert-VersionBumped.ps1 -Base main -Head HEAD
#>
[Diagnostics.CodeAnalysis.SuppressMessageAttribute('PSAvoidUsingWriteHost', '',
    Justification = 'Workflow commands such as ::error:: go to the console, as in release.yml; Write-Output inside a function would become its return value.')]
[CmdletBinding()]
param(
    [Parameter(Mandatory)] [string] $Base,
    [Parameter(Mandatory)] [string] $Head
)

$ErrorActionPreference = 'Stop'
$csproj = 'src/Source191.Vantage/Source191.Vantage.csproj'

# The tool paths and the version reader are shared with Assert-ChangelogUpdated.ps1.
. (Join-Path $PSScriptRoot 'ToolPaths.ps1')

function ConvertTo-Version([string] $text, [string] $commit) {
    $parsed = $null
    if ([semver]::TryParse($text, [ref] $parsed)) {
        return $parsed
    }
    Write-Host "::error::Version '$text' in $csproj at $commit is not a semantic version (major.minor.patch)."
    exit 1
}

$changed = Get-ChangedToolPath $Base $Head
if ($changed.Count -eq 0) {
    Write-Host "Nothing that ships changed between $Base and ${Head}: no version bump needed."
    exit 0
}

$before = ConvertTo-Version (Read-ToolVersion $Base) $Base
$after = ConvertTo-Version (Read-ToolVersion $Head) $Head
if ($after -gt $before) {
    Write-Host "Version raised from $before to $after."
    exit 0
}

$shown = ($changed | Select-Object -First 5) -join ', '
Write-Host "::error::This change touches the packaged tool ($shown) but Version $after is not higher than $before on the base. Raise <Version> in $csproj; CONTRIBUTING.md, Versions, says whether it is a patch, minor or major bump."
exit 1
