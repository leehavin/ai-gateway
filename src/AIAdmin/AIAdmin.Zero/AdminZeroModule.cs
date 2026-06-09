// Copyright © 2023-present https://github.com/dymproject/purest-admin作者以及贡献者

using Microsoft.Extensions.DependencyInjection;

using AIAdmin.Core;
using AIAdmin.SqlSugar;

using Volo.Abp;
using Volo.Abp.Modularity;
using Volo.Abp.Timing;

namespace AIAdmin.Zero;
[DependsOn(typeof(AdminSqlSugarModule), typeof(AdminCoreModule))]
public class AdminZeroModule : AbpModule
{
    public override void ConfigureServices(ServiceConfigurationContext context)
    {
        context.Services.AddSingleton<IClock, Clock>();
    }
    public override void OnApplicationInitialization(ApplicationInitializationContext context)
    {

    }
}
