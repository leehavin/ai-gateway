/*
  DataChat — SQL Server 表结构
  使用前请先创建数据库，例如: CREATE DATABASE Chat;
  执行: sqlcmd -S host -d DataChat -i 01-schema.sql
*/

SET ANSI_NULLS ON;
SET QUOTED_IDENTIFIER ON;
GO

IF OBJECT_ID(N'dbo.chat_message', N'U') IS NOT NULL
    DROP TABLE dbo.chat_message;
GO
IF OBJECT_ID(N'dbo.chat_session', N'U') IS NOT NULL
    DROP TABLE dbo.chat_session;
GO
IF OBJECT_ID(N'dbo.dc_domain', N'U') IS NOT NULL
    DROP TABLE dbo.dc_domain;
GO
IF OBJECT_ID(N'dbo.dc_global_defaults', N'U') IS NOT NULL
    DROP TABLE dbo.dc_global_defaults;
GO

CREATE TABLE dbo.dc_global_defaults (
    id                  INT             NOT NULL,
    version             INT             NOT NULL CONSTRAINT DF_dc_global_defaults_version DEFAULT (1),
    dbgpt_base_url      NVARCHAR(512)   NOT NULL,
    coze_endpoint       NVARCHAR(128)   NOT NULL,
    timeout_seconds     INT             NOT NULL CONSTRAINT DF_dc_global_defaults_timeout DEFAULT (120),
    max_history_turns   INT             NOT NULL CONSTRAINT DF_dc_global_defaults_history DEFAULT (20),
    updated_at          BIGINT          NOT NULL,
    CONSTRAINT PK_dc_global_defaults PRIMARY KEY CLUSTERED (id)
);
GO

CREATE TABLE dbo.dc_domain (
    id                      NVARCHAR(64)    NOT NULL,
    display_name            NVARCHAR(128)   NOT NULL,
    chat_mode               NVARCHAR(64)    NOT NULL,
    provider                NVARCHAR(32)    NOT NULL,
    model                   NVARCHAR(128)   NULL,
    system_prompt           NVARCHAR(MAX)   NULL,
    placeholder             NVARCHAR(512)   NULL,
    quick_prompts_json      NVARCHAR(MAX)   NULL,
    provider_options_json   NVARCHAR(MAX)   NULL,
    sort_order              INT             NOT NULL CONSTRAINT DF_dc_domain_sort_order DEFAULT (0),
    enabled                 BIT             NOT NULL CONSTRAINT DF_dc_domain_enabled DEFAULT (1),
    updated_at              BIGINT          NOT NULL,
    CONSTRAINT PK_dc_domain PRIMARY KEY CLUSTERED (id)
);
GO

CREATE NONCLUSTERED INDEX IX_dc_domain_enabled_sort
    ON dbo.dc_domain (enabled, sort_order);
GO

CREATE TABLE dbo.chat_session (
    id              NVARCHAR(64)    NOT NULL,
    title           NVARCHAR(256)   NOT NULL,
    domain_id       NVARCHAR(64)    NOT NULL,
    chat_mode       NVARCHAR(64)    NOT NULL,
    model           NVARCHAR(128)   NULL,
    resource_id     NVARCHAR(128)   NULL,
    created_at      BIGINT          NOT NULL,
    updated_at      BIGINT          NOT NULL,
    CONSTRAINT PK_chat_session PRIMARY KEY CLUSTERED (id)
);
GO

CREATE NONCLUSTERED INDEX IX_chat_session_updated_at
    ON dbo.chat_session (updated_at DESC);
GO

CREATE TABLE dbo.chat_message (
    id              NVARCHAR(64)    NOT NULL,
    session_id      NVARCHAR(64)    NOT NULL,
    role            NVARCHAR(32)    NOT NULL,
    content         NVARCHAR(MAX)   NOT NULL,
    extras_json     NVARCHAR(MAX)   NULL,
    created_at      BIGINT          NOT NULL,
    CONSTRAINT PK_chat_message PRIMARY KEY CLUSTERED (id),
    CONSTRAINT FK_chat_message_session
        FOREIGN KEY (session_id) REFERENCES dbo.chat_session (id) ON DELETE CASCADE
);
GO

CREATE NONCLUSTERED INDEX IX_chat_message_session_id
    ON dbo.chat_message (session_id);
GO
