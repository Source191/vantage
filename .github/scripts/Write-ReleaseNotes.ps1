#Requires -Version 7
<#
.SYNOPSIS
    Writes one version's section of CHANGELOG.md, without its heading, for a stable GitHub Release's notes.

.DESCRIPTION
    release.yml's build job runs it for every release and uploads the file with the package; the release job passes it
    to gh release create only for a stable version, where it comes before the generated list of pull requests. For a
    prerelease the file is written empty. A stable version without a section fails, which Assert-ChangelogUpdated.ps1
    has already refused in its pull request.

.EXAMPLE
    .github\scripts\Write-ReleaseNotes.ps1 -Version 0.21.0 -Destination release-notes.md
#>
[CmdletBinding(SupportsShouldProcess)]
param(
    [Parameter(Mandatory)] [string] $Version,
    [Parameter(Mandatory)] [string] $Destination,
    [string] $Path = (Join-Path -Path $PSScriptRoot -ChildPath '..' -AdditionalChildPath '..', 'CHANGELOG.md')
)

$ErrorActionPreference = 'Stop'
$section = @()
if (-not $Version.Contains('-')) {
    $heading = "^## $([regex]::Escape($Version))(\s+-\s+.*)?\s*$"
    $inside = $false
    $found = $false
    foreach ($line in (Get-Content $Path -Raw) -split '\r?\n') {
        if ($line -match '^## ') {
            $inside = $line -match $heading
            $found = $found -or $inside
            continue
        }
        if ($inside) {
            $section += $line
        }
    }
    if (-not $found) {
        throw "$Path has no ## $Version section."
    }
}

if ($PSCmdlet.ShouldProcess($Destination, "Write the notes of $Version")) {
    Set-Content -Path $Destination -Value (($section -join "`n").Trim() + "`n") -NoNewline
}
