// Copyright © 2023-present https://github.com/dymproject/purest-admin作者以及贡献者

namespace PurestAdmin.SqlSugar.Entity;

/// <summary>
/// 角色实体
/// </summary>
public partial class RoleEntity
{
    /// <summary>
    /// 子集
    /// </summary>
    [SugarColumn(IsIgnore = true)]
    public List<RoleEntity> Children { get; set; }
}