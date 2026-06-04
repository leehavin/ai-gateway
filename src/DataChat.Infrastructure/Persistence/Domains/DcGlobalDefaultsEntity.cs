using SqlSugar;

namespace DataChat.Infrastructure.Persistence.Domains;

[SugarTable("dc_global_defaults")]
public sealed class DcGlobalDefaultsEntity
{
    [SugarColumn(IsPrimaryKey = true)]
    public int Id { get; set; } = 1;

    public int Version { get; set; } = 1;

    [SugarColumn(ColumnName = "dbgpt_base_url", Length = 512, IsNullable = false)]
    public string DbgptBaseUrl { get; set; } = "";

    [SugarColumn(ColumnName = "coze_endpoint", Length = 128, IsNullable = false)]
    public string CozeEndpoint { get; set; } = "api.coze.cn";

    [SugarColumn(ColumnName = "timeout_seconds")]
    public int TimeoutSeconds { get; set; } = 120;

    [SugarColumn(ColumnName = "max_history_turns")]
    public int MaxHistoryTurns { get; set; } = 20;

    [SugarColumn(ColumnName = "updated_at")]
    public long UpdatedAt { get; set; }
}
