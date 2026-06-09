// Copyright © 2023-present https://github.com/dymproject/purest-admin作者以及贡献者

namespace AIAdmin.SqlSugar.Entity;

/// <summary>
/// 角色页面
/// </summary>
[SugarTable("sys_role_function")]
public partial class RoleFunctionEntity : BaseEntity
{
    /// <summary>
    /// 角色ID
    /// </summary>
    [SugarColumn(ColumnName = "role_id")]
    public long RoleId { get; set; }
    /// <summary>
    /// 功能ID
    /// </summary>
    [SugarColumn(ColumnName = "function_id")]
    public long FunctionId { get; set; }
}