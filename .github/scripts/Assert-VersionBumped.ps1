#Requires -Version 7
<#
.SYNOPSIS
    Fails when a change to the packaged tool does not raise Version in the tool's csproj.

.DESCRIPTION
    Compares two commits: check.yml passes the two parents of a pull request's merge ref, so Base is the
    tip of main the merge was computed against and Head is what main would become. Run it locally with
    any two refs, for example -Base main -Head HEAD. Exit code 0 means no bump is needed or the version
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

# What ships in the package or changes how it is compiled or restored. Everything else (README, CLAUDE.md,
# .github, openspec, test, tools, the solution file) merges without a bump. A new folder that ships must be
# added here; a folder ending in / matches everything under it.
$toolPaths = @('src/', 'Directory.Build.props', 'global.json', 'nuget.config')

function Test-ToolPath([string] $path) {
    $matching = @($toolPaths | Where-Object { Test-PathMatch $path $_ })
    return $matching.Count -gt 0
}

function Test-PathMatch([string] $path, [string] $toolPath) {
    if ($toolPath.EndsWith('/')) {
        return $path.StartsWith($toolPath, [StringComparison]::OrdinalIgnoreCase)
    }
    return $path -eq $toolPath
}

function Get-ChangedToolPath {
    $changed = @(git diff --name-only $Base $Head)
    if ($LASTEXITCODE -ne 0) {
        throw "git diff $Base $Head failed with exit code $LASTEXITCODE."
    }
    return @($changed | Where-Object { Test-ToolPath $_ })
}

# The same regex release.yml uses: git's output comes through the console encoding, and a byte order mark
# would break an [xml] cast.
function Read-Version([string] $commit) {
    $content = git show "${commit}:$csproj" | Out-String
    if ($LASTEXITCODE -ne 0) {
        throw "git show ${commit}:$csproj failed with exit code $LASTEXITCODE."
    }
    $match = [regex]::Match($content, '<Version>\s*([^<]+?)\s*</Version>')
    if (-not $match.Success) {
        throw "No <Version> in $csproj at $commit."
    }
    return $match.Groups[1].Value
}

function ConvertTo-Version([string] $text, [string] $commit) {
    $parsed = $null
    if ([semver]::TryParse($text, [ref] $parsed)) {
        return $parsed
    }
    Write-Host "::error::Version '$text' in $csproj at $commit is not a semantic version (major.minor.patch)."
    exit 1
}

$changed = Get-ChangedToolPath
if ($changed.Count -eq 0) {
    Write-Host "Nothing that ships changed between $Base and ${Head}: no version bump needed."
    exit 0
}

$before = ConvertTo-Version (Read-Version $Base) $Base
$after = ConvertTo-Version (Read-Version $Head) $Head
if ($after -gt $before) {
    Write-Host "Version raised from $before to $after."
    exit 0
}

$shown = ($changed | Select-Object -First 5) -join ', '
Write-Host "::error::This change touches the packaged tool ($shown) but Version $after is not higher than $before on the base. Raise <Version> in $csproj; README.md, Version bumps, says whether it is a patch, minor or major bump."
exit 1
