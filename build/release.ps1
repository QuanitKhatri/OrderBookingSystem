# Builds an installable release of the Distribution Management System and, with -Publish,
# uploads it to GitHub so that every customer's copy offers the update.
#
#   .\build\release.ps1 -Version 1.1.0
#   .\build\release.ps1 -Version 1.1.0 -Notes "Receive Payment screen added." -Publish -Token ghp_xxx
#
# The first build of a new installation produces build\Releases\*-Setup.exe. Give that file to the
# customer once; after that the application updates itself.

[CmdletBinding()]
param(
    # Version to publish, for example 1.1.0. Must be higher than the last published one.
    [Parameter(Mandatory = $true)]
    [ValidatePattern('^\d+\.\d+\.\d+$')]
    [string]$Version,

    # Shown to the customer in the update window.
    [string]$Notes = '',

    # Upload to GitHub. Without this the release is only built locally.
    [switch]$Publish,

    # GitHub token with write access. Falls back to the GITHUB_TOKEN environment variable.
    [string]$Token = $env:GITHUB_TOKEN,

    [string]$RepositoryUrl = 'https://github.com/QuanitKhatri/OrderBookingSystem'
)

$ErrorActionPreference = 'Stop'
$root = Split-Path -Parent $PSScriptRoot
Push-Location $root

try {
    $packId  = 'DistributionManagementSystem'
    $project = 'src\Distribution.WinForms\Distribution.WinForms.csproj'
    $stage   = Join-Path $root 'build\stage'
    $out     = Join-Path $root 'build\Releases'

    Write-Host "Building $packId $Version" -ForegroundColor Cyan

    if (Test-Path $stage) { Remove-Item $stage -Recurse -Force }

    # Self-contained: the customer's PC does not need the .NET runtime installed.
    dotnet publish $project `
        -c Release `
        -r win-x64 `
        --self-contained true `
        -p:Version=$Version `
        -p:PublishSingleFile=false `
        -o $stage
    if ($LASTEXITCODE -ne 0) { throw "dotnet publish failed." }

    # appsettings.json holds this machine's database password; it must never ship to a customer.
    # Each site keeps its own, written during installation.
    $shipped = Join-Path $stage 'appsettings.json'
    if (Test-Path $shipped) { Remove-Item $shipped -Force }
    Copy-Item (Join-Path $root 'src\Distribution.WinForms\appsettings.example.json') $shipped -Force

    $notesArgs = @()
    if ($Notes) {
        $notesFile = Join-Path $stage '..\release-notes.md'
        Set-Content -Path $notesFile -Value $Notes -Encoding utf8
        $notesArgs = @('--releaseNotes', (Resolve-Path $notesFile).Path)
    }

    # --runtime must match the publish above, otherwise vpk packages it as x86.
    dotnet vpk pack `
        --packId $packId `
        --packVersion $Version `
        --packDir $stage `
        --mainExe 'Distribution.WinForms.exe' `
        --packTitle 'Distribution Management System' `
        --runtime win-x64 `
        --outputDir $out `
        @notesArgs
    if ($LASTEXITCODE -ne 0) { throw "vpk pack failed." }

    Write-Host "Built into $out" -ForegroundColor Green

    if (-not $Publish) {
        Write-Host "Not published. Add -Publish to upload it." -ForegroundColor Yellow
        return
    }

    if (-not $Token) { throw "No token. Pass -Token or set GITHUB_TOKEN." }

    dotnet vpk upload github `
        --repoUrl $RepositoryUrl `
        --token $Token `
        --tag "v$Version" `
        --releaseName "v$Version" `
        --outputDir $out `
        --publish
    if ($LASTEXITCODE -ne 0) { throw "vpk upload failed." }

    Write-Host "Published v$Version. Customers will be offered it at their next check." -ForegroundColor Green
}
finally {
    Pop-Location
}
