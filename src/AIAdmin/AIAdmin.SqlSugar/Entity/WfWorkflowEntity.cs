// Copyright © 2023-present https://github.com/dymproject/purest-admin作者以及贡献者

namespace AIAdmin.SqlSugar.Entity;

/// <summary>
/// 工作流程
/// </summary>
[SugarTable("wf_workflow")]
public partial class WfWorkflowEntity
{
    /// <summary>
    /// 
    /// </summary>
    [SugarColumn(ColumnName = "persistence_id", IsPrimaryKey = true)]
    public long PersistenceId { get; set; }
    /// <summary>
    /// 
    /// </summary>
    [SugarColumn(ColumnName = "complete_time")]
    public DateTime? CompleteTime { get; set; }
    /// <summary>
    /// 
    /// </summary>
    [SugarColumn(ColumnName = "data")]
    public string Data { get; set; }
    /// <summary>
    /// 
    /// </summary>
    [SugarColumn(ColumnName = "description")]
    public string Description { get; set; }
    /// <summary>
    /// 
    /// </summary>
    [SugarColumn(ColumnName = "instance_id")]
    public string InstanceId { get; set; }
    /// <summary>
    /// 
    /// </summary>
    [SugarColumn(ColumnName = "next_execution")]
    public long? NextExecution { get; set; }
    /// <summary>
    /// 
    /// </summary>
    [SugarColumn(ColumnName = "status")]
    public int Status { get; set; }
    /// <summary>
    /// 
    /// </summary>
    [SugarColumn(ColumnName = "version")]
    public int Version { get; set; }
    /// <summary>
    /// 
    /// </summary>
    [SugarColumn(ColumnName = "workflow_definition_id")]
    public string WorkflowDefinitionId { get; set; }
    /// <summary>
    /// 
    /// </summary>
    [SugarColumn(ColumnName = "reference")]
    public string Reference { get; set; }
    /// <summary>
    /// 创建时间
    ///</summary>
    [SugarColumn(ColumnName = "create_time")]
    public DateTime CreateTime { get; set; }
    /// <summary>
    /// 创建人
    ///</summary>
    [SugarColumn(ColumnName = "create_by")]
    public long CreateBy { get; set; }
    /// <summary>
    /// 备注 
    ///</summary>
    [SugarColumn(ColumnName = "remark")]
    public string Remark { get; set; }
}