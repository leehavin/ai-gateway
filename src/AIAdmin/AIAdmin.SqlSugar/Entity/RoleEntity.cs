// Copyright © 2023-present https://github.com/dymproject/purest-admin作者以及贡献者

namespace AIAdmin.SqlSugar.Entity;

/// <summary>
/// 角色
/// </summary>
[SugarTable("sys_role")]
public partial class RoleEntity : BaseEntity
{
    /// <summary>
    /// 角色名称
    /// </summary>
    [SugarColumn(ColumnName = "name")]
    public string Name { get; set; }
    /// <summary>
    /// 角色描述
    /// </summary>
    [SugarColumn(ColumnName = "description")]
    public string Description { get; set; }
    /// <summary>
    /// 父级Id
    /// </summary>
    [SugarColumn(ColumnName = "parent_id")]
    public long? ParentId { get; set; }
}