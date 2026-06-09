// Copyright © 2023-present https://github.com/dymproject/purest-admin作者以及贡献者

namespace AIAdmin.SqlSugar.Entity;

/// <summary>
/// 事件
/// </summary>
[SugarTable("wf_event")]
public partial class WfEventEntity
{
    /// <summary>
    /// 
    /// </summary>
    [SugarColumn(ColumnName = "persistence_id", IsPrimaryKey = true)]
    public long PersistenceId { get; set; }
    /// <summary>
    /// 
    /// </summary>
    [SugarColumn(ColumnName = "event_id")]
    public string EventId { get; set; }
    /// <summary>
    /// 
    /// </summary>
    [SugarColumn(ColumnName = "event_name")]
    public string EventName { get; set; }
    /// <summary>
    /// 
    /// </summary>
    [SugarColumn(ColumnName = "event_key")]
    public string EventKey { get; set; }
    /// <summary>
    /// 
    /// </summary>
    [SugarColumn(ColumnName = "event_data")]
    public string EventData { get; set; }
    /// <summary>
    /// 
    /// </summary>
    [SugarColumn(ColumnName = "event_time")]
    public DateTime EventTime { get; set; }
    /// <summary>
    /// 
    /// </summary>
    [SugarColumn(ColumnName = "is_processed")]
    public bool IsProcessed { get; set; }
}