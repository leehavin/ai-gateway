// Copyright © 2023-present https://github.com/dymproject/purest-admin作者以及贡献者

namespace AIAdmin.SqlSugar.Entity;

/// <summary>
/// 字典数据
/// </summary>
[SugarTable("sys_dict_data")]
public partial class DictDataEntity : BaseEntity
{
    /// <summary>
    /// 字典分类ID
    /// </summary>
    [SugarColumn(ColumnName = "category_id")]
    public long CategoryId { get; set; }
    /// <summary>
    /// 字典名称
    /// </summary>
    [SugarColumn(ColumnName = "name")]
    public string Name { get; set; }
    /// <summary>
    /// 字典编码
    /// </summary>
    [SugarColumn(ColumnName = "code")]
    public string Code { get; set; }
    /// <summary>
    /// 排序
    /// </summary>
    [SugarColumn(ColumnName = "sort")]
    public int Sort { get; set; }
}