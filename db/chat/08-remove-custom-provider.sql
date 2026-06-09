/*
  移除连接器 provider 约束中的 custom（管理端已不再提供该类型）。
  可重复执行。

  sqlcmd -S <host> -d chat -U sa -P <pwd> -C -f 65001 -i db\chat\08-remove-custom-provider.sql
*/

SET ANSI_NULLS ON;
SET QUOTED_IDENTIFIER ON;
GO

IF EXISTS (
    SELECT 1 FROM sys.check_constraints
    WHERE name = N'CK_dc_pa_provider' AND parent_object_id = OBJECT_ID(N'dbo.dc_provider_account')
)
    ALTER TABLE dbo.dc_provider_account DROP CONSTRAINT CK_dc_pa_provider;
GO

ALTER TABLE dbo.dc_provider_account
    ADD CONSTRAINT CK_dc_pa_provider CHECK (
        provider IN (N'coze', N'dbgpt', N'openai')
    );
GO

IF EXISTS (
    SELECT 1 FROM sys.check_constraints
    WHERE name = N'CK_dc_agent_provider' AND parent_object_id = OBJECT_ID(N'dbo.dc_agent')
)
    ALTER TABLE dbo.dc_agent DROP CONSTRAINT CK_dc_agent_provider;
GO

IF COL_LENGTH(N'dbo.dc_agent', N'provider') IS NOT NULL
BEGIN
    ALTER TABLE dbo.dc_agent
        ADD CONSTRAINT CK_dc_agent_provider CHECK (
            provider IN (N'coze', N'dbgpt', N'openai')
        );
END
GO
