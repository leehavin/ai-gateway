/* PatentOA 工作流：声明必填 doc 附件，供 Gateway 映射宿主带入的五书文档 */
UPDATE dbo.dc_agent_resource
SET config_json = N'{"inputParameter":"BOT_USER_INPUT","inputHint":"上传五书文档后发送","inputs":[{"name":"doc","type":"file","required":true,"label":"五书文档","accept":[".doc",".docx",".pdf"]}]}',
    update_time = SYSDATETIME()
WHERE agent_id = N'patent-oa'
  AND resource_type = N'workflow'
  AND external_id = N'7649037302673063970';
GO
