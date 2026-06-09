// Copyright © 2023-present https://github.com/dymproject/purest-admin作者以及贡献者

using System.Reflection;

using AIAdmin.BackgroundService;
using AIAdmin.Core.Mapster;
using AIAdmin.Multiplex;

using Volo.Abp.AspNetCore.Mvc;
using Volo.Abp.Modularity;

namespace AIAdmin.Application
{
    [DependsOn(typeof(AdminBackgroundModule),
        typeof(AdminMultiplexModule))]
    public class AdminApplicationModule : AbpModule
    {
        public override void ConfigureServices(ServiceConfigurationContext context)
        {
            context.Services.AddMapsterIRegister(Assembly.GetExecutingAssembly());
            Configure<AbpAspNetCoreMvcOptions>(options =>
            {
                options.ConventionalControllers.Create(typeof(AdminApplicationModule).Assembly, opts =>
                {
                    opts.RootPath = "v1";
                    //opts.UrlActionNameNormalizer = (action) =>
                    //{
                    //    return action.ActionNameInUrl;
                    //};
                });
            });
        }
    }
}
