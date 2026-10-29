#Requires -Version 7
<#
.SYNOPSIS
    Answers whether a change touches only documentation, so check.yml can skip the build for it.

.DESCRIPTION
    Compares two commits: check.yml passes the two parents of a pull request's merge ref, as it does for
    Assert-VersionBumped.ps1. Writes $true when every changed path is under openspec/, or is a .md file outside
    .github/, and $false otherwise, printing the first path that is not documentation. An empty difference is not
    documentation-only, so nothing is skipped by mistake. Renames are listed as a deletion and an addition, so a
    file moved out of src/ into openspec/ still counts as a code change. Run it locally with any two refs.

.EXAMPLE
    .github\scripts\Test-DocumentationOnly.ps1 -Base main -Head HEAD
#>
[Diagnostics.CodeAnalysis.SuppressMessageAttribute('PSAvoidUsingWriteHost', '',
    Justification = 'The reason goes to the console; the answer is the only output.')]
[CmdletBinding()]
[OutputType([bool])]
param(
    [Parameter(Mandatory)] [string] $Base,
    [Parameter(Mandatory)] [string] $Head
)

$ErrorActionPreference = 'Stop'

function Test-DocumentationPath([string] $path) {
    if ($path.StartsWith('openspec/', [StringComparison]::OrdinalIgnoreCase)) {
        return $true
    }
    if ($path.StartsWith('.github/', [StringComparison]::OrdinalIgnoreCase)) {
        return $false
    }
    return $path.EndsWith('.md', [StringComparison]::OrdinalIgnoreCase)
}

$changed = @(git diff --no-renames --name-only $Base $Head)
if ($LASTEXITCODE -ne 0) {
    throw "git diff $Base $Head failed with exit code $LASTEXITCODE."
}
if ($changed.Count -eq 0) {
    Write-Host "Nothing changed between $Base and ${Head}: running the full check."
    return $false
}

$code = @($changed | Where-Object { -not (Test-DocumentationPath $_) })
if ($code.Count -gt 0) {
    Write-Host "$($code[0]) is not documentation: running the full check."
    return $false
}

Write-Host "All $($changed.Count) changed files are documentation (openspec/ or .md)."
return $true
