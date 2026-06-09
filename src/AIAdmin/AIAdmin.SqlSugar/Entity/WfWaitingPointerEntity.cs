// Copyright © 2023-present https://github.com/dymproject/purest-admin作者以及贡献者

namespace AIAdmin.SqlSugar.Entity;

/// <summary>
/// 待审批数据表
/// </summary>
[SugarTable("wf_waiting_pointer")]
public partial class WfWaitingPointerEntity
{
    /// <summary>
    /// 主键Id
    /// </summary>
    [SugarColumn(ColumnName = "id", IsPrimaryKey = true)]
    public long Id { get; set; }
    /// <summary>
    /// 用户Id
    /// </summary>
    [SugarColumn(ColumnName = "user_id")]
    public long UserId { get; set; }
    /// <summary>
    /// 步骤Id
    /// </summary>
    [SugarColumn(ColumnName = "pointer_id")]
    public string PointerId { get; set; }
}