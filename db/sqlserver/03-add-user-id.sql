/*
  DataChat — 为已有库增加 user_id（会话按用户隔离）
*/
IF COL_LENGTH('dbo.chat_session', 'user_id') IS NULL
BEGIN
    ALTER TABLE dbo.chat_session ADD user_id NVARCHAR(128) NULL;
END
GO

IF NOT EXISTS (
    SELECT 1 FROM sys.indexes
    WHERE name = N'IX_chat_session_user_id' AND object_id = OBJECT_ID(N'dbo.chat_session')
)
BEGIN
    CREATE NONCLUSTERED INDEX IX_chat_session_user_id ON dbo.chat_session (user_id);
END
GO
