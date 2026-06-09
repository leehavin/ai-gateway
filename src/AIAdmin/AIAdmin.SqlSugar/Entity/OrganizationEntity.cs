// Copyright © 2023-present https://github.com/dymproject/purest-admin作者以及贡献者

namespace AIAdmin.SqlSugar.Entity;

/// <summary>
/// 组织机构
/// </summary>
[SugarTable("sys_organization")]
public partial class OrganizationEntity : BaseEntity
{
    /// <summary>
    /// 名称
    /// </summary>
    [SugarColumn(ColumnName = "name")]
    public string Name { get; set; }
    /// <summary>
    /// 父级Id
    /// </summary>
    [SugarColumn(ColumnName = "parent_id")]
    public long? ParentId { get; set; }
    /// <summary>
    /// 联系电话
    /// </summary>
    [SugarColumn(ColumnName = "telephone")]
    public string Telephone { get; set; }
    /// <summary>
    /// 负责人
    /// </summary>
    [SugarColumn(ColumnName = "leader")]
    public string Leader { get; set; }
    /// <summary>
    /// 排序
    /// </summary>
    [SugarColumn(ColumnName = "sort")]
    public int? Sort { get; set; }
}