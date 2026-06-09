// Copyright © 2023-present https://github.com/dymproject/purest-admin作者以及贡献者

namespace AIAdmin.SqlSugar.Entity;

/// <summary>
/// 流程定义
/// </summary>
[SugarTable("wf_definition")]
public partial class WfDefinitionEntity : BaseEntity
{
    /// <summary>
    /// 名称
    /// </summary>
    [SugarColumn(ColumnName = "name")]
    public string Name { get; set; }
    /// <summary>
    /// 定义ID
    /// </summary>
    [SugarColumn(ColumnName = "definition_id")]
    public string DefinitionId { get; set; }
    /// <summary>
    /// 流程内容
    /// </summary>
    [SugarColumn(ColumnName = "workflow_content")]
    public string WorkflowContent { get; set; }
    /// <summary>
    /// 设计器内容
    /// </summary>
    [SugarColumn(ColumnName = "designs_content")]
    public string DesignsContent { get; set; }
    /// <summary>
    /// 表单内容
    /// </summary>
    [SugarColumn(ColumnName = "form_content")]
    public string FormContent { get; set; }
    /// <summary>
    /// 版本
    /// </summary>
    [SugarColumn(ColumnName = "version")]
    public int Version { get; set; }
    /// <summary>
    /// 是否锁定
    /// </summary>
    [SugarColumn(ColumnName = "is_locked")]
    public bool IsLocked { get; set; }
}