#Requires -Version 7
<#
.SYNOPSIS
    Fails when a change to the packaged tool leaves CHANGELOG.md as it was, or when a change that raises Version to a
    stable version has not turned Unreleased into that version's section.

.DESCRIPTION
    Compares two commits, as Assert-VersionBumped.ps1 does: pipeline.yml passes the two parents of a pull request's merge
    ref, so Base is the tip of the base branch and Head is what that branch would become. Run it locally with any two
    refs, for example -Base main -Head HEAD. Exit code 0 means the changelog is as it should be; 1 means an entry or a
    version's section is needed.

.EXAMPLE
    .github\scripts\Assert-ChangelogUpdated.ps1 -Base main -Head HEAD
#>
[Diagnostics.CodeAnalysis.SuppressMessageAttribute('PSAvoidUsingWriteHost', '',
    Justification = 'Workflow commands such as ::error:: go to the console, as in pipeline.yml; Write-Output inside a function would become its return value.')]
[CmdletBinding()]
param(
    [Parameter(Mandatory)] [string] $Base,
    [Parameter(Mandatory)] [string] $Head
)

$ErrorActionPreference = 'Stop'
$changelog = 'CHANGELOG.md'

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


$changed = Get-ChangedToolPath $Base $Head
if ($changed.Count -gt 0) {
    git diff --quiet $Base $Head -- $changelog
    $difference = $LASTEXITCODE
    if ($difference -gt 1) {
        throw "git diff $Base $Head -- $changelog failed with exit code $difference."
    }
    if ($difference -eq 0) {
        $shown = ($changed | Select-Object -First 5) -join ', '
        $failures.Add("This change touches the packaged tool ($shown) but $changelog is unchanged. Add a line under Unreleased, written for the people who use it and ending with the pull request number, or under Internal when they cannot see it (CONTRIBUTING.md, Pull requests).")
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
