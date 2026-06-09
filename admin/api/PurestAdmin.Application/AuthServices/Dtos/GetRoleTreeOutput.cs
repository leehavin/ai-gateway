// Copyright © 2023-present https://github.com/dymproject/purest-admin作者以及贡献者

using PurestAdmin.Application.RoleServices.Dtos;

namespace PurestAdmin.Application.AuthServices.Dtos;

/// <summary>
/// 角色详情
/// </summary>
public class GetRoleTreeOutput
{
    /// <summary>
    /// 主键Id
    /// </summary>
    public long Id { get; set; }
    /// <summary>
    /// 备注
    /// </summary>
    public string Remark { get; set; }
    /// <summary>
    /// 角色名称
    /// </summary>
    public string Name { get; set; }
    /// <summary>
    /// 角色描述
    /// </summary>
    public string Description { get; set; }
    /// <summary>
    /// 父级Id
    /// </summary>
    public long ParentId { get; set; }
    /// <summary>
    /// 子集
    /// </summary>
    public List<RoleOutput> Children { get; set; }
}