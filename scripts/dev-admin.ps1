# AIAdmin 本地开发：后端 :5062 + 前端 Vite :5777
# 用法：.\scripts\dev-admin.ps1
# 浏览器打开 http://localhost:5777/ （API 由 Vite 代理到 5062）

$ErrorActionPreference = "Stop"
$RepoRoot = Resolve-Path (Join-Path $PSScriptRoot "..")
$HostProject = Join-Path $RepoRoot "src\AIAdmin\AIAdmin.Api.Host\AIAdmin.Api.Host.csproj"
$WebAdminDir = Join-Path $RepoRoot "web\ai-admin"

Write-Host "==> 启动 AIAdmin API (http://localhost:5062) ..." -ForegroundColor Cyan
$api = Start-Process -FilePath "dotnet" `
    -ArgumentList "run --project `"$HostProject`"" `
    -WorkingDirectory $RepoRoot `
    -PassThru `
    -WindowStyle Normal

Start-Sleep -Seconds 3

Write-Host "==> 启动 web-ele Vite (http://localhost:5777) ..." -ForegroundColor Cyan
Write-Host "    按 Ctrl+C 结束前端；API 窗口请手动关闭" -ForegroundColor Yellow

Push-Location $WebAdminDir
try {
    pnpm dev:ele
}
finally {
    Pop-Location
    if (-not $api.HasExited) {
        Write-Host "==> 关闭 API 进程 (PID $($api.Id))" -ForegroundColor Cyan
        Stop-Process -Id $api.Id -Force -ErrorAction SilentlyContinue
    }
}
