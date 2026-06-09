/*
  增量插入「智能体中心」菜单（sys_function）并授权给已拥有 system 权限的角色。
  适用于已跑过 DataSeed、但缺少 agent.* 功能码的库；可重复执行（按 code 幂等）。

  执行:
    sqlcmd -S <host> -d chat -U sa -P <pwd> -C -f 65001 -i db/admin/07-seed-agent-menus.sql

  执行后请重新登录 ai-admin（或清浏览器缓存）以刷新侧栏菜单。
*/

SET ANSI_NULLS ON;
SET QUOTED_IDENTIFIER ON;
GO

DECLARE @now DATETIME2(3) = SYSDATETIME();
DECLARE @cb BIGINT = 0;

/* 固定 ID，避免与现有雪花 ID 冲突（当前 max 约 814320686588019） */
DECLARE @id_agent            BIGINT = 814320686590001;
DECLARE @id_provider         BIGINT = 814320686590002;
DECLARE @id_provider_add     BIGINT = 814320686590003;
DECLARE @id_provider_edit    BIGINT = 814320686590004;
DECLARE @id_provider_view    BIGINT = 814320686590005;
DECLARE @id_provider_del     BIGINT = 814320686590006;
DECLARE @id_manage           BIGINT = 814320686590007;
DECLARE @id_manage_add       BIGINT = 814320686590008;
DECLARE @id_manage_edit      BIGINT = 814320686590009;
DECLARE @id_manage_view      BIGINT = 814320686590010;
DECLARE @id_manage_del       BIGINT = 814320686590011;
DECLARE @id_resource         BIGINT = 814320686590012;
DECLARE @id_resource_add     BIGINT = 814320686590013;
DECLARE @id_resource_edit    BIGINT = 814320686590014;
DECLARE @id_resource_view    BIGINT = 814320686590015;
DECLARE @id_resource_del     BIGINT = 814320686590016;
DECLARE @id_access           BIGINT = 814320686590017;
DECLARE @id_access_view      BIGINT = 814320686590018;
DECLARE @id_access_assign    BIGINT = 814320686590019;

MERGE dbo.sys_function AS t
USING (VALUES
    (@id_agent,         N'智能体中心', N'agent',              NULL),
    (@id_provider,      N'连接器管理', N'agent.provider',     @id_agent),
    (@id_provider_add,  N'连接器新增', N'agent.provider.add', @id_provider),
    (@id_provider_edit, N'连接器编辑', N'agent.provider.edit',@id_provider),
    (@id_provider_view, N'连接器查看', N'agent.provider.view',@id_provider),
    (@id_provider_del,  N'连接器删除', N'agent.provider.delete',@id_provider),
    (@id_manage,        N'智能体管理', N'agent.manage',       @id_agent),
    (@id_manage_add,    N'智能体新增', N'agent.manage.add',   @id_manage),
    (@id_manage_edit,   N'智能体编辑', N'agent.manage.edit',  @id_manage),
    (@id_manage_view,   N'智能体查看', N'agent.manage.view',  @id_manage),
    (@id_manage_del,    N'智能体删除', N'agent.manage.delete',@id_manage),
    (@id_resource,      N'子资源管理', N'agent.resource',     @id_agent),
    (@id_resource_add,  N'子资源新增', N'agent.resource.add', @id_resource),
    (@id_resource_edit, N'子资源编辑', N'agent.resource.edit',@id_resource),
    (@id_resource_view, N'子资源查看', N'agent.resource.view',@id_resource),
    (@id_resource_del,  N'子资源删除', N'agent.resource.delete',@id_resource),
    (@id_access,        N'智能体授权', N'agent.access',       @id_agent),
    (@id_access_view,   N'授权查看',   N'agent.access.view',  @id_access),
    (@id_access_assign, N'授权分配',   N'agent.access.assign',@id_access)
) AS s(id, name, code, parent_id)
ON t.code = s.code
WHEN MATCHED THEN
    UPDATE SET name = s.name, parent_id = s.parent_id, update_time = @now
WHEN NOT MATCHED THEN
    INSERT (id, create_by, create_time, name, code, parent_id)
    VALUES (s.id, @cb, @now, s.name, s.code, s.parent_id);
GO

/* 授权：凡已拥有 system 菜单的角色，同步获得 agent.* 权限 */
DECLARE @rf_base BIGINT = 814320686593001;

INSERT INTO dbo.sys_role_function (id, create_by, create_time, role_id, function_id)
SELECT
    @rf_base + ROW_NUMBER() OVER (ORDER BY r.id, f.id) - 1,
    0,
    SYSDATETIME(),
    r.id,
    f.id
FROM dbo.sys_role r
INNER JOIN dbo.sys_role_function rf0 ON rf0.role_id = r.id
INNER JOIN dbo.sys_function sf0 ON sf0.id = rf0.function_id AND sf0.code = N'system'
CROSS JOIN dbo.sys_function f
WHERE f.code LIKE N'agent%'
  AND NOT EXISTS (
      SELECT 1 FROM dbo.sys_role_function x
      WHERE x.role_id = r.id AND x.function_id = f.id
  );
GO

PRINT N'智能体中心菜单已写入 sys_function，并授权给拥有 system 的角色。请重新登录 ai-admin。';
GO
