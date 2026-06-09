using SqlSugar;

namespace DataChat.Infrastructure.Persistence.Access;

[SugarTable("sys_user_role")]
public sealed class SysUserRoleEntity
{
    [SugarColumn(ColumnName = "user_id", IsPrimaryKey = true)]
    public long UserId { get; set; }

    [SugarColumn(ColumnName = "role_id", IsPrimaryKey = true)]
    public long RoleId { get; set; }
}
