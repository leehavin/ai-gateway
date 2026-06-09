/*
  移除 dc_provider_account.api_key_ref（密钥统一存 api_key_ciphertext）。
  可重复执行。

  执行: sqlcmd -S host -d chat -i db/chat/06-drop-api-key-ref.sql
*/

SET ANSI_NULLS ON;
SET QUOTED_IDENTIFIER ON;
GO

IF COL_LENGTH(N'dbo.dc_provider_account', N'api_key_ref') IS NOT NULL
BEGIN
    ALTER TABLE dbo.dc_provider_account DROP COLUMN api_key_ref;
    PRINT N'已删除 dc_provider_account.api_key_ref';
END
ELSE
    PRINT N'dc_provider_account.api_key_ref 不存在，跳过';
GO
