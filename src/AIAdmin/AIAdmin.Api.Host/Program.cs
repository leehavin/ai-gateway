// Copyright © 2023-present https://github.com/dymproject/purest-admin作者以及贡献者

using AIAdmin.Api.Host;

var builder = WebApplication.CreateBuilder(args);

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