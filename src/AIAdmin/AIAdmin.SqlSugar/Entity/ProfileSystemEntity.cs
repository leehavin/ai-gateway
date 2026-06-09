// Copyright © 2023-present https://github.com/dymproject/purest-admin作者以及贡献者

namespace AIAdmin.SqlSugar.Entity;

/// <summary>
/// 系统文件实体
/// </summary>
[SugarTable("sys_profile_system")]
public partial class ProfileSystemEntity : BaseEntity
{
    /// <summary>
    /// 名称
    /// </summary>
    [SugarColumn(ColumnName = "name")]
    public string Name { get; set; }
    /// <summary>
    /// 编码
    /// </summary>
    [SugarColumn(ColumnName = "code")]
    public string Code { get; set; }
    /// <summary>
    /// 文件ID
    /// </summary>
    [SugarColumn(ColumnName = "file_id")]
    public long FileId { get; set; }
}