# chat-ui 生产构建
# 用法：
#   .\scripts\build-chat-ui.ps1
#   .\scripts\build-chat-ui.ps1 -CopyTo "D:\IPSpace\ipspaceprojects_ipspace\IPSpaceMain\wwwroot\chat-ui"

param(
    [string]$CopyTo = "D:\IPSpace\ipspaceprojects_ipspace\IPSpaceMain\wwwroot\chat-ui"
)

$ErrorActionPreference = "Stop"
$RepoRoot = Resolve-Path (Join-Path $PSScriptRoot "..")
$ChatUiDir = Join-Path $RepoRoot "web\chat-ui"
$DistDir = Join-Path $ChatUiDir "dist"

Write-Host "==> chat-ui build" -ForegroundColor Cyan
Write-Host "    Source: $ChatUiDir"
Write-Host "    Output: $DistDir"

Push-Location $ChatUiDir
try {
    if (-not (Test-Path "node_modules")) {
        Write-Host "    Installing dependencies..." -ForegroundColor Yellow
        npm install
    }
    npm run build
    if ($LASTEXITCODE -ne 0) {
        throw "npm run build failed with exit code $LASTEXITCODE"
    }
}
finally {
    Pop-Location
}

if (-not (Test-Path $DistDir)) {
    throw "Build failed: dist directory not found."
}

Write-Host ""
Write-Host "Done. dist -> $DistDir" -ForegroundColor Green

if ($CopyTo) {
    $dest = $ExecutionContext.SessionState.Path.GetUnresolvedProviderPathFromPSPath($CopyTo)
    Write-Host "    Copying to $dest ..." -ForegroundColor Yellow
    if (-not (Test-Path $dest)) {
        New-Item -ItemType Directory -Path $dest -Force | Out-Null
    }
    robocopy $DistDir $dest /MIR /NFL /NDL /NJH /NJS /NC /NS | Out-Null
    if ($LASTEXITCODE -ge 8) {
        throw "robocopy failed with exit code $LASTEXITCODE"
    }
    Write-Host "    Copied to $dest" -ForegroundColor Green
}

Write-Host ""
Write-Host 'Embed IPSpace example:' -ForegroundColor DarkGray
Write-Host '  .\scripts\build-chat-ui.ps1 -CopyTo D:\path\to\IPSpaceMain\wwwroot\chat-ui'
