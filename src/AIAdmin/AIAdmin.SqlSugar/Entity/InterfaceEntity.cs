// Copyright © 2023-present https://github.com/dymproject/purest-admin作者以及贡献者

namespace AIAdmin.SqlSugar.Entity;

/// <summary>
/// 接口表
/// </summary>
[SugarTable("sys_interface")]
public partial class InterfaceEntity : BaseEntity
{
    /// <summary>
    /// 接口名称
    /// </summary>
    [SugarColumn(ColumnName = "name")]
    public string Name { get; set; }
    /// <summary>
    /// 接口地址
    /// </summary>
    [SugarColumn(ColumnName = "path")]
    public string Path { get; set; }
    /// <summary>
    /// 请求方法
    /// </summary>
    [SugarColumn(ColumnName = "request_method")]
    public string RequestMethod { get; set; }
    /// <summary>
    /// 分组Id
    /// </summary>
    [SugarColumn(ColumnName = "group_id")]
    public long GroupId { get; set; }
}