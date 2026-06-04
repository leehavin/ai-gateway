using SqlSugar;

namespace DataChat.Infrastructure.Persistence.Domains;

[SugarTable("dc_global_defaults")]
public sealed class DcGlobalDefaultsEntity
{
    [SugarColumn(IsPrimaryKey = true)]
    public int Id { get; set; } = 1;

    public int Version { get; set; } = 1;

    [SugarColumn(Length = 512, IsNullable = false)]
    public string DbgptBaseUrl { get; set; } = "";

    [SugarColumn(Length = 128, IsNullable = false)]
    public string CozeEndpoint { get; set; } = "api.coze.cn";

    public int TimeoutSeconds { get; set; } = 120;

    public int MaxHistoryTurns { get; set; } = 20;

    public long UpdatedAt { get; set; }
}
