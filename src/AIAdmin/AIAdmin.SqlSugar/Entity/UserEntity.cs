// Copyright © 2023-present https://github.com/dymproject/purest-admin作者以及贡献者

namespace AIAdmin.SqlSugar.Entity;

/// <summary>
/// 用户
/// </summary>
[SugarTable("sys_user")]
public partial class UserEntity : BaseEntity
{
    /// <summary>
    /// 账号
    /// </summary>
    [SugarColumn(ColumnName = "account")]
    public string Account { get; set; }
    /// <summary>
    /// 密码
    /// </summary>
    [SugarColumn(ColumnName = "password")]
    public string Password { get; set; }
    /// <summary>
    /// 真实姓名
    /// </summary>
    [SugarColumn(ColumnName = "name")]
    public string Name { get; set; }
    /// <summary>
    /// 电话
    /// </summary>
    [SugarColumn(ColumnName = "telephone")]
    public string Telephone { get; set; }
    /// <summary>
    /// 邮箱
    /// </summary>
    [SugarColumn(ColumnName = "email")]
    public string Email { get; set; }
    /// <summary>
    /// 头像
    /// </summary>
    [SugarColumn(ColumnName = "avatar")]
    public byte[] Avatar { get; set; }
    /// <summary>
    /// 状态
    /// </summary>
    [SugarColumn(ColumnName = "status")]
    public int Status { get; set; }
    /// <summary>
    /// 组织机构Id
    /// </summary>
    [SugarColumn(ColumnName = "organization_id")]
    public long OrganizationId { get; set; }
}