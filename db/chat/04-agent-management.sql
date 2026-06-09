/*
  智能体（Agent）通用管理 + 子资源权限
  支持多 Provider：coze / dbgpt / custom / openai（可扩展）
  表前缀: dc_；关联 sys_role（需已执行 db/admin/admin-sqlserver.sql）

  执行: sqlcmd -S host -d chat -i db/chat/04-agent-management.sql
  可重复执行。若曾执行旧版 04-coze-management.sql，会先删除旧表名。
*/

SET ANSI_NULLS ON;
SET QUOTED_IDENTIFIER ON;
GO

/* ---------------------------- 删除旧版 Coze 专用表名（兼容升级） ---------------------------- */
IF OBJECT_ID(N'dbo.dc_role_workflow', N'U') IS NOT NULL DROP TABLE dbo.dc_role_workflow;
GO
IF OBJECT_ID(N'dbo.dc_coze_workflow', N'U') IS NOT NULL DROP TABLE dbo.dc_coze_workflow;
GO
IF OBJECT_ID(N'dbo.dc_coze_account', N'U') IS NOT NULL DROP TABLE dbo.dc_coze_account;
GO

/* ---------------------------- 按依赖逆序删除（当前模型） ---------------------------- */
IF OBJECT_ID(N'dbo.dc_role_resource', N'U') IS NOT NULL DROP TABLE dbo.dc_role_resource;
GO
IF OBJECT_ID(N'dbo.dc_role_agent', N'U') IS NOT NULL DROP TABLE dbo.dc_role_agent;
GO
IF OBJECT_ID(N'dbo.dc_user_agent', N'U') IS NOT NULL DROP TABLE dbo.dc_user_agent;
GO
IF OBJECT_ID(N'dbo.dc_agent_resource', N'U') IS NOT NULL DROP TABLE dbo.dc_agent_resource;
GO
IF OBJECT_ID(N'dbo.dc_agent', N'U') IS NOT NULL DROP TABLE dbo.dc_agent;
GO
IF OBJECT_ID(N'dbo.dc_provider_account', N'U') IS NOT NULL DROP TABLE dbo.dc_provider_account;
GO

/* ---------------------------- dc_provider_account 连接器账号（按 Provider） ---------------------------- */
CREATE TABLE dbo.dc_provider_account (
    id                  BIGINT          NOT NULL,
    provider            NVARCHAR(32)    NOT NULL,
    name                NVARCHAR(128)   NOT NULL,
    endpoint            NVARCHAR(512)   NULL,
    api_key_ciphertext  NVARCHAR(MAX)   NULL,
    config_json         NVARCHAR(MAX)   NULL,
    sort_order          INT             NOT NULL CONSTRAINT DF_dc_pa_sort DEFAULT (0),
    enabled             BIT             NOT NULL CONSTRAINT DF_dc_pa_enabled DEFAULT (1),
    remark              NVARCHAR(1000)  NULL,
    create_by           BIGINT          NULL,
    create_time         DATETIME2(3)    NOT NULL CONSTRAINT DF_dc_pa_ctime DEFAULT (SYSDATETIME()),
    update_by           BIGINT          NULL,
    update_time         DATETIME2(3)    NULL,
    CONSTRAINT PK_dc_provider_account PRIMARY KEY CLUSTERED (id),
    CONSTRAINT CK_dc_pa_provider CHECK (
        provider IN (N'coze', N'dbgpt', N'openai')
    )
);
GO

CREATE NONCLUSTERED INDEX IX_dc_provider_account_provider
    ON dbo.dc_provider_account (provider, enabled, sort_order);
GO

/* ----------------------------
   dc_agent 智能体（Gateway domain / chat-ui @ 列表项）
   provider + config_json 承载各平台差异配置，避免为每种 Agent 加列
   config_json 示例:
     coze:   {"botId","workspaceId","autoSaveHistory","userIdPrefix","listPublishStatus"}
     dbgpt:  {"chatMode","appId","datasourceId","knowledgeSpaceName"}
     custom: {"endpoint","adapter","authHeaderName"}
     openai: {"baseUrl","model"}
   ---------------------------- */
CREATE TABLE dbo.dc_agent (
    id                      NVARCHAR(64)    NOT NULL,
    display_name            NVARCHAR(128)   NOT NULL,
    provider                NVARCHAR(32)    NOT NULL,
    chat_mode               NVARCHAR(64)    NOT NULL,
    provider_account_id     BIGINT          NULL,
    model                   NVARCHAR(128)   NULL,
    config_json             NVARCHAR(MAX)   NOT NULL CONSTRAINT DF_dc_agent_config DEFAULT (N'{}'),
    placeholder             NVARCHAR(512)   NULL,
    quick_prompts_json      NVARCHAR(MAX)   NULL,
    system_prompt           NVARCHAR(MAX)   NULL,
    sort_order              INT             NOT NULL CONSTRAINT DF_dc_agent_sort DEFAULT (0),
    enabled                 BIT             NOT NULL CONSTRAINT DF_dc_agent_enabled DEFAULT (1),
    remark                  NVARCHAR(1000)  NULL,
    create_by               BIGINT          NULL,
    create_time             DATETIME2(3)    NOT NULL CONSTRAINT DF_dc_agent_ctime DEFAULT (SYSDATETIME()),
    update_by               BIGINT          NULL,
    update_time             DATETIME2(3)    NULL,
    CONSTRAINT PK_dc_agent PRIMARY KEY CLUSTERED (id),
    CONSTRAINT CK_dc_agent_provider CHECK (
        provider IN (N'coze', N'dbgpt', N'openai')
    ),
    CONSTRAINT FK_dc_agent_account FOREIGN KEY (provider_account_id)
        REFERENCES dbo.dc_provider_account (id)
);
GO

CREATE NONCLUSTERED INDEX IX_dc_agent_provider
    ON dbo.dc_agent (provider, enabled, sort_order);
GO

CREATE NONCLUSTERED INDEX IX_dc_agent_account
    ON dbo.dc_agent (provider_account_id);
GO

/* ----------------------------
   dc_agent_resource 智能体子资源（工作流 / 工具 / 技能等）
   resource_type:
     workflow  — Coze 工作流（/coze 命令）
     tool      — 预留：MCP / 插件
     skill     — 预留：复合能力
   config_json 示例 (workflow):
     {"inputParameter","inputHint","inputs","defaultParameters","appId"}
   ---------------------------- */
CREATE TABLE dbo.dc_agent_resource (
    id                  BIGINT          NOT NULL,
    agent_id            NVARCHAR(64)    NOT NULL,
    resource_type       NVARCHAR(32)    NOT NULL CONSTRAINT DF_dc_ar_type DEFAULT (N'workflow'),
    external_id         NVARCHAR(128)   NOT NULL,
    display_name        NVARCHAR(256)   NULL,
    description         NVARCHAR(2000)  NULL,
    config_json         NVARCHAR(MAX)   NULL,
    sort_order          INT             NOT NULL CONSTRAINT DF_dc_ar_sort DEFAULT (0),
    enabled             BIT             NOT NULL CONSTRAINT DF_dc_ar_enabled DEFAULT (1),
    remark              NVARCHAR(1000)  NULL,
    create_by           BIGINT          NULL,
    create_time         DATETIME2(3)    NOT NULL CONSTRAINT DF_dc_ar_ctime DEFAULT (SYSDATETIME()),
    update_by           BIGINT          NULL,
    update_time         DATETIME2(3)    NULL,
    CONSTRAINT PK_dc_agent_resource PRIMARY KEY CLUSTERED (id),
    CONSTRAINT CK_dc_ar_type CHECK (
        resource_type IN (N'workflow', N'tool', N'skill')
    ),
    CONSTRAINT FK_dc_ar_agent FOREIGN KEY (agent_id)
        REFERENCES dbo.dc_agent (id) ON DELETE CASCADE,
    CONSTRAINT UK_dc_ar_agent_type_ext UNIQUE (agent_id, resource_type, external_id)
);
GO

CREATE NONCLUSTERED INDEX IX_dc_agent_resource_agent
    ON dbo.dc_agent_resource (agent_id, resource_type, enabled, sort_order);
GO

/* ---------------------------- dc_role_agent 角色-智能体授权 ---------------------------- */
CREATE TABLE dbo.dc_role_agent (
    role_id     BIGINT          NOT NULL,
    agent_id    NVARCHAR(64)    NOT NULL,
    create_by   BIGINT          NULL,
    create_time DATETIME2(3)    NOT NULL CONSTRAINT DF_dc_ra_ctime DEFAULT (SYSDATETIME()),
    CONSTRAINT PK_dc_role_agent PRIMARY KEY CLUSTERED (role_id, agent_id),
    CONSTRAINT FK_dc_ra_role FOREIGN KEY (role_id)
        REFERENCES dbo.sys_role (id) ON DELETE CASCADE,
    CONSTRAINT FK_dc_ra_agent FOREIGN KEY (agent_id)
        REFERENCES dbo.dc_agent (id) ON DELETE CASCADE
);
GO

CREATE NONCLUSTERED INDEX IX_dc_role_agent_agent
    ON dbo.dc_role_agent (agent_id);
GO

/* ---------------------------- dc_role_resource 角色-子资源授权（细粒度，可选） ---------------------------- */
CREATE TABLE dbo.dc_role_resource (
    role_id         BIGINT          NOT NULL,
    resource_row_id BIGINT          NOT NULL,
    create_by       BIGINT          NULL,
    create_time     DATETIME2(3)    NOT NULL CONSTRAINT DF_dc_rr_ctime DEFAULT (SYSDATETIME()),
    CONSTRAINT PK_dc_role_resource PRIMARY KEY CLUSTERED (role_id, resource_row_id),
    CONSTRAINT FK_dc_rr_role FOREIGN KEY (role_id)
        REFERENCES dbo.sys_role (id) ON DELETE CASCADE,
    CONSTRAINT FK_dc_rr_resource FOREIGN KEY (resource_row_id)
        REFERENCES dbo.dc_agent_resource (id) ON DELETE CASCADE
);
GO

CREATE NONCLUSTERED INDEX IX_dc_role_resource_row
    ON dbo.dc_role_resource (resource_row_id);
GO

/* ---------------------------- dc_user_agent 用户直授（可选） ---------------------------- */
CREATE TABLE dbo.dc_user_agent (
    user_id     BIGINT          NOT NULL,
    agent_id    NVARCHAR(64)    NOT NULL,
    create_by   BIGINT          NULL,
    create_time DATETIME2(3)    NOT NULL CONSTRAINT DF_dc_ua_ctime DEFAULT (SYSDATETIME()),
    CONSTRAINT PK_dc_user_agent PRIMARY KEY CLUSTERED (user_id, agent_id),
    CONSTRAINT FK_dc_ua_user FOREIGN KEY (user_id)
        REFERENCES dbo.sys_user (id) ON DELETE CASCADE,
    CONSTRAINT FK_dc_ua_agent FOREIGN KEY (agent_id)
        REFERENCES dbo.dc_agent (id) ON DELETE CASCADE
);
GO

/* ---------------------------- 示例：Coze 连接器 + 智能体 + 工作流 ---------------------------- */
MERGE dbo.dc_provider_account AS t
USING (
    SELECT
        CAST(1001 AS BIGINT)     AS id,
        N'coze'                  AS provider,
        N'默认扣子空间'           AS name,
        N'api.coze.cn'           AS endpoint,
        NULL                     AS api_key_ciphertext,
        NULL                     AS config_json,
        0                        AS sort_order,
        CAST(1 AS BIT)           AS enabled,
        N'在管理后台填写 Coze API Key' AS remark
) AS s ON t.id = s.id
WHEN MATCHED THEN
    UPDATE SET provider = s.provider, name = s.name, endpoint = s.endpoint,
        enabled = s.enabled, remark = s.remark, update_time = SYSDATETIME()
WHEN NOT MATCHED THEN
    INSERT (id, provider, name, endpoint, api_key_ciphertext, config_json, sort_order, enabled, remark, create_time)
    VALUES (s.id, s.provider, s.name, s.endpoint, s.api_key_ciphertext, s.config_json, s.sort_order, s.enabled, s.remark, SYSDATETIME());
GO

MERGE dbo.dc_agent AS t
USING (
    SELECT
        N'my-assistant' AS id,
        N'智能助手' AS display_name,
        N'coze' AS provider,
        N'CozeAgent' AS chat_mode,
        CAST(1001 AS BIGINT) AS provider_account_id,
        NULL AS model,
        N'{"botId":"YOUR_COZE_BOT_ID","workspaceId":"YOUR_COZE_WORKSPACE_ID","autoSaveHistory":true,"userIdPrefix":"user","listPublishStatus":"published_online"}' AS config_json,
        N'请输入问题…' AS placeholder,
        NULL AS quick_prompts_json,
        NULL AS system_prompt,
        0 AS sort_order,
        CAST(1 AS BIT) AS enabled
) AS s ON t.id = s.id
WHEN MATCHED THEN
    UPDATE SET display_name = s.display_name, provider = s.provider, chat_mode = s.chat_mode,
        provider_account_id = s.provider_account_id, config_json = s.config_json,
        placeholder = s.placeholder, sort_order = s.sort_order, enabled = s.enabled, update_time = SYSDATETIME()
WHEN NOT MATCHED THEN
    INSERT (id, display_name, provider, chat_mode, provider_account_id, model, config_json,
        placeholder, quick_prompts_json, system_prompt, sort_order, enabled, create_time)
    VALUES (s.id, s.display_name, s.provider, s.chat_mode, s.provider_account_id, s.model, s.config_json,
        s.placeholder, s.quick_prompts_json, s.system_prompt, s.sort_order, s.enabled, SYSDATETIME());
GO

MERGE dbo.dc_agent_resource AS t
USING (
    SELECT
        CAST(2001 AS BIGINT) AS id,
        N'my-assistant' AS agent_id,
        N'workflow' AS resource_type,
        N'YOUR_COZE_WORKFLOW_ID' AS external_id,
        N'示例工作流' AS display_name,
        N'替换为真实 Coze workflow_id' AS description,
        N'{"inputParameter":"BOT_USER_INPUT","inputHint":"输入后发送执行"}' AS config_json,
        0 AS sort_order,
        CAST(1 AS BIT) AS enabled
) AS s ON t.id = s.id
WHEN MATCHED THEN
    UPDATE SET agent_id = s.agent_id, resource_type = s.resource_type, external_id = s.external_id,
        display_name = s.display_name, description = s.description, config_json = s.config_json,
        sort_order = s.sort_order, enabled = s.enabled, update_time = SYSDATETIME()
WHEN NOT MATCHED THEN
    INSERT (id, agent_id, resource_type, external_id, display_name, description, config_json, sort_order, enabled, create_time)
    VALUES (s.id, s.agent_id, s.resource_type, s.external_id, s.display_name, s.description, s.config_json, s.sort_order, s.enabled, SYSDATETIME());
GO

/*
  示例：DB-GPT 问数智能体（使用全局 dbgpt 地址时可不绑 provider_account）
*/
MERGE dbo.dc_provider_account AS t
USING (
    SELECT CAST(1002 AS BIGINT) AS id, N'dbgpt' AS provider, N'公司 DB-GPT' AS name,
        N'http://42.193.110.76:5670' AS endpoint, NULL AS api_key_ciphertext,
        NULL AS config_json, 1 AS sort_order,
        CAST(1 AS BIT) AS enabled, N'对接 Text2SQL' AS remark
) AS s ON t.id = s.id
WHEN NOT MATCHED THEN
    INSERT (id, provider, name, endpoint, api_key_ciphertext, config_json, sort_order, enabled, remark, create_time)
    VALUES (s.id, s.provider, s.name, s.endpoint, s.api_key_ciphertext, s.config_json, s.sort_order, s.enabled, s.remark, SYSDATETIME());
GO

MERGE dbo.dc_agent AS t
USING (
    SELECT N'data-query' AS id, N'数据问数' AS display_name, N'dbgpt' AS provider,
        N'DbGptData' AS chat_mode, CAST(1002 AS BIGINT) AS provider_account_id,
        N'deepseek-ai/DeepSeek-V3' AS model,
        N'{"chatMode":"chat_data","datasourceId":"Walmart_Sales"}' AS config_json,
        N'用自然语言查询数据…' AS placeholder, NULL AS quick_prompts_json, NULL AS system_prompt,
        10 AS sort_order, CAST(0 AS BIT) AS enabled
) AS s ON t.id = s.id
WHEN NOT MATCHED THEN
    INSERT (id, display_name, provider, chat_mode, provider_account_id, model, config_json,
        placeholder, quick_prompts_json, system_prompt, sort_order, enabled, create_time)
    VALUES (s.id, s.display_name, s.provider, s.chat_mode, s.provider_account_id, s.model, s.config_json,
        s.placeholder, s.quick_prompts_json, s.system_prompt, s.sort_order, s.enabled, SYSDATETIME());
GO

/* SuperAdmin（role_id=1）授权示例 Coze 智能体 + 工作流 */
IF EXISTS (SELECT 1 FROM dbo.sys_role WHERE id = 1)
BEGIN
    MERGE dbo.dc_role_agent AS t
    USING (SELECT CAST(1 AS BIGINT) AS role_id, N'my-assistant' AS agent_id) AS s
    ON t.role_id = s.role_id AND t.agent_id = s.agent_id
    WHEN NOT MATCHED THEN INSERT (role_id, agent_id, create_time) VALUES (s.role_id, s.agent_id, SYSDATETIME());

    MERGE dbo.dc_role_resource AS t
    USING (SELECT CAST(1 AS BIGINT) AS role_id, CAST(2001 AS BIGINT) AS resource_row_id) AS s
    ON t.role_id = s.role_id AND t.resource_row_id = s.resource_row_id
    WHEN NOT MATCHED THEN INSERT (role_id, resource_row_id, create_time) VALUES (s.role_id, s.resource_row_id, SYSDATETIME());
END
GO

PRINT N'dc_provider_account / dc_agent / dc_agent_resource / dc_role_* 已就绪（通用多 Provider）。';
GO
