// Copyright © 2023-present https://github.com/dymproject/purest-admin作者以及贡献者

namespace AIAdmin.SqlSugar.Entity;

/// <summary>
/// 步骤
/// </summary>
[SugarTable("wf_execution_pointer")]
public partial class WfExecutionPointerEntity
{
    /// <summary>
    /// 
    /// </summary>
    [SugarColumn(ColumnName = "persistence_id", IsPrimaryKey = true)]
    public long PersistenceId { get; set; }
    /// <summary>
    /// 
    /// </summary>
    [SugarColumn(ColumnName = "active")]
    public bool Active { get; set; }
    /// <summary>
    /// 
    /// </summary>
    [SugarColumn(ColumnName = "retry_count")]
    public int RetryCount { get; set; }
    /// <summary>
    /// 
    /// </summary>
    [SugarColumn(ColumnName = "end_time")]
    public DateTime? EndTime { get; set; }
    /// <summary>
    /// 
    /// </summary>
    [SugarColumn(ColumnName = "event_data")]
    public string EventData { get; set; }
    /// <summary>
    /// 
    /// </summary>
    [SugarColumn(ColumnName = "event_key")]
    public string EventKey { get; set; }
    /// <summary>
    /// 
    /// </summary>
    [SugarColumn(ColumnName = "event_name")]
    public string EventName { get; set; }
    /// <summary>
    /// 
    /// </summary>
    [SugarColumn(ColumnName = "event_published")]
    public bool EventPublished { get; set; }
    /// <summary>
    /// 
    /// </summary>
    [SugarColumn(ColumnName = "id")]
    public string Id { get; set; }
    /// <summary>
    /// 
    /// </summary>
    [SugarColumn(ColumnName = "persistence_data")]
    public string PersistenceData { get; set; }
    /// <summary>
    /// 
    /// </summary>
    [SugarColumn(ColumnName = "sleep_until")]
    public DateTime? SleepUntil { get; set; }
    /// <summary>
    /// 
    /// </summary>
    [SugarColumn(ColumnName = "start_time")]
    public DateTime? StartTime { get; set; }
    /// <summary>
    /// 
    /// </summary>
    [SugarColumn(ColumnName = "step_id")]
    public int StepId { get; set; }
    /// <summary>
    /// 
    /// </summary>
    [SugarColumn(ColumnName = "step_name")]
    public string StepName { get; set; }
    /// <summary>
    /// 
    /// </summary>
    [SugarColumn(ColumnName = "workflow_id")]
    public long WorkflowId { get; set; }
    /// <summary>
    /// 
    /// </summary>
    [SugarColumn(ColumnName = "children")]
    public string Children { get; set; }
    /// <summary>
    /// 
    /// </summary>
    [SugarColumn(ColumnName = "context_item")]
    public string ContextItem { get; set; }
    /// <summary>
    /// 
    /// </summary>
    [SugarColumn(ColumnName = "predecessor_id")]
    public string PredecessorId { get; set; }
    /// <summary>
    /// 
    /// </summary>
    [SugarColumn(ColumnName = "outcome")]
    public string Outcome { get; set; }
    /// <summary>
    /// 
    /// </summary>
    [SugarColumn(ColumnName = "scope")]
    public string Scope { get; set; }
    /// <summary>
    /// 
    /// </summary>
    [SugarColumn(ColumnName = "status")]
    public int Status { get; set; }
}