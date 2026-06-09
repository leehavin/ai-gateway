// Copyright © 2023-present https://github.com/dymproject/purest-admin作者以及贡献者

namespace AIAdmin.SqlSugar.Entity;

/// <summary>
/// 订阅
/// </summary>
[SugarTable("wf_subscription")]
public partial class WfSubscriptionEntity
{
    /// <summary>
    /// 
    /// </summary>
    [SugarColumn(ColumnName = "persistence_id", IsPrimaryKey = true)]
    public long PersistenceId { get; set; }
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
    [SugarColumn(ColumnName = "step_id")]
    public int StepId { get; set; }
    /// <summary>
    /// 
    /// </summary>
    [SugarColumn(ColumnName = "subscription_id")]
    public string SubscriptionId { get; set; }
    /// <summary>
    /// 
    /// </summary>
    [SugarColumn(ColumnName = "workflow_id")]
    public string WorkflowId { get; set; }
    /// <summary>
    /// 
    /// </summary>
    [SugarColumn(ColumnName = "subscribe_as_of")]
    public DateTime SubscribeAsOf { get; set; }
    /// <summary>
    /// 
    /// </summary>
    [SugarColumn(ColumnName = "subscription_data")]
    public string SubscriptionData { get; set; }
    /// <summary>
    /// 
    /// </summary>
    [SugarColumn(ColumnName = "execution_pointer_id")]
    public string ExecutionPointerId { get; set; }
    /// <summary>
    /// 
    /// </summary>
    [SugarColumn(ColumnName = "external_token")]
    public string ExternalToken { get; set; }
    /// <summary>
    /// 
    /// </summary>
    [SugarColumn(ColumnName = "external_token_expiry")]
    public DateTime? ExternalTokenExpiry { get; set; }
    /// <summary>
    /// 
    /// </summary>
    [SugarColumn(ColumnName = "external_worker_id")]
    public string ExternalWorkerId { get; set; }
}