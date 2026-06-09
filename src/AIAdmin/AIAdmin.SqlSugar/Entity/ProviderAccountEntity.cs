namespace AIAdmin.SqlSugar.Entity;

/// <summary>
/// 连接器账号（Coze / DB-GPT / Custom / OpenAI）
/// </summary>
[SugarTable("dc_provider_account")]
public partial class ProviderAccountEntity : BaseEntity
{
    [SugarColumn(ColumnName = "provider")]
    public string Provider { get; set; } = "";

    [SugarColumn(ColumnName = "name")]
    public string Name { get; set; } = "";

    [SugarColumn(ColumnName = "endpoint")]
    public string? Endpoint { get; set; }

    [SugarColumn(ColumnName = "api_key_ciphertext")]
    public string? ApiKeyCiphertext { get; set; }

    [SugarColumn(ColumnName = "config_json")]
    public string? ConfigJson { get; set; }

    [SugarColumn(ColumnName = "sort_order")]
    public int SortOrder { get; set; }

    [SugarColumn(ColumnName = "enabled")]
    public bool Enabled { get; set; } = true;
}
