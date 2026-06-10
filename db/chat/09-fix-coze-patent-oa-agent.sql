/* Idempotent: Coze patent OA agent + PatentOA workflow (API-verified IDs) */
DELETE FROM dbo.dc_agent WHERE id = N'test';
GO

MERGE dbo.dc_agent AS t
USING (
    SELECT
        N'patent-oa' AS id,
        N'专利答OA智能体' AS display_name,
        N'coze' AS provider,
        N'CozeAgent' AS chat_mode,
        CAST(814352859922501 AS BIGINT) AS provider_account_id,
        N'{"botId":"7649024378344816674","workspaceId":"7545313173269299219","autoSaveHistory":true,"userIdPrefix":"user","listPublishStatus":"published_online"}' AS config_json,
        N'请输入专利 OA 相关问题' AS placeholder,
        N'["请帮我分析这份OA的核心驳回点","如何生成符合要求的答复意见"]' AS quick_prompts_json,
        N'扣子个人空间 · 专利答OA' AS remark
) AS s ON t.id = s.id
WHEN MATCHED THEN
    UPDATE SET
        display_name = s.display_name,
        provider_account_id = s.provider_account_id,
        config_json = s.config_json,
        placeholder = s.placeholder,
        quick_prompts_json = s.quick_prompts_json,
        remark = s.remark,
        enabled = 1,
        update_time = SYSDATETIME()
WHEN NOT MATCHED THEN
    INSERT (id, display_name, provider, chat_mode, provider_account_id, config_json,
        placeholder, quick_prompts_json, sort_order, enabled, remark, create_time)
    VALUES (s.id, s.display_name, s.provider, N'CozeAgent', s.provider_account_id, s.config_json,
        s.placeholder, s.quick_prompts_json, 0, 1, s.remark, SYSDATETIME());
GO

MERGE dbo.dc_agent_resource AS t
USING (
    SELECT
        CAST(814352859922520 AS BIGINT) AS id,
        N'patent-oa' AS agent_id,
        N'workflow' AS resource_type,
        N'7649037302673063970' AS external_id,
        N'PatentOA' AS display_name,
        N'专利答OA' AS description,
        N'{"inputParameter":"BOT_USER_INPUT","inputHint":""}' AS config_json
) AS s ON t.agent_id = s.agent_id AND t.resource_type = s.resource_type AND t.external_id = s.external_id
WHEN MATCHED THEN
    UPDATE SET display_name = s.display_name, description = s.description, enabled = 1, update_time = SYSDATETIME()
WHEN NOT MATCHED THEN
    INSERT (id, agent_id, resource_type, external_id, display_name, description, config_json, sort_order, enabled, create_time)
    VALUES (s.id, s.agent_id, s.resource_type, s.external_id, s.display_name, s.description, s.config_json, 0, 1, SYSDATETIME());
GO

MERGE dbo.dc_role_agent AS t
USING (SELECT CAST(814320686415941 AS BIGINT) AS role_id, N'patent-oa' AS agent_id) AS s
ON t.role_id = s.role_id AND t.agent_id = s.agent_id
WHEN NOT MATCHED THEN INSERT (role_id, agent_id, create_time) VALUES (s.role_id, s.agent_id, SYSDATETIME());
GO

MERGE dbo.dc_role_resource AS t
USING (SELECT CAST(814320686415941 AS BIGINT) AS role_id, CAST(814352859922520 AS BIGINT) AS resource_row_id) AS s
ON t.role_id = s.role_id AND t.resource_row_id = s.resource_row_id
WHEN NOT MATCHED THEN INSERT (role_id, resource_row_id, create_time) VALUES (s.role_id, s.resource_row_id, SYSDATETIME());
GO
