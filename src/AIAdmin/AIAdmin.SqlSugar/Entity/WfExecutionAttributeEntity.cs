// Copyright © 2023-present https://github.com/dymproject/purest-admin作者以及贡献者

namespace AIAdmin.SqlSugar.Entity;

/// <summary>
/// 自定义属性
/// </summary>
[SugarTable("wf_execution_attribute")]
public partial class WfExecutionAttributeEntity
{
    /// <summary>
    /// 
    /// </summary>
    [SugarColumn(ColumnName = "persistence_id", IsPrimaryKey = true)]
    public long PersistenceId { get; set; }
    /// <summary>
    /// 
    /// </summary>
    [SugarColumn(ColumnName = "attribute_key")]
    public string AttributeKey { get; set; }
    /// <summary>
    /// 
    /// </summary>
    [SugarColumn(ColumnName = "attribute_value")]
    public string AttributeValue { get; set; }
    /// <summary>
    /// 
    /// </summary>
    [SugarColumn(ColumnName = "execution_pointer_id")]
    public long ExecutionPointerId { get; set; }
}