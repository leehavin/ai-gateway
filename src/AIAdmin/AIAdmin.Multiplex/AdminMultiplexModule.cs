// Copyright © 2023-present https://github.com/dymproject/purest-admin作者以及贡献者


using AIAdmin.Multiplex.File;
using AIAdmin.Multiplex.File.Containers;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

using AIAdmin.Multiplex.AI;
using AIAdmin.SqlSugar;

using Volo.Abp;
using Volo.Abp.BlobStoring;
using Volo.Abp.BlobStoring.FileSystem;
using Volo.Abp.Modularity;
namespace AIAdmin.Multiplex
{
    [DependsOn(typeof(AdminSqlSugarModule))]
    public class AdminMultiplexModule : AbpModule
    {
        public override void ConfigureServices(ServiceConfigurationContext context)
        {
            var configuration = context.Services.GetConfiguration();

            context.Services.AddAIService(configuration);

            context.Services.AddFileStorage();

            ConfigFileStorage(configuration);
        }

        private void ConfigFileStorage(IConfiguration configuration)
        {
            Configure<AbpBlobStoringOptions>(options =>
            {
                var fileSystemOptions = configuration.GetRequiredSection(nameof(FileSystemOptions)).Get<FileSystemOptions>();
                options.Containers.Configure<ProfileSystemContainer>(container =>
                {
                    container.UseFileSystem(fileSystem =>
                    {
                        fileSystem.BasePath = fileSystemOptions.Path ?? Path.Combine(AppContext.BaseDirectory, "FileSystem");
                    });
                });
            });
        }
    }
}
