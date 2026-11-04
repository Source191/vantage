#Requires -Version 7.4
<#
.SYNOPSIS
    Makes the README's screenshot (assets\readme\sessions.png) and animation (assets\readme\new-session.gif).

.DESCRIPTION
    Builds a scene folder of made-up Northwind.* repositories (and a stand-in for Torni itself) with worktrees,
    branches and commits, then runs tools\ReadmeScene: Torni's real console screen over that scene, with Claude
    Code, gh and az answered from tools\ReadmeScene\scene.json. The console runs in a Windows Terminal window of its
    own, opens the scene's session tabs there, and captures the window. Nothing is read from or written to your own
    Torni state, Claude Code folder or sessions; the scene folder is deleted afterwards unless -KeepScene is given.

    Needs Windows Terminal, git and, for the animation, ffmpeg (winget install Gyan.FFmpeg). The desktop may be
    locked while it runs, but do not click in the scene window.

.PARAMETER SceneRoot
    Where the scene is built. Its home is <SceneRoot>\me, so folder labels read ~\source\repos.

.PARAMETER KeepScene
    Leaves the scene folder and window in place, to look at or to run the console again by hand.

.PARAMETER Only
    Screenshot or Animation to make just one of them; both by default.

.EXAMPLE
    .\tools\New-ReadmeScreenshot.ps1
#>
[CmdletBinding()]
param(
    [string] $SceneRoot = 'C:\TorniDemo',
    [switch] $KeepScene,
    [ValidateSet('Both', 'Screenshot', 'Animation', 'None')]
    [string] $Only = 'Both'
)

$ErrorActionPreference = 'Stop'
$repositoryRoot = Split-Path -Parent $PSScriptRoot
$marker = '.readme-scene'
$author = @('-c', 'user.name=Demo', '-c', 'user.email=demo@example.com', '-c', 'commit.gpgsign=false', '-c', 'core.autocrlf=false')

function Invoke-Git {
    param([string] $Folder, [string[]] $Arguments, [int] $MinutesAgo = 0)

    $when = [DateTimeOffset]::UtcNow.AddMinutes(-$MinutesAgo).ToString('yyyy-MM-ddTHH:mm:ssZ', [Globalization.CultureInfo]::InvariantCulture)
    $env:GIT_AUTHOR_DATE = $when
    $env:GIT_COMMITTER_DATE = $when
    $output = & git -C $Folder @author @Arguments 2>&1
    if ($LASTEXITCODE -ne 0) {
        throw "git $($Arguments -join ' ') failed in ${Folder}: $output"
    }
}

function Add-Commit {
    param([string] $Folder, [string] $File, [string] $Text, [string] $Message, [int] $MinutesAgo)

    $path = Join-Path $Folder $File
    New-Item -ItemType Directory -Force -Path (Split-Path -Parent $path) | Out-Null
    Add-Content -Path $path -Value $Text
    Invoke-Git $Folder @('add', '--all')
    Invoke-Git $Folder @('commit', '--quiet', '-m', $Message) -MinutesAgo $MinutesAgo
}

# Asks the scene to close (the console quits and its session tabs end cleanly, which closes the window), then ends
# anything of the scene's that is still running: only programs run from the scene's own bin.
function Stop-Scene {
    $bin = Join-Path $SceneRoot 'bin'
    $running = { @(Get-Process -Name 'readme-scene' -ErrorAction SilentlyContinue | Where-Object { $_.Path -and $_.Path.StartsWith($bin, [StringComparison]::OrdinalIgnoreCase) }) }
    if ((& $running).Count -eq 0) {
        return
    }

    New-Item -ItemType File -Force -Path (Join-Path $SceneRoot 'close.mark') | Out-Null
    $deadline = (Get-Date).AddSeconds(15)
    while ((& $running).Count -gt 0 -and (Get-Date) -lt $deadline) {
        Start-Sleep -Milliseconds 250
    }

    $left = & $running
    if ($left.Count -gt 0) {
        $left | Stop-Process -Force
        Write-Warning 'The scene did not close by itself; close its Windows Terminal window by hand.'
    }

    Remove-Item (Join-Path $SceneRoot 'close.mark') -ErrorAction SilentlyContinue
}
function Remove-Scene {
    if (-not (Test-Path $SceneRoot)) {
        return
    }

    Stop-Scene

    if (-not (Test-Path (Join-Path $SceneRoot $marker))) {
        throw "$SceneRoot exists but was not made by this script, so it is left alone. Choose another -SceneRoot."
    }

    Remove-Item -Recurse -Force $SceneRoot
}

function New-Repository {
    param([string] $Name, [string] $Remote, [string] $Folder)

    New-Item -ItemType Directory -Force -Path $Folder | Out-Null
    Invoke-Git $Folder @('init', '--quiet', '--initial-branch', 'main')
    Add-Commit $Folder 'README.md' "# $Name" 'Start the repository' -MinutesAgo 20160
    Add-Commit $Folder "src/$Name.csproj" '<Project Sdk="Microsoft.NET.Sdk" />' 'Add the project' -MinutesAgo 10080
    Add-Commit $Folder 'src/Program.cs' 'Console.WriteLine("Northwind");' 'Add the entry point' -MinutesAgo 2880
    Invoke-Git $Folder @('remote', 'add', 'origin', $Remote)
    # The remote is never fetched; these refs make the checkout look like a clone of it.
    Invoke-Git $Folder @('update-ref', 'refs/remotes/origin/main', 'main')
    Invoke-Git $Folder @('symbolic-ref', 'refs/remotes/origin/HEAD', 'refs/remotes/origin/main')
    Invoke-Git $Folder @('branch', '--quiet', '--set-upstream-to', 'origin/main', 'main')
}

# How each session's worktree looks: its commits, whether they are pushed or merged, and any uncommitted change.
function Set-WorktreeHistory {
    param([string] $Repository, [string] $Worktree, [string] $Slug, [string] $State)

    Add-Commit $Worktree "src/$Slug.cs" "// $Slug" "Start $Slug" -MinutesAgo 90
    switch ($State) {
        'waiting' {
            Invoke-Git $Worktree @('update-ref', "refs/remotes/origin/$Slug", 'HEAD')
            Invoke-Git $Worktree @('branch', '--quiet', '--set-upstream-to', "origin/$Slug")
            Add-Content -Path (Join-Path $Worktree "src/$Slug.cs") -Value '// rounding to the penny, half up'
        }
        'working' {
            Add-Commit $Worktree "src/$Slug.cs" '// first pass' "Shape $Slug" -MinutesAgo 20
            Add-Content -Path (Join-Path $Worktree "src/$Slug.Tests.cs") -Value '// tests to come'
        }
        'idle' {
            Invoke-Git $Worktree @('update-ref', "refs/remotes/origin/$Slug", 'HEAD')
            Invoke-Git $Worktree @('branch', '--quiet', '--set-upstream-to', "origin/$Slug")
        }
        'done' {
            Invoke-Git $Repository @('merge', '--quiet', '--no-ff', '-m', "Merge $Slug", $Slug) -MinutesAgo 30
            Invoke-Git $Repository @('update-ref', 'refs/remotes/origin/main', 'main')
        }
        'stopped' {
            Invoke-Git $Repository @('merge', '--quiet', '--no-ff', '-m', "Merge $Slug", $Slug) -MinutesAgo 1500
            Invoke-Git $Repository @('update-ref', 'refs/remotes/origin/main', 'main')
        }
        default {
            Add-Commit $Worktree "src/$Slug.cs" '// retry with backoff' "Retry $Slug" -MinutesAgo 240
        }
    }
}

function New-Scene {
    param($Scene, [string] $SceneHome)

    $repos = Join-Path $SceneHome 'source\repos'
    $worktrees = Join-Path $SceneHome 'source\worktrees'
    foreach ($repository in $Scene.repositories) {
        New-Repository $repository.name $repository.remote (Join-Path $repos $repository.name)
    }

    foreach ($session in $Scene.sessions | Where-Object { $_.worktree }) {
        $repository = Join-Path $repos $session.repository
        $worktree = Join-Path $worktrees "$($session.repository)-$($session.worktree)"
        Invoke-Git $repository @('worktree', 'add', '--quiet', '-b', $session.worktree, $worktree, 'main')
        Set-WorktreeHistory $repository $worktree $session.worktree $session.state
    }
}

function Get-TorniVersion {
    [xml] $project = Get-Content (Join-Path $repositoryRoot 'src\Source191.Torni\Source191.Torni.csproj') -Raw
    $version = @($project.Project.PropertyGroup.Version | Where-Object { $_ })[0]
    if (-not $version) {
        throw 'No <Version> in src\Source191.Torni\Source191.Torni.csproj.'
    }

    return $version
}

# Windows Terminal hands the caller's environment to new tabs, and these turn colour off in everything they run.
function Clear-ClaudeEnvironment {
    Get-ChildItem Env: |
        Where-Object { $_.Name -in 'NO_COLOR', 'CLAUDECODE', 'CLAUDE_PID' -or $_.Name -like 'CLAUDE_CODE_*' } |
        ForEach-Object { Remove-Item "Env:$($_.Name)" }
}

foreach ($program in 'git', 'wt') {
    if (-not (Get-Command $program -CommandType Application -ErrorAction SilentlyContinue)) {
        throw "$program is not on the PATH."
    }
}

$version = Get-TorniVersion
$scene = Get-Content (Join-Path $repositoryRoot 'tools\ReadmeScene\scene.json') -Raw | ConvertFrom-Json
$sceneHome = Join-Path $SceneRoot 'me'
$bin = Join-Path $SceneRoot 'bin'

Remove-Scene
New-Item -ItemType Directory -Force -Path $SceneRoot | Out-Null
New-Item -ItemType File -Path (Join-Path $SceneRoot $marker) | Out-Null

Write-Host "Building tools\ReadmeScene into $bin"
dotnet publish (Join-Path $repositoryRoot 'tools\ReadmeScene') -c Release -o $bin --nologo -v quiet
if ($LASTEXITCODE -ne 0) {
    throw 'tools\ReadmeScene did not build.'
}

Write-Host "Building the scene's repositories under $sceneHome"
New-Scene $scene $sceneHome

Clear-ClaudeEnvironment
$sceneExe = Join-Path $bin 'readme-scene.exe'
$assets = Join-Path $repositoryRoot 'assets\readme'
New-Item -ItemType Directory -Force -Path $assets | Out-Null

# Opens the scene's window and starts the console, which opens the session tabs and writes <mark>.mark when it is done.
function Start-SceneRun {
    param([string] $Mark, [string[]] $Extra)

    Remove-Item (Join-Path $SceneRoot '*.mark') -ErrorAction SilentlyContinue
    wt -w readme-scene --size 140,30 new-tab --startingDirectory $sceneHome $sceneExe run $SceneRoot $version @Extra
    $markFile = Join-Path $SceneRoot "$Mark.mark"
    $deadline = (Get-Date).AddMinutes(3)
    while (-not (Test-Path $markFile)) {
        if ((Get-Date) -gt $deadline) {
            throw "The scene never wrote $Mark.mark; look at its window, then run again."
        }

        Start-Sleep -Milliseconds 500
    }
}

if ($Only -in 'Both', 'Screenshot') {
    Write-Host 'Taking the screenshot'
    Start-SceneRun -Mark 'ready'
    & $sceneExe capture $SceneRoot (Join-Path $assets 'sessions.png')
    Stop-Scene
}

if ($Only -in 'Both', 'Animation') {
    $ffmpeg = Get-Command ffmpeg -CommandType Application -ErrorAction SilentlyContinue
    if (-not $ffmpeg) {
        Write-Warning 'ffmpeg is not on the PATH, so no animation was made: winget install Gyan.FFmpeg, open a new terminal and run again with -Only Animation.'
    }
    else {
        Write-Host 'Recording the animation'
        $frames = Join-Path $SceneRoot 'frames'
        Start-SceneRun -Mark 'done' -Extra @('--frames', $frames, (Join-Path $bin 'new-session.keys'))
        Stop-Scene
        $gif = Join-Path $assets 'new-session.gif'
        $filter = 'fps=10,scale=960:-1:flags=lanczos,split[a][b];[a]palettegen=max_colors=128:stats_mode=diff[p];[b][p]paletteuse=dither=bayer:bayer_scale=5:diff_mode=rectangle'
        & $ffmpeg.Source -y -loglevel error -framerate 10 -i (Join-Path $frames 'frame-%05d.png') -vf $filter -loop 0 $gif
        if ($LASTEXITCODE -ne 0) {
            throw 'ffmpeg could not make the animation.'
        }

        Write-Host ("new-session.gif is {0:N1} MB" -f ((Get-Item $gif).Length / 1MB))
    }
}

if ($Only -eq 'None') {
    Write-Host 'Starting the scene and leaving it running'
    Start-SceneRun -Mark 'ready'
    return
}

if (-not $KeepScene) {
    Stop-Scene
    Remove-Scene
}

Write-Host "Done: see $assets"