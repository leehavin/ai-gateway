namespace AIAdmin.SqlSugar.Entity;

[SugarTable("dc_role_resource")]
public class RoleResourceEntity
{
    [SugarColumn(ColumnName = "role_id", IsPrimaryKey = true)]
    public long RoleId { get; set; }

    [SugarColumn(ColumnName = "resource_row_id", IsPrimaryKey = true)]
    public long ResourceRowId { get; set; }

    [SugarColumn(ColumnName = "create_by")]
    public long? CreateBy { get; set; }

    [SugarColumn(ColumnName = "create_time")]
    public DateTime CreateTime { get; set; }
}
