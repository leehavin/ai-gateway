# AIAdmin 一体化发布：web-ele SPA + AIAdmin.Api.Host
# 用法：
#   .\scripts\publish-admin.ps1
#   .\scripts\publish-admin.ps1 -OutputDir D:\release\admin -SkipFrontendBuild

param(
    [string]$OutputDir = "$PSScriptRoot\..\publish\admin",
    [switch]$SkipFrontendBuild
)

$ErrorActionPreference = "Stop"
$RepoRoot = Resolve-Path (Join-Path $PSScriptRoot "..")
$WebAdminDir = Join-Path $RepoRoot "web\ai-admin"
$HostProject = Join-Path $RepoRoot "src\AIAdmin\AIAdmin.Api.Host\AIAdmin.Api.Host.csproj"

Write-Host "==> AIAdmin SPA integrated publish" -ForegroundColor Cyan
Write-Host "    Repo:   $RepoRoot"
Write-Host "    Output: $OutputDir"

if (-not $SkipFrontendBuild) {
    Write-Host "==> pnpm install (web/ai-admin)" -ForegroundColor Cyan
    Push-Location $WebAdminDir
    try {
        if (-not (Get-Command pnpm -ErrorAction SilentlyContinue)) {
            throw "未找到 pnpm。请先安装 Node.js 与 pnpm：npm install -g pnpm"
        }
        pnpm install --frozen-lockfile
        Write-Host "==> pnpm build:ele" -ForegroundColor Cyan
        pnpm run build:ele
    }
    finally {
        Pop-Location
    }
}
else {
    Write-Host "==> Skip frontend build (-SkipFrontendBuild)" -ForegroundColor Yellow
}

Write-Host "==> dotnet publish (BuildAdminSpa=false, dist 已在上面构建)" -ForegroundColor Cyan
dotnet publish $HostProject `
    -c Release `
    -o $OutputDir `
    /p:BuildAdminSpa=false

$indexHtml = Join-Path $OutputDir "wwwroot\index.html"
if (-not (Test-Path $indexHtml)) {
    Write-Warning "发布目录缺少 wwwroot/index.html，请确认 web-ele 构建成功。"
    exit 1
}

Write-Host ""
Write-Host "Done. 启动示例：" -ForegroundColor Green
Write-Host "  `$env:ASPNETCORE_ENVIRONMENT = 'Production'"
Write-Host "  dotnet $OutputDir\AIAdmin.Api.Host.dll"
Write-Host "  浏览器打开 http://localhost:5062/ （API 同域 /api/v1）"
