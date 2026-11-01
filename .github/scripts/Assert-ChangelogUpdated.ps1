#Requires -Version 7
<#
.SYNOPSIS
    Fails when a change to the packaged tool leaves CHANGELOG.md as it was, when a change that raises Version to a
    stable version has not turned Unreleased into that version's section, or when the changelog is too long for nuget.org.

.DESCRIPTION
    Compares two commits, as Assert-VersionBumped.ps1 does: check.yml passes the two parents of a pull request's merge
    ref, so Base is the tip of the base branch and Head is what that branch would become. Run it locally with any two
    refs, for example -Base main -Head HEAD. The changelog is packed as the package's release notes, which nuget.org
    limits to 35,000 characters (https://learn.microsoft.com/nuget/reference/nuspec#releasenotes). Exit code 0 means
    the changelog is as it should be; 1 means an entry, a version's section or a shorter changelog is needed.

.EXAMPLE
    .github\scripts\Assert-ChangelogUpdated.ps1 -Base main -Head HEAD
#>
[Diagnostics.CodeAnalysis.SuppressMessageAttribute('PSAvoidUsingWriteHost', '',
    Justification = 'Workflow commands such as ::error:: go to the console, as in release.yml; Write-Output inside a function would become its return value.')]
[CmdletBinding()]
param(
    [Parameter(Mandatory)] [string] $Base,
    [Parameter(Mandatory)] [string] $Head
)

$ErrorActionPreference = 'Stop'
$changelog = 'CHANGELOG.md'
$limit = 35000

# The tool paths and the version reader are shared with Assert-VersionBumped.ps1.
. (Join-Path $PSScriptRoot 'ToolPaths.ps1')

function Read-Changelog([string] $commit) {
    $text = git show "${commit}:$changelog" 2>$null | Out-String
    if ($LASTEXITCODE -ne 0) {
        return $null
    }
    return $text
}

# The lines of one section: from its "## " heading to the next one.
function Get-Section([string] $text, [string] $heading) {
    $lines = $text -split "`r?`n"
    $inside = $false
    foreach ($line in $lines) {
        if ($line -match '^## ') {
            $inside = $line -match "^## $([regex]::Escape($heading))(\s+-\s+.*)?\s*$"
            continue
        }
        if ($inside) {
            $line
        }
    }
}

function Test-HasSection([string] $text, [string] $heading) {
    return @(($text -split "`r?`n") | Where-Object { $_ -match "^## $([regex]::Escape($heading))(\s+-\s+.*)?\s*$" }).Count -gt 0
}

$failures = [System.Collections.Generic.List[string]]::new()
$text = Read-Changelog $Head

if ($null -ne $text -and $text.TrimEnd().Length -ge $limit) {
    $failures.Add("$changelog is $($text.TrimEnd().Length) characters; nuget.org keeps at most $limit as release notes. Move the oldest sections to docs\CHANGELOG-earlier.md.")
}

$changed = Get-ChangedToolPath $Base $Head
if ($changed.Count -gt 0) {
    git diff --quiet $Base $Head -- $changelog
    $difference = $LASTEXITCODE
    if ($difference -gt 1) {
        throw "git diff $Base $Head -- $changelog failed with exit code $difference."
    }
    if ($difference -eq 0) {
        $shown = ($changed | Select-Object -First 5) -join ', '
        $failures.Add("This change touches the packaged tool ($shown) but $changelog is unchanged. Add a line under Unreleased, written for the people who use Vantage and ending with the pull request number, or under Internal when they cannot see it (CONTRIBUTING.md, Pull requests).")
    }
}

$before = Read-ToolVersion $Base
$after = Read-ToolVersion $Head
if ($after -ne $before -and -not $after.Contains('-')) {
    if ($null -eq $text -or -not (Test-HasSection $text $after)) {
        $failures.Add("Version is raised to $after, a stable version, but $changelog has no ## $after section. Move the Unreleased entries to a ## $after section (CONTRIBUTING.md, Releasing a stable version).")
    }
    elseif (@(Get-Section $text 'Unreleased' | Where-Object { $_ -match '^- ' }).Count -gt 0) {
        $failures.Add("Version is raised to $after, a stable version, but Unreleased in $changelog still has entries. Move them to the ## $after section and leave Unreleased empty.")
    }
}

if ($failures.Count -eq 0) {
    Write-Host "$changelog is up to date between $Base and $Head."
    exit 0
}

foreach ($failure in $failures) {
    Write-Host "::error::$failure"
}
exit 1
