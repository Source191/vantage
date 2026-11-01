#Requires -Version 7
<#
.SYNOPSIS
    The paths that make up the packaged tool, shared by Assert-VersionBumped.ps1 and Assert-ChangelogUpdated.ps1.

.DESCRIPTION
    Dot-source it. What ships in the package or changes how it is compiled or restored. Everything else (README,
    CONTRIBUTING.md, CLAUDE.md, CHANGELOG.md, .github, openspec, test, tools, the solution file) merges without a bump.
    A new folder that ships must be added here; a folder ending in / matches everything under it.
#>

$ToolPaths = @('src/', 'Directory.Build.props', 'global.json', 'nuget.config')

function Test-ToolPath([string] $path) {
    $matching = @($ToolPaths | Where-Object { Test-PathMatch $path $_ })
    return $matching.Count -gt 0
}

function Test-PathMatch([string] $path, [string] $toolPath) {
    if ($toolPath.EndsWith('/')) {
        return $path.StartsWith($toolPath, [StringComparison]::OrdinalIgnoreCase)
    }
    return $path -eq $toolPath
}

# The files that changed between two commits and are part of the packaged tool.
function Get-ChangedToolPath([string] $Base, [string] $Head) {
    $changed = @(git diff --name-only $Base $Head)
    if ($LASTEXITCODE -ne 0) {
        throw "git diff $Base $Head failed with exit code $LASTEXITCODE."
    }
    return @($changed | Where-Object { Test-ToolPath $_ })
}

# The tool's Version at a commit. The same regex release.yml uses: git's output comes through the console encoding,
# and a byte order mark would break an [xml] cast.
function Read-ToolVersion([string] $commit) {
    $csproj = 'src/Source191.Vantage/Source191.Vantage.csproj'
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
