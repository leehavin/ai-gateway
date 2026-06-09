// Copyright © 2023-present https://github.com/dymproject/purest-admin作者以及贡献者

namespace AIAdmin.SqlSugar.Entity;
/// <summary>
/// 通知公告表
/// </summary>
[SugarTable("sys_notice")]
public partial class NoticeEntity : BaseEntity
{
    /// <summary>
    /// 标题
    /// </summary>
    [SugarColumn(ColumnName = "title")]
    public string Title { get; set; }
    /// <summary>
    /// 内容
    /// </summary>
    [SugarColumn(ColumnName = "content")]
    public string Content { get; set; }
    /// <summary>
    /// 类型
    /// </summary>
    [SugarColumn(ColumnName = "notice_type")]
    public long NoticeType { get; set; }
    /// <summary>
    /// 级别
    /// </summary>
    [SugarColumn(ColumnName = "level")]
    public long Level { get; set; }
}
