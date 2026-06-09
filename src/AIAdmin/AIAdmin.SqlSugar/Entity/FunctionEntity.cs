// Copyright © 2023-present https://github.com/dymproject/purest-admin作者以及贡献者

namespace AIAdmin.SqlSugar.Entity;

/// <summary>
/// 功能实体
/// </summary>
[SugarTable("sys_function")]
public partial class FunctionEntity : BaseEntity
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
    /// 隶属于
    /// </summary>
    [SugarColumn(ColumnName = "parent_id")]
    public long? ParentId { get; set; }
}