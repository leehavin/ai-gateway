/*
  示例种子数据 — 仅保留一条 Coze 领域作为配置参考。
  在 01-schema.sql 之后执行；可重复执行（MERGE 幂等）。
  实际领域请根据业务需求修改 id、display_name 及 provider_options_json。
*/

MERGE dbo.dc_global_defaults AS t
USING (
    SELECT
        1              AS id,
        1              AS version,
        N''            AS dbgpt_base_url,
        N'api.coze.cn' AS coze_endpoint,
        120            AS timeout_seconds,
        20             AS max_history_turns,
        0              AS updated_at
) AS s ON t.id = s.id
WHEN MATCHED THEN
    UPDATE SET
        version           = s.version,
        dbgpt_base_url    = s.dbgpt_base_url,
        coze_endpoint     = s.coze_endpoint,
        timeout_seconds   = s.timeout_seconds,
        max_history_turns = s.max_history_turns,
        updated_at        = s.updated_at
WHEN NOT MATCHED THEN
    INSERT (id, version, dbgpt_base_url, coze_endpoint, timeout_seconds, max_history_turns, updated_at)
    VALUES (s.id, s.version, s.dbgpt_base_url, s.coze_endpoint, s.timeout_seconds, s.max_history_turns, s.updated_at);

MERGE dbo.dc_domain AS t
USING (VALUES
    (
        N'my-assistant',
        N'智能助手',
        N'CozeAgent',
        N'coze',
        NULL,
        NULL,
        N'请输入问题…',
        NULL,
        N'{"botId":"YOUR_COZE_BOT_ID","apiKeyRef":"coze-main","autoSaveHistory":true,"userIdPrefix":"user"}',
        0, 1, 0
    )
) AS s (
    id, display_name, chat_mode, provider, model, system_prompt, placeholder,
    quick_prompts_json, provider_options_json, sort_order, enabled, updated_at
) ON t.id = s.id
WHEN MATCHED THEN
    UPDATE SET
        display_name          = s.display_name,
        chat_mode             = s.chat_mode,
        provider              = s.provider,
        model                 = s.model,
        system_prompt         = s.system_prompt,
        placeholder           = s.placeholder,
        quick_prompts_json    = s.quick_prompts_json,
        provider_options_json = s.provider_options_json,
        sort_order            = s.sort_order,
        enabled               = s.enabled,
        updated_at            = s.updated_at
WHEN NOT MATCHED THEN
    INSERT (
        id, display_name, chat_mode, provider, model, system_prompt, placeholder,
        quick_prompts_json, provider_options_json, sort_order, enabled, updated_at
    )
    VALUES (
        s.id, s.display_name, s.chat_mode, s.provider, s.model, s.system_prompt, s.placeholder,
        s.quick_prompts_json, s.provider_options_json, s.sort_order, s.enabled, s.updated_at
    );
