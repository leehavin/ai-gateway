// Copyright © 2023-present https://github.com/dymproject/purest-admin作者以及贡献者

namespace AIAdmin.SqlSugar.Entity;

/// <summary>
/// 字典分类
/// </summary>
[SugarTable("sys_dict_category")]
public partial class DictCategoryEntity : BaseEntity
{
    /// <summary>
    /// 分类名称
    /// </summary>
    [SugarColumn(ColumnName = "name")]
    public string Name { get; set; }
    /// <summary>
    /// 分类编码
    /// </summary>
    [SugarColumn(ColumnName = "code")]
    public string Code { get; set; }
}