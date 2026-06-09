// Copyright © 2023-present https://github.com/dymproject/purest-admin作者以及贡献者

namespace AIAdmin.SqlSugar.Entity;

/// <summary>
/// 流程审批记录
/// </summary>
[SugarTable("wf_auditing_record")]
public partial class WfAuditingRecordEntity
{
    /// <summary>
    /// 主键Id
    /// </summary>
    [SugarColumn(ColumnName = "id", IsPrimaryKey = true)]
    public long Id { get; set; }
    /// <summary>
    /// 步骤Id
    /// </summary>
    [SugarColumn(ColumnName = "execution_pointer_id")]
    public long ExecutionPointerId { get; set; }
    /// <summary>
    /// 审批时间
    /// </summary>
    [SugarColumn(ColumnName = "auditing_time")]
    public DateTime AuditingTime { get; set; }
    /// <summary>
    /// 审批人
    /// </summary>
    [SugarColumn(ColumnName = "auditor")]
    public long Auditor { get; set; }
    /// <summary>
    /// 审批人姓名
    /// </summary>
    [SugarColumn(ColumnName = "auditor_name")]
    public string AuditorName { get; set; }
    /// <summary>
    /// 审批意见
    /// </summary>
    [SugarColumn(ColumnName = "auditing_opinion")]
    public string AuditingOpinion { get; set; }
    /// <summary>
    /// 是否同意
    /// </summary>
    [SugarColumn(ColumnName = "is_agree")]
    public bool IsAgree { get; set; }
}