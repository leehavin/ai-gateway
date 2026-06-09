using SqlSugar;

namespace DataChat.Infrastructure.Persistence.Access;

[SugarTable("sys_role")]
public sealed class SysRoleEntity
{
    [SugarColumn(IsPrimaryKey = true)]
    public long Id { get; set; }

    [SugarColumn(Length = 64, IsNullable = false)]
    public string Name { get; set; } = "";
}
