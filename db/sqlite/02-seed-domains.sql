/*
  示例种子数据 — 仅保留一条 Coze 领域作为配置参考。
  在 01-schema.sql 之后执行。
  实际领域请根据业务需求修改 id、display_name 及 provider_options_json。
*/

INSERT OR REPLACE INTO dc_global_defaults (
    id, version, dbgpt_base_url, coze_endpoint, timeout_seconds, max_history_turns, updated_at
) VALUES (
    1, 1, '', 'api.coze.cn', 120, 20, 0
);

INSERT OR REPLACE INTO dc_domain (
    id, display_name, chat_mode, provider, model, system_prompt, placeholder,
    quick_prompts_json, provider_options_json, sort_order, enabled, updated_at
) VALUES (
    'my-assistant', '智能助手', 'CozeAgent', 'coze', NULL,
    NULL, '请输入问题…',
    NULL,
    '{"botId":"YOUR_COZE_BOT_ID","apiKeyRef":"coze-main","autoSaveHistory":true,"userIdPrefix":"user"}',
    0, 1, 0
);
