// Copyright © 2023-present https://github.com/dymproject/purest-admin作者以及贡献者

namespace AIAdmin.SqlSugar.Entity;

/// <summary>
/// 文件上传记录表
///</summary>
[SugarTable("sys_file_record")]
public class FileRecordEntity : BaseEntity
{
    /// <summary>
    /// 文件名 
    ///</summary>
    [SugarColumn(ColumnName = "file_name")]
    public string FileName { get; set; }
    /// <summary>
    /// 文件大小 
    ///</summary>
    [SugarColumn(ColumnName = "file_size")]
    public int FileSize { get; set; }
    /// <summary>
    /// 文件扩展名 
    ///</summary>
    [SugarColumn(ColumnName = "file_ext")]
    public string FileExt { get; set; }
}
