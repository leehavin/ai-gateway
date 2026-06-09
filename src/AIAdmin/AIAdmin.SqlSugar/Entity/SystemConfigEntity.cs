// Copyright © 2023-present https://github.com/dymproject/purest-admin作者以及贡献者

namespace AIAdmin.SqlSugar.Entity;

/// <summary>
/// 系统配置表
/// </summary>
[SugarTable("sys_config")]
public partial class SystemConfigEntity : BaseEntity
{
    /// <summary>
    /// 名称
    /// </summary>
    [SugarColumn(ColumnName = "name")]
    public string Name { get; set; }
    /// <summary>
    /// 编码
    /// </summary>
    [SugarColumn(ColumnName = "config_code")]
    public string ConfigCode { get; set; }
    /// <summary>
    /// 值
    /// </summary>
    [SugarColumn(ColumnName = "config_value")]
    public string ConfigValue { get; set; }
}