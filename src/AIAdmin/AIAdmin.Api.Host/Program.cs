// Copyright © 2023-present https://github.com/dymproject/purest-admin作者以及贡献者

using AIAdmin.Api.Host;

#if DEBUG
// 直接运行 bin/Debug 下的 exe 不会读取 launchSettings.json，默认会变成 Production
if (string.IsNullOrWhiteSpace(Environment.GetEnvironmentVariable("ASPNETCORE_ENVIRONMENT"))
    && string.IsNullOrWhiteSpace(Environment.GetEnvironmentVariable("DOTNET_ENVIRONMENT")))
{
    Environment.SetEnvironmentVariable("ASPNETCORE_ENVIRONMENT", "Development");
}
#endif

// exe 同目录为 ContentRoot；wwwroot 必须存在（可为空），否则 StaticWebAssets / FileProvider 启动即崩溃
var contentRoot = AppContext.BaseDirectory.TrimEnd(Path.DirectorySeparatorChar, Path.AltDirectorySeparatorChar);
var webRoot = Path.Combine(contentRoot, "wwwroot");
Directory.CreateDirectory(webRoot);

var builder = WebApplication.CreateBuilder(new WebApplicationOptions
{
    Args = args,
    ContentRootPath = contentRoot,
    WebRootPath = webRoot,
});

builder.Host.UseAutofac();

builder.Services.ReplaceConfiguration(builder.Configuration);

builder.Services.AddApplication<AdminHostModule>();

var app = builder.Build();
app.InitializeApplication();

// hash 路由 SPA：未匹配 /api、/swagger、/signalr-hubs 的请求回退到 index.html
var indexHtml = Path.Combine(app.Environment.WebRootPath ?? string.Empty, "index.html");
if (File.Exists(indexHtml))
{
    app.MapFallbackToFile("index.html");
}

app.Run();