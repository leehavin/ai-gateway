using SqlSugar;

namespace DataChat.Infrastructure.Persistence.Agents;

[SugarTable("dc_provider_account")]
public sealed class DcProviderAccountEntity
{
    [SugarColumn(IsPrimaryKey = true)]
    public long Id { get; set; }

    [SugarColumn(Length = 32, IsNullable = false)]
    public string Provider { get; set; } = "";

    [SugarColumn(Length = 128, IsNullable = false)]
    public string Name { get; set; } = "";

    [SugarColumn(Length = 512, IsNullable = true)]
    public string? Endpoint { get; set; }

    [SugarColumn(ColumnName = "api_key_ciphertext", ColumnDataType = "TEXT", IsNullable = true)]
    public string? ApiKeyCiphertext { get; set; }

    [SugarColumn(ColumnName = "config_json", ColumnDataType = "TEXT", IsNullable = true)]
    public string? ConfigJson { get; set; }

    [SugarColumn(ColumnName = "sort_order")]
    public int SortOrder { get; set; }

    public bool Enabled { get; set; } = true;
}
