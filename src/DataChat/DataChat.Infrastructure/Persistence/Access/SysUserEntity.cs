using SqlSugar;

namespace DataChat.Infrastructure.Persistence.Access;

[SugarTable("sys_user")]
public sealed class SysUserEntity
{
    [SugarColumn(IsPrimaryKey = true)]
    public long Id { get; set; }

    [SugarColumn(ColumnName = "account", Length = 64, IsNullable = false)]
    public string Account { get; set; } = "";

    [SugarColumn(ColumnName = "password", Length = 64, IsNullable = false)]
    public string Password { get; set; } = "";

    [SugarColumn(ColumnName = "name", Length = 64, IsNullable = false)]
    public string Name { get; set; } = "";

    [SugarColumn(ColumnName = "status")]
    public int Status { get; set; }
}
