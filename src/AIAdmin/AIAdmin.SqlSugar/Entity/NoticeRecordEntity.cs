// Copyright © 2023-present https://github.com/dymproject/purest-admin作者以及贡献者

namespace AIAdmin.SqlSugar.Entity;
/// <summary>
/// 通知公告表
/// </summary>
[SugarTable("sys_notice_record")]
public partial class NoticeRecordEntity : BaseEntity
{
    /// <summary>
    /// 通知公告Id
    /// </summary>
    [SugarColumn(ColumnName = "notice_id")]
    public long NoticeId { get; set; }
    /// <summary>
    /// 接收人
    /// </summary>
    [SugarColumn(ColumnName = "receiver")]
    public long Receiver { get; set; }
    /// <summary>
    /// 是否已读
    /// </summary>
    [SugarColumn(ColumnName = "is_read")]
    public bool IsRead { get; set; }
}
