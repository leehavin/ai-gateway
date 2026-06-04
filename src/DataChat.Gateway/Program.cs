using DataChat.Gateway.Configuration;
using DataChat.Gateway.Extensions;
using Microsoft.OpenApi.Models;

var builder = WebApplication.CreateBuilder(args);
builder.Configuration.AddJsonFile("appsettings.Development.local.json", optional: true, reloadOnChange: true);

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

if (gatewayOptions.EnableSwagger || app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI(c => c.SwaggerEndpoint("/swagger/v1/swagger.json", "DataChat Gateway v1"));
}

app.UseCors("Embed");
app.UseDataChatGateway();
app.MapControllers();

app.Run();
