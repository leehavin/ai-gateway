using DataChat.Gateway.Configuration;
using DataChat.Gateway.Extensions;
using Microsoft.OpenApi.Models;
using Serilog;
using Serilog.Events;

// 启动早期日志：捕获 Host 构建期间（DI/配置）异常，应用配置就绪后再替换为正式 Logger。
Log.Logger = new LoggerConfiguration()
    .MinimumLevel.Information()
    .WriteTo.Console()
    .CreateBootstrapLogger();

try
{
    Log.Information("DataChat Gateway 正在启动…");
    RunApp(args);
    return 0;
}
catch (Exception ex)
{
    Log.Fatal(ex, "DataChat Gateway 启动失败，进程终止。");
    return 1;
}
finally
{
    Log.CloseAndFlush();
}

static void RunApp(string[] args)
{
#if DEBUG
    if (string.IsNullOrWhiteSpace(Environment.GetEnvironmentVariable("ASPNETCORE_ENVIRONMENT"))
        && string.IsNullOrWhiteSpace(Environment.GetEnvironmentVariable("DOTNET_ENVIRONMENT")))
    {
        Environment.SetEnvironmentVariable("ASPNETCORE_ENVIRONMENT", "Development");
    }
#endif

    var contentRoot = AppContext.BaseDirectory.TrimEnd(Path.DirectorySeparatorChar, Path.AltDirectorySeparatorChar);
    Directory.CreateDirectory(Path.Combine(contentRoot, "logs"));
    Directory.CreateDirectory(Path.Combine(contentRoot, "uploads"));
    Directory.CreateDirectory(Path.Combine(contentRoot, "data"));

    var builder = WebApplication.CreateBuilder(new WebApplicationOptions
    {
        Args = args,
        ContentRootPath = contentRoot,
    });

// 从配置（appsettings.json 的 Serilog 节）读取日志设置，并补充运行时上下文 enrich。
builder.Host.UseSerilog((context, services, configuration) => configuration
    .ReadFrom.Configuration(context.Configuration)
    .ReadFrom.Services(services)
    .Enrich.FromLogContext()
    .Enrich.WithMachineName()
    .Enrich.WithThreadId()
    .Enrich.WithProperty("Application", "DataChat.Gateway"));

builder.Services.AddControllers();
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen(options =>
{
    options.SwaggerDoc("v1", new OpenApiInfo
    {
        Title = "DataChat Gateway API",
        Version = "v1",
        Description = "Web 嵌入与 WinForms 共用的聊天网关。流式对话、领域配置、DB-GPT 资源代理。"
    });
    options.AddSecurityDefinition("Bearer", new OpenApiSecurityScheme
    {
        Description = "业务系统 Token，如：Bearer demo-token",
        Name = "Authorization",
        In = ParameterLocation.Header,
        Type = SecuritySchemeType.ApiKey,
        Scheme = "Bearer"
    });
    options.AddSecurityRequirement(new OpenApiSecurityRequirement
    {
        {
            new OpenApiSecurityScheme
            {
                Reference = new OpenApiReference { Type = ReferenceType.SecurityScheme, Id = "Bearer" }
            },
            Array.Empty<string>()
        }
    });
});

var gatewaySection = builder.Configuration.GetSection(GatewayOptions.SectionName);
builder.Services.Configure<GatewayOptions>(gatewaySection);
var gatewayOptions = gatewaySection.Get<GatewayOptions>() ?? new GatewayOptions();

builder.Services.AddCors(options =>
{
    options.AddPolicy("Embed", policy =>
    {
        if (gatewayOptions.AllowedOrigins.Contains("*"))
            policy.AllowAnyOrigin().AllowAnyHeader().AllowAnyMethod();
        else
            policy.WithOrigins(gatewayOptions.AllowedOrigins)
                .AllowAnyHeader()
                .AllowAnyMethod()
                .AllowCredentials();
    });
});

builder.Services.AddDataChatGateway(builder.Configuration);

var app = builder.Build();

Log.Information("DataChat Gateway 当前环境：{Environment}，ContentRoot={ContentRoot}",
    app.Environment.EnvironmentName, app.Environment.ContentRootPath);

// 结构化请求日志：每个 HTTP 请求一行，含方法/路径/状态码/耗时。
app.UseSerilogRequestLogging(options =>
{
    options.MessageTemplate =
        "HTTP {RequestMethod} {RequestPath} 响应 {StatusCode}，耗时 {Elapsed:0.0} ms";
    options.GetLevel = (httpContext, elapsed, ex) =>
        ex is not null || httpContext.Response.StatusCode >= 500
            ? LogEventLevel.Error
            : httpContext.Response.StatusCode >= 400
                ? LogEventLevel.Warning
                : LogEventLevel.Information;
    options.EnrichDiagnosticContext = (diagnosticContext, httpContext) =>
    {
        diagnosticContext.Set("ClientIP", httpContext.Connection.RemoteIpAddress?.ToString());
        diagnosticContext.Set("UserAgent", httpContext.Request.Headers.UserAgent.ToString());
    };
});

if (gatewayOptions.EnableSwagger || app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI(c => c.SwaggerEndpoint("/swagger/v1/swagger.json", "DataChat Gateway v1"));
}

app.UseCors("Embed");
app.UseDataChatGateway();
app.MapControllers();

app.Run();
}
