/*
  仅最小登录数据（能登录，但左侧菜单为空）。

  完整后台请用 AIAdmin.Zero 初始化（组织/用户/角色/菜单/字典）:
    dotnet run --project src/AIAdmin/AIAdmin.Zero -- --seed

  本脚本仅适合无法跑 Zero 时的应急登录；密码 123456 (md5: e10adc...883e)
*/

IF NOT EXISTS (SELECT 1 FROM dbo.sys_organization WHERE id = 1)
    INSERT INTO dbo.sys_organization (id, create_by, create_time, name, parent_id, leader, sort)
    VALUES (1, 0, SYSDATETIME(), N'AIAdmin', NULL, N'admin', 0);
GO

IF NOT EXISTS (SELECT 1 FROM dbo.sys_role WHERE id = 1)
    INSERT INTO dbo.sys_role (id, create_by, create_time, name, description, parent_id)
    VALUES (1, 0, SYSDATETIME(), N'SuperAdmin', N'System administrator', NULL);
GO

IF NOT EXISTS (SELECT 1 FROM dbo.sys_user WHERE account = N'admin')
    INSERT INTO dbo.sys_user (
        id, create_by, create_time, account, password, name,
        telephone, email, avatar, status, organization_id)
    VALUES (
        2, 0, SYSDATETIME(), N'admin', N'e10adc3949ba59abbe56e057f20f883e', N'Administrator',
        NULL, NULL, NULL, 0, 1);
GO

IF NOT EXISTS (SELECT 1 FROM dbo.sys_user_role WHERE user_id = 2 AND role_id = 1)
    INSERT INTO dbo.sys_user_role (id, create_by, create_time, role_id, user_id)
    VALUES (3, 0, SYSDATETIME(), 1, 2);
GO

SELECT id, account, name, status, organization_id FROM dbo.sys_user WHERE account = N'admin';
GO
