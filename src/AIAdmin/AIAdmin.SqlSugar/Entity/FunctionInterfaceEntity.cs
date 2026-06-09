// Copyright © 2023-present https://github.com/dymproject/purest-admin作者以及贡献者

namespace AIAdmin.SqlSugar.Entity;

/// <summary>
/// 页面接口
/// </summary>
[SugarTable("sys_function_interface")]
public partial class FunctionInterfaceEntity : BaseEntity
{
    /// <summary>
    /// 功能ID
    /// </summary>
    [SugarColumn(ColumnName = "function_id")]
    public long FunctionId { get; set; }
    /// <summary>
    /// 接口ID
    /// </summary>
    [SugarColumn(ColumnName = "interface_id")]
    public long InterfaceId { get; set; }
}