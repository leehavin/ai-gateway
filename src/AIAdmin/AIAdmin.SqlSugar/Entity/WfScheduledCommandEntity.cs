// Copyright © 2023-present https://github.com/dymproject/purest-admin作者以及贡献者

namespace AIAdmin.SqlSugar.Entity;

/// <summary>
/// 计划命令
/// </summary>
[SugarTable("wf_scheduled_command")]
public partial class WfScheduledCommandEntity
{
    /// <summary>
    /// 
    /// </summary>
    [SugarColumn(ColumnName = "persistence_id", IsPrimaryKey = true)]
    public long PersistenceId { get; set; }
    /// <summary>
    /// 
    /// </summary>
    [SugarColumn(ColumnName = "command_name")]
    public string CommandName { get; set; }
    /// <summary>
    /// 
    /// </summary>
    [SugarColumn(ColumnName = "data")]
    public string Data { get; set; }
    /// <summary>
    /// 
    /// </summary>
    [SugarColumn(ColumnName = "execute_time")]
    public long ExecuteTime { get; set; }
}