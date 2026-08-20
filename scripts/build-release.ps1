param(
    [string]$OutputPath
)

$ErrorActionPreference = 'Stop'
$repositoryRoot = Split-Path -Parent $PSScriptRoot
if ([string]::IsNullOrWhiteSpace($OutputPath)) {
    $OutputPath = Join-Path $repositoryRoot '.build\release\win-x64'
}
$OutputPath = [System.IO.Path]::GetFullPath($OutputPath)
$projectPath = Join-Path $repositoryRoot 'src\AIsle.DesktopApp\AIsle.DesktopApp.csproj'

dotnet publish $projectPath -c Release -r win-x64 --self-contained true -p:PublishProfile=win-x64 --output $OutputPath --nologo
if ($LASTEXITCODE -ne 0) { throw 'dotnet publish failed.' }

$required = @(
    (Join-Path $OutputPath 'AIsleDesktop.exe'),
    (Join-Path $OutputPath 'UI\index.html'),
    (Join-Path $OutputPath 'UI\default-project.json'),
    (Join-Path $OutputPath 'UI\desktop-bridge.js')
)
foreach ($path in $required) {
    if (-not (Test-Path -LiteralPath $path -PathType Leaf)) { throw "Release output is missing: $path" }
}

$forbidden = Get-ChildItem -LiteralPath $OutputPath -Recurse -File | Where-Object {
    $_.Name -in @('node.exe', 'UnityPlayer.dll') -or $_.FullName -match '(?i)Reality|VideoAnalytics'
}
if ($forbidden) { throw 'Release output contains a forbidden runtime or removed module.' }

$manifest = [ordered]@{
    product = 'AIsle Desktop'
    version = '1.0.0-mvp'
    runtimeIdentifier = 'win-x64'
    selfContainedDotNet = $true
    webView2Runtime = 'Evergreen prerequisite'
    builtAtUtc = [DateTimeOffset]::UtcNow.ToString('O')
}
$manifest | ConvertTo-Json | Set-Content -LiteralPath (Join-Path $OutputPath 'release-manifest.json') -Encoding utf8
Write-Host "Release ready: $OutputPath"
