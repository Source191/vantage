#Requires -Version 7
<#
.SYNOPSIS
    Adds one line under Unreleased, Internal, in CHANGELOG.md, making the heading when it is missing.

.DESCRIPTION
    For changes nobody using Vantage can see that a workflow makes on a pull request's behalf, such as a Dependabot
    dependency update, so Assert-ChangelogUpdated.ps1 finds the entry it asks every change to the packaged tool for.
    Internal is the last heading of a section, so a missing one goes at the end of Unreleased. The text is written as
    given on one line, after the "- " that starts an entry; line breaks in it become spaces.

.EXAMPLE
    .github\scripts\Add-InternalChangelogEntry.ps1 -Text 'Bump Terminal.Gui from 2.5.0 to 2.5.1 (#190)'
#>
[CmdletBinding(SupportsShouldProcess)]
param(
    [Parameter(Mandatory)] [string] $Text,
    [string] $Path = (Join-Path -Path $PSScriptRoot -ChildPath '..' -AdditionalChildPath '..', 'CHANGELOG.md')
)

$ErrorActionPreference = 'Stop'
$entry = '- ' + ($Text -replace '\s*[\r\n]+\s*', ' ').Trim()
$lines = [System.Collections.Generic.List[string]]((Get-Content $Path -Raw) -split '\r?\n')

$unreleased = $lines.FindIndex({ param($line) $line -match '^## Unreleased\s*$' })
if ($unreleased -lt 0) {
    throw "$Path has no ## Unreleased section."
}

$end = $lines.FindIndex($unreleased + 1, { param($line) $line -match '^## ' })
if ($end -lt 0) {
    $end = $lines.Count
}

$internal = $lines.FindIndex($unreleased + 1, $end - $unreleased - 1, { param($line) $line -match '^### Internal\s*$' })
if ($internal -lt 0) {
    # The section's last line that is not blank, then the new heading and the entry.
    $last = $end - 1
    while ($last -gt $unreleased -and [string]::IsNullOrWhiteSpace($lines[$last])) {
        $last--
    }
    $heading = [string[]]@('', '### Internal', '', $entry)
    if ($last + 1 -lt $lines.Count -and -not [string]::IsNullOrWhiteSpace($lines[$last + 1])) {
        $heading += ''
    }
    $lines.InsertRange($last + 1, [string[]]$heading)
}
else {
    $after = $internal
    for ($index = $internal + 1; $index -lt $end -and $lines[$index] -notmatch '^### '; $index++) {
        if ($lines[$index] -match '^- ') {
            $after = $index
        }
    }
    $lines.InsertRange($after + 1, [string[]]@(if ($after -eq $internal) { '', $entry } else { $entry }))
}

if ($PSCmdlet.ShouldProcess($Path, "Add '$entry' under Unreleased, Internal")) {
    Set-Content -Path $Path -Value ($lines -join "`n") -NoNewline
}
