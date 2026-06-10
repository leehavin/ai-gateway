# DataChat Gateway 发布
# 用法：
#   .\scripts\publish-gateway.ps1
#   .\scripts\publish-gateway.ps1 -OutputDir D:\release\gateway

param(
    [string]$OutputDir = "$PSScriptRoot\..\publish\gateway"
)

$ErrorActionPreference = "Stop"
$RepoRoot = Resolve-Path (Join-Path $PSScriptRoot "..")
$Project = Join-Path $RepoRoot "src\DataChat\DataChat.Gateway\DataChat.Gateway.csproj"

Write-Host "==> DataChat Gateway publish" -ForegroundColor Cyan
Write-Host "    Repo:   $RepoRoot"
Write-Host "    Output: $OutputDir"

dotnet publish $Project -c Release -o $OutputDir

Write-Host ""
Write-Host "Done. 启动示例：" -ForegroundColor Green
Write-Host "  `$env:ASPNETCORE_ENVIRONMENT = 'Production'"
Write-Host "  dotnet $OutputDir\DataChat.Gateway.dll"
Write-Host "  健康检查 http://localhost:5080/v1/health"
Write-Host ""
Write-Host "分发时请打包整个 publish\gateway 目录（含 appsettings.json、logs/uploads 会在运行时创建）。"
