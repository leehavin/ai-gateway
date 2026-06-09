// Copyright © 2023-present https://github.com/dymproject/purest-admin作者以及贡献者

namespace AIAdmin.SqlSugar;
public abstract class BaseEntity
{
    /// <summary>
    /// Id
    ///</summary>
    [SugarColumn(ColumnName = "id", IsPrimaryKey = true)]
    public long Id { get; set; }

    /// <summary>
    /// 创建人
    ///</summary>
    [SugarColumn(ColumnName = "create_by")]
    public long CreateBy { get; set; }

    /// <summary>
    /// 创建时间
    ///</summary>
    [SugarColumn(ColumnName = "create_time")]
    public DateTime CreateTime { get; set; }

    /// <summary>
    /// 修改人
    ///</summary>
    [SugarColumn(ColumnName = "update_by")]
    public long? UpdateBy { get; set; }

    /// <summary>
    /// 修改时间
    ///</summary>
    [SugarColumn(ColumnName = "update_time")]
    public DateTime? UpdateTime { get; set; }

    /// <summary>
    /// 备注 
    ///</summary>
    [SugarColumn(ColumnName = "remark")]
    public string Remark { get; set; }

}
