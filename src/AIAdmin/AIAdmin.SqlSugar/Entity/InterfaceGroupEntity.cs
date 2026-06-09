// Copyright © 2023-present https://github.com/dymproject/purest-admin作者以及贡献者

namespace AIAdmin.SqlSugar.Entity;

/// <summary>
/// 接口表
/// </summary>
[SugarTable("sys_interface_group")]
public partial class InterfaceGroupEntity : BaseEntity
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
}