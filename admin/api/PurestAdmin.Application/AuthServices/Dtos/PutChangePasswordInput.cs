// Copyright © 2023-present https://github.com/dymproject/purest-admin作者以及贡献者

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace PurestAdmin.Application.AuthServices.Dtos;

/// <summary>
/// 修改密码
/// </summary>
public class PutChangePasswordInput
{
    /// <summary>
    /// 旧密码
    /// </summary>
    [Required(ErrorMessage = "旧密码不能为空")]
    public string OldPassword { get; set; }
    /// <summary>
    /// 新密码
    /// </summary>
    [Required(ErrorMessage = "新密码不能为空"), Length(6, 12, ErrorMessage = "密码为6-12位")]
    [RegularExpression("^[a-zA-Z]\\w{6,12}$",ErrorMessage = "密码必须以字母开头，只能包含字母、数字和下划线，长度为6-12位")]
    public string NewPassword { get; set; }
}
