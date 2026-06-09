/*
  移除「子资源管理」独立菜单（工作流已并入智能体管理）。
  可重复执行。执行后请重新登录 ai-admin。
*/
SET ANSI_NULLS ON;
SET QUOTED_IDENTIFIER ON;
GO

DELETE rf
FROM dbo.sys_role_function rf
INNER JOIN dbo.sys_function f ON f.id = rf.function_id
WHERE f.code LIKE N'agent.resource%';
GO

DELETE FROM dbo.sys_function WHERE code LIKE N'agent.resource%';
GO

PRINT N'已移除 agent.resource* 菜单。请重新登录 ai-admin。';
GO
