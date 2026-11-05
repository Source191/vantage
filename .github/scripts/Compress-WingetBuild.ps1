#Requires -Version 7
<#
.SYNOPSIS
    Zips the self-contained winget build and fails unless the zip holds exactly the files the program ships.

.DESCRIPTION
    The publish folder (dotnet publish src\Source191.Torni -p:PublishProfile=win-x64) must hold torni.exe, Terminal.Gui's
    native libonigwrap.dll, and the three files the program reads or must carry beside itself: CHANGELOG.md (the notes of the
    running version), logo.ico (the Windows Terminal profiles' icon) and THIRD-PARTY-NOTICES.txt. Anything else, a PDB or
    an XML documentation file above all, fails the build rather than shipping, and so does a missing file. The zip is flat,
    so winget extracts every file beside torni.exe. Exit code 0 means the zip was written and checked; 1 means it was not.

.EXAMPLE
    .github\scripts\Compress-WingetBuild.ps1 -PublishFolder artifacts\publish -Destination artifacts\winget\torni_0.21.0_win-x64.zip
#>
[Diagnostics.CodeAnalysis.SuppressMessageAttribute('PSAvoidUsingWriteHost', '',
    Justification = 'Workflow commands such as ::error:: go to the console, as in the other scripts here.')]
[CmdletBinding()]
param(
    [Parameter(Mandatory)] [string] $PublishFolder,
    [Parameter(Mandatory)] [string] $Destination
)

$ErrorActionPreference = 'Stop'
$expected = @('CHANGELOG.md', 'libonigwrap.dll', 'THIRD-PARTY-NOTICES.txt', 'torni.exe', 'logo.ico')

$folder = (Resolve-Path -LiteralPath $PublishFolder).Path
$published = @(Get-ChildItem -LiteralPath $folder -Recurse -File | ForEach-Object { [IO.Path]::GetRelativePath($folder, $_.FullName) })
$missing = @($expected | Where-Object { $_ -notin $published })
$extra = @($published | Where-Object { $_ -notin $expected })
if ($missing.Count -gt 0 -or $extra.Count -gt 0) {
    if ($missing.Count -gt 0) {
        Write-Host "::error::The winget build is missing $($missing -join ', ') in $folder."
    }
    if ($extra.Count -gt 0) {
        Write-Host "::error::The winget build holds files it must not ship: $($extra -join ', '). Leave them out in src\Source191.Torni\Properties\PublishProfiles\win-x64.pubxml."
    }
    exit 1
}

New-Item -ItemType Directory -Force (Split-Path -Parent $Destination) | Out-Null
if (Test-Path -LiteralPath $Destination) {
    Remove-Item -LiteralPath $Destination
}
$compress = @{
    LiteralPath      = @($expected | ForEach-Object { Join-Path $folder $_ })
    DestinationPath  = $Destination
    CompressionLevel = 'Optimal'
}
Compress-Archive @compress

Add-Type -AssemblyName System.IO.Compression.FileSystem
$archive = [System.IO.Compression.ZipFile]::OpenRead((Resolve-Path -LiteralPath $Destination).Path)
try {
    $entries = @($archive.Entries.FullName | Sort-Object)
}
finally {
    $archive.Dispose()
}
if (($entries -join '|') -ne (($expected | Sort-Object) -join '|')) {
    Write-Host "::error::$Destination holds $($entries -join ', '); expected $($expected -join ', ')."
    exit 1
}

Write-Host "Zipped $($entries.Count) files into $Destination ($([math]::Round((Get-Item -LiteralPath $Destination).Length / 1MB, 1)) MB)."
exit 0