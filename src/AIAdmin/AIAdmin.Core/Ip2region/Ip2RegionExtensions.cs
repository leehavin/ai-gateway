// Copyright © 2023-present https://github.com/dymproject/purest-admin作者以及贡献者

using IP2Region.Net.Abstractions;
using IP2Region.Net.XDB;

using Microsoft.Extensions.DependencyInjection;

namespace AIAdmin.Core.Ip2region;

/// <summary>
/// ip离线地址库
/// </summary>
/// https://github.com/lionsoul2014/ip2region
public static class Ip2regionExtensions
{
    /// <summary>
    /// 注册 IP 离线地址查询服务
    /// </summary>
    public static IServiceCollection AddIp2region(this IServiceCollection services)
    {
        services.AddSingleton<ISearcher>(new Searcher(
            CachePolicy.Content,
            Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "Ip2region", "ip2region.xdb")));
        return services;
    }
}
