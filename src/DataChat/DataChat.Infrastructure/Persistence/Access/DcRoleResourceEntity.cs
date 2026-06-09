using SqlSugar;

namespace DataChat.Infrastructure.Persistence.Access;

[SugarTable("dc_role_resource")]
public sealed class DcRoleResourceEntity
{
    [SugarColumn(ColumnName = "role_id", IsPrimaryKey = true)]
    public long RoleId { get; set; }

    [SugarColumn(ColumnName = "resource_row_id", IsPrimaryKey = true)]
    public long ResourceRowId { get; set; }
}
