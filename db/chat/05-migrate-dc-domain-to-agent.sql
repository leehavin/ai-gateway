/*
  将 dc_domain 迁移到通用表 dc_agent / dc_agent_resource。
  前置: 04-agent-management.sql 已执行。

  - 所有 provider（coze/dbgpt/custom/openai）→ dc_agent
  - coze 且 provider_options_json.workflows[] → dc_agent_resource (resource_type=workflow)
  - 不删除 dc_domain（Gateway 过渡期可读旧表）
  - 可重复执行（MERGE 幂等）

  执行: sqlcmd -S host -d chat -i db/chat/05-migrate-dc-domain-to-agent.sql
*/

SET ANSI_NULLS ON;
SET QUOTED_IDENTIFIER ON;
GO

DECLARE @coze_account_id BIGINT = 1001;
DECLARE @dbgpt_account_id BIGINT = 1002;

IF NOT EXISTS (SELECT 1 FROM dbo.dc_provider_account WHERE id = @coze_account_id)
BEGIN
    RAISERROR(N'请先执行 04-agent-management.sql。', 16, 1);
    RETURN;
END
GO

/* ---------- 按 provider 匹配默认连接器 ---------- */
MERGE dbo.dc_agent AS t
USING (
    SELECT
        d.id                                            AS id,
        d.display_name                                  AS display_name,
        LOWER(d.provider)                               AS provider,
        d.chat_mode                                     AS chat_mode,
        CASE LOWER(d.provider)
            WHEN N'coze' THEN CAST(1001 AS BIGINT)
            WHEN N'dbgpt' THEN CAST(1002 AS BIGINT)
            ELSE NULL
        END                                             AS provider_account_id,
        d.model                                         AS model,
        CASE
            WHEN ISJSON(d.provider_options_json) = 1 AND LOWER(d.provider) <> N'custom'
                THEN d.provider_options_json
            WHEN LOWER(d.provider) = N'custom' AND ISJSON(d.provider_options_json) = 1
                THEN d.provider_options_json
            ELSE N'{}'
        END                                             AS config_json,
        d.placeholder,
        d.quick_prompts_json,
        d.system_prompt,
        d.sort_order,
        d.enabled
    FROM dbo.dc_domain d
    WHERE d.enabled = 1
      AND LOWER(d.provider) IN (N'coze', N'dbgpt', N'custom', N'openai')
) AS s ON t.id = s.id
WHEN MATCHED THEN
    UPDATE SET
        display_name = s.display_name,
        provider = s.provider,
        chat_mode = s.chat_mode,
        provider_account_id = COALESCE(s.provider_account_id, t.provider_account_id),
        model = s.model,
        config_json = s.config_json,
        placeholder = s.placeholder,
        quick_prompts_json = s.quick_prompts_json,
        system_prompt = s.system_prompt,
        sort_order = s.sort_order,
        enabled = s.enabled,
        update_time = SYSDATETIME()
WHEN NOT MATCHED THEN
    INSERT (
        id, display_name, provider, chat_mode, provider_account_id, model, config_json,
        placeholder, quick_prompts_json, system_prompt, sort_order, enabled, create_time
    )
    VALUES (
        s.id, s.display_name, s.provider, s.chat_mode, s.provider_account_id, s.model, s.config_json,
        s.placeholder, s.quick_prompts_json, s.system_prompt, s.sort_order, s.enabled, SYSDATETIME()
    );
GO

/* ---------- Coze workflows → dc_agent_resource ---------- */
;WITH wf AS (
    SELECT
        d.id AS agent_id,
        w.workflow_id,
        w.display_name,
        w.description,
        w.input_parameter,
        w.input_hint,
        w.inputs_json,
        w.default_parameters_json,
        w.app_id,
        ROW_NUMBER() OVER (PARTITION BY d.id ORDER BY w.workflow_id) AS sort_order
    FROM dbo.dc_domain d
    CROSS APPLY OPENJSON(d.provider_options_json, '$.workflows')
        WITH (
            workflow_id             NVARCHAR(64)    '$.workflowId',
            display_name            NVARCHAR(256)   '$.displayName',
            description             NVARCHAR(2000)  '$.description',
            input_parameter         NVARCHAR(64)    '$.inputParameter',
            input_hint              NVARCHAR(512)   '$.inputHint',
            inputs_json             NVARCHAR(MAX)   '$.inputs' AS JSON,
            default_parameters_json NVARCHAR(MAX)   '$.defaultParameters' AS JSON,
            app_id                  NVARCHAR(64)    '$.appId'
        ) w
    WHERE LOWER(d.provider) = N'coze'
      AND d.enabled = 1
      AND ISJSON(d.provider_options_json) = 1
      AND w.workflow_id IS NOT NULL
),
numbered AS (
    SELECT
        CAST(2000000000 + ROW_NUMBER() OVER (ORDER BY agent_id, workflow_id) AS BIGINT) AS id,
        agent_id,
        N'workflow' AS resource_type,
        workflow_id AS external_id,
        display_name,
        description,
        (
            SELECT
                COALESCE(NULLIF(input_parameter, N''), N'BOT_USER_INPUT') AS inputParameter,
                input_hint AS inputHint,
                JSON_QUERY(inputs_json) AS inputs,
                JSON_QUERY(default_parameters_json) AS defaultParameters,
                app_id AS appId
            FOR JSON PATH, WITHOUT_ARRAY_WRAPPER
        ) AS config_json,
        sort_order,
        CAST(1 AS BIT) AS enabled
    FROM wf
)
MERGE dbo.dc_agent_resource AS t
USING numbered AS s
ON t.agent_id = s.agent_id AND t.resource_type = s.resource_type AND t.external_id = s.external_id
WHEN MATCHED THEN
    UPDATE SET
        display_name = s.display_name,
        description = s.description,
        config_json = s.config_json,
        sort_order = s.sort_order,
        enabled = s.enabled,
        update_time = SYSDATETIME()
WHEN NOT MATCHED THEN
    INSERT (id, agent_id, resource_type, external_id, display_name, description, config_json, sort_order, enabled, create_time)
    VALUES (s.id, s.agent_id, s.resource_type, s.external_id, s.display_name, s.description, s.config_json, s.sort_order, s.enabled, SYSDATETIME());
GO

PRINT N'dc_domain 已迁移至 dc_agent / dc_agent_resource（未删除 dc_domain）。';
GO
