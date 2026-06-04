/*
  可选种子数据（与 config/domains.json.example 对齐）
  在 01-schema.sql 之后执行；可重复执行（MERGE）。
*/

SET NOCOUNT ON;
GO

MERGE dbo.dc_global_defaults AS t
USING (
    SELECT
        1 AS id,
        1 AS version,
        N'http://42.193.110.76:5670' AS dbgpt_base_url,
        N'api.coze.cn' AS coze_endpoint,
        120 AS timeout_seconds,
        20 AS max_history_turns,
        0 AS updated_at
) AS s ON t.id = s.id
WHEN MATCHED THEN
    UPDATE SET
        version = s.version,
        dbgpt_base_url = s.dbgpt_base_url,
        coze_endpoint = s.coze_endpoint,
        timeout_seconds = s.timeout_seconds,
        max_history_turns = s.max_history_turns,
        updated_at = s.updated_at
WHEN NOT MATCHED THEN
    INSERT (id, version, dbgpt_base_url, coze_endpoint, timeout_seconds, max_history_turns, updated_at)
    VALUES (s.id, s.version, s.dbgpt_base_url, s.coze_endpoint, s.timeout_seconds, s.max_history_turns, s.updated_at);
GO

MERGE dbo.dc_domain AS t
USING (VALUES
    (N'patent-coze', N'专利助手（Coze）', N'CozeAgent', N'coze', NULL,
     N'你是专利业务管理系统的智能助手。', N'向 Coze 专利 Bot 提问…',
     N'["今年新申请案件授权率","客户回款情况"]',
     N'{"botId":"YOUR_COZE_BOT_ID","apiKeyRef":"coze-main","autoSaveHistory":true,"userIdPrefix":"patent"}',
     0, 1, 0),
    (N'legal', N'法务问答（演示）', N'DbGptApp', N'dbgpt', N'deepseek-ai/DeepSeek-V3',
     N'你是企业法务助手。', NULL, NULL,
     N'{"chatMode":"chat_normal"}',
     1, 1, 0),
    (N'patent', N'专利智能助手（DB-GPT）', N'DbGptApp', N'dbgpt', N'deepseek-ai/DeepSeek-V3',
     N'你是专利业务管理系统的智能助手。', N'@智能体，可对他说今天新登记的官文有多少？',
     N'["今年新申请案件授权率","智能客服","客户回款情况","专利业务知识库","查看今年通过驳回复审，获得授权的案件"]',
     N'{"chatMode":"chat_normal"}',
     2, 1, 0),
    (N'data-query', N'数据问数', N'DbGptData', N'dbgpt', N'deepseek-ai/DeepSeek-V3',
     NULL, NULL,
     N'["Walmart_Sales 表有多少行？","按州统计销售总额前5名","2023年各品类销售额对比"]',
     N'{"chatMode":"chat_data","datasourceId":"Walmart_Sales"}',
     3, 1, 0),
    (N'finance-app', N'财务分析', N'DbGptApp', N'dbgpt', N'deepseek-ai/DeepSeek-V3',
     NULL, NULL, NULL,
     N'{"chatMode":"chat_normal"}',
     4, 1, 0)
) AS s (
    id, display_name, chat_mode, provider, model, system_prompt, placeholder,
    quick_prompts_json, provider_options_json, sort_order, enabled, updated_at
) ON t.id = s.id
WHEN MATCHED THEN
    UPDATE SET
        display_name = s.display_name,
        chat_mode = s.chat_mode,
        provider = s.provider,
        model = s.model,
        system_prompt = s.system_prompt,
        placeholder = s.placeholder,
        quick_prompts_json = s.quick_prompts_json,
        provider_options_json = s.provider_options_json,
        sort_order = s.sort_order,
        enabled = s.enabled,
        updated_at = s.updated_at
WHEN NOT MATCHED THEN
    INSERT (
        id, display_name, chat_mode, provider, model, system_prompt, placeholder,
        quick_prompts_json, provider_options_json, sort_order, enabled, updated_at
    )
    VALUES (
        s.id, s.display_name, s.chat_mode, s.provider, s.model, s.system_prompt, s.placeholder,
        s.quick_prompts_json, s.provider_options_json, s.sort_order, s.enabled, s.updated_at
    );
GO
