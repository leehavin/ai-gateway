// Copyright © 2023-present https://github.com/dymproject/purest-admin作者以及贡献者

namespace AIAdmin.SqlSugar.Entity;

/// <summary>
/// 用户角色
/// </summary>
[SugarTable("sys_user_role")]
public partial class UserRoleEntity : BaseEntity
{
    /// <summary>
    /// 角色ID
    /// </summary>
    [SugarColumn(ColumnName = "role_id")]
    public long RoleId { get; set; }
    /// <summary>
    /// 用户ID
    /// </summary>
    [SugarColumn(ColumnName = "user_id")]
    public long UserId { get; set; }
}