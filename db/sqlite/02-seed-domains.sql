/*
  可选种子数据（与 config/domains.json.example 对齐）
  在 01-schema.sql 之后执行。
*/

INSERT OR REPLACE INTO dc_global_defaults (
    id, version, dbgpt_base_url, coze_endpoint, timeout_seconds, max_history_turns, updated_at
) VALUES (
    1, 1, 'http://42.193.110.76:5670', 'api.coze.cn', 120, 20, 0
);

INSERT OR REPLACE INTO dc_domain (
    id, display_name, chat_mode, provider, model, system_prompt, placeholder,
    quick_prompts_json, provider_options_json, sort_order, enabled, updated_at
) VALUES
(
    'patent-coze', '专利助手（Coze）', 'CozeAgent', 'coze', NULL,
    '你是专利业务管理系统的智能助手。', '向 Coze 专利 Bot 提问…',
    '["今年新申请案件授权率","客户回款情况"]',
    '{"botId":"YOUR_COZE_BOT_ID","apiKeyRef":"coze-main","autoSaveHistory":true,"userIdPrefix":"patent"}',
    0, 1, 0
),
(
    'legal', '法务问答（演示）', 'DbGptApp', 'dbgpt', 'deepseek-ai/DeepSeek-V3',
    '你是企业法务助手。', NULL, NULL,
    '{"chatMode":"chat_normal"}',
    1, 1, 0
),
(
    'patent', '专利智能助手（DB-GPT）', 'DbGptApp', 'dbgpt', 'deepseek-ai/DeepSeek-V3',
    '你是专利业务管理系统的智能助手。', '@智能体，可对他说今天新登记的官文有多少？',
    '["今年新申请案件授权率","智能客服","客户回款情况","专利业务知识库","查看今年通过驳回复审，获得授权的案件"]',
    '{"chatMode":"chat_normal"}',
    2, 1, 0
),
(
    'data-query', '数据问数', 'DbGptData', 'dbgpt', 'deepseek-ai/DeepSeek-V3',
    NULL, NULL,
    '["Walmart_Sales 表有多少行？","按州统计销售总额前5名","2023年各品类销售额对比"]',
    '{"chatMode":"chat_data","datasourceId":"Walmart_Sales"}',
    3, 1, 0
),
(
    'finance-app', '财务分析', 'DbGptApp', 'dbgpt', 'deepseek-ai/DeepSeek-V3',
    NULL, NULL, NULL,
    '{"chatMode":"chat_normal"}',
    4, 1, 0
);
