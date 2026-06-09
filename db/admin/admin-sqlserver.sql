/*
  AIAdmin — SQL Server 表结构
  表前缀: sys_/wf_；字段: 小写 snake_case；由 admin-mysql.sql 转换

  执行前请先创建数据库，例如:
    CREATE DATABASE AIAdmin;
  执行:
    sqlcmd -S host -d AIAdmin -i admin-sqlserver.sql
*/

SET ANSI_NULLS ON;
SET QUOTED_IDENTIFIER ON;
GO

/* 按依赖逆序删除 */
IF OBJECT_ID(N'dbo.wf_instance', N'U') IS NOT NULL DROP TABLE dbo.wf_instance;
GO
IF OBJECT_ID(N'dbo.wf_execution_attribute', N'U') IS NOT NULL DROP TABLE dbo.wf_execution_attribute;
GO
IF OBJECT_ID(N'dbo.wf_execution_pointer', N'U') IS NOT NULL DROP TABLE dbo.wf_execution_pointer;
GO
IF OBJECT_ID(N'dbo.wf_workflow', N'U') IS NOT NULL DROP TABLE dbo.wf_workflow;
GO
IF OBJECT_ID(N'dbo.wf_waiting_pointer', N'U') IS NOT NULL DROP TABLE dbo.wf_waiting_pointer;
GO
IF OBJECT_ID(N'dbo.wf_subscription', N'U') IS NOT NULL DROP TABLE dbo.wf_subscription;
GO
IF OBJECT_ID(N'dbo.wf_scheduled_command', N'U') IS NOT NULL DROP TABLE dbo.wf_scheduled_command;
GO
IF OBJECT_ID(N'dbo.wf_execution_error', N'U') IS NOT NULL DROP TABLE dbo.wf_execution_error;
GO
IF OBJECT_ID(N'dbo.wf_event', N'U') IS NOT NULL DROP TABLE dbo.wf_event;
GO
IF OBJECT_ID(N'dbo.wf_auditing_record', N'U') IS NOT NULL DROP TABLE dbo.wf_auditing_record;
GO
IF OBJECT_ID(N'dbo.wf_definition', N'U') IS NOT NULL DROP TABLE dbo.wf_definition;
GO
IF OBJECT_ID(N'dbo.sys_oauth2_user', N'U') IS NOT NULL DROP TABLE dbo.sys_oauth2_user;
GO
IF OBJECT_ID(N'dbo.sys_user_role', N'U') IS NOT NULL DROP TABLE dbo.sys_user_role;
GO
IF OBJECT_ID(N'dbo.sys_user', N'U') IS NOT NULL DROP TABLE dbo.sys_user;
GO
IF OBJECT_ID(N'dbo.sys_role_function', N'U') IS NOT NULL DROP TABLE dbo.sys_role_function;
GO
IF OBJECT_ID(N'dbo.sys_function_interface', N'U') IS NOT NULL DROP TABLE dbo.sys_function_interface;
GO
IF OBJECT_ID(N'dbo.sys_interface', N'U') IS NOT NULL DROP TABLE dbo.sys_interface;
GO
IF OBJECT_ID(N'dbo.sys_notice_record', N'U') IS NOT NULL DROP TABLE dbo.sys_notice_record;
GO
IF OBJECT_ID(N'dbo.sys_profile_system', N'U') IS NOT NULL DROP TABLE dbo.sys_profile_system;
GO
IF OBJECT_ID(N'dbo.sys_dict_data', N'U') IS NOT NULL DROP TABLE dbo.sys_dict_data;
GO
IF OBJECT_ID(N'dbo.sys_organization', N'U') IS NOT NULL DROP TABLE dbo.sys_organization;
GO
IF OBJECT_ID(N'dbo.sys_role', N'U') IS NOT NULL DROP TABLE dbo.sys_role;
GO
IF OBJECT_ID(N'dbo.sys_function', N'U') IS NOT NULL DROP TABLE dbo.sys_function;
GO
IF OBJECT_ID(N'dbo.sys_interface_group', N'U') IS NOT NULL DROP TABLE dbo.sys_interface_group;
GO
IF OBJECT_ID(N'dbo.sys_notice', N'U') IS NOT NULL DROP TABLE dbo.sys_notice;
GO
IF OBJECT_ID(N'dbo.sys_file_record', N'U') IS NOT NULL DROP TABLE dbo.sys_file_record;
GO
IF OBJECT_ID(N'dbo.sys_dict_category', N'U') IS NOT NULL DROP TABLE dbo.sys_dict_category;
GO
IF OBJECT_ID(N'dbo.sys_config', N'U') IS NOT NULL DROP TABLE dbo.sys_config;
GO
IF OBJECT_ID(N'dbo.sys_request_log', N'U') IS NOT NULL DROP TABLE dbo.sys_request_log;
GO
IF OBJECT_ID(N'dbo.sys_background_job_record', N'U') IS NOT NULL DROP TABLE dbo.sys_background_job_record;
GO

/* ---------------------------- sys_background_job_record 后台作业记录表 ---------------------------- */
CREATE TABLE dbo.sys_background_job_record (
    id              NVARCHAR(40)    NOT NULL,
    application_name NVARCHAR(128)  NOT NULL,
    job_name        NVARCHAR(128)   NOT NULL,
    job_args        NVARCHAR(MAX)   NOT NULL,
    try_count       DECIMAL(10, 0)  NULL,
    creation_time   DATETIME2(3)    NULL,
    next_try_time   DATETIME2(3)    NULL,
    last_try_time   DATETIME2(3)    NULL,
    is_abandoned    DECIMAL(1, 0)   NULL,
    priority        DECIMAL(10, 0)  NULL,
    CONSTRAINT PK_sys_background_job_record PRIMARY KEY CLUSTERED (id)
);
GO

/* ---------------------------- sys_dict_category 字典分类 ---------------------------- */
CREATE TABLE dbo.sys_dict_category (
    id          BIGINT          NOT NULL,
    create_by   BIGINT          NOT NULL,
    create_time DATETIME2(3)    NOT NULL,
    update_by   BIGINT          NULL,
    update_time DATETIME2(3)    NULL,
    remark      NVARCHAR(1000)  NULL,
    name        NVARCHAR(20)    NOT NULL,
    code        NVARCHAR(40)    NOT NULL,
    CONSTRAINT PK_sys_dict_category PRIMARY KEY CLUSTERED (id),
    CONSTRAINT UK_sys_CATEGORY_CODE UNIQUE (code)
);
GO

/* ---------------------------- sys_dict_data 字典数据 ---------------------------- */
CREATE TABLE dbo.sys_dict_data (
    id          BIGINT          NOT NULL,
    create_by   BIGINT          NOT NULL,
    create_time DATETIME2(3)    NOT NULL,
    update_by   BIGINT          NULL,
    update_time DATETIME2(3)    NULL,
    remark      NVARCHAR(1000)  NULL,
    category_id BIGINT          NOT NULL,
    name        NVARCHAR(20)    NOT NULL,
    code        NVARCHAR(20)    NOT NULL,
    sort        DECIMAL(10, 0)  NOT NULL,
    CONSTRAINT PK_sys_dict_data PRIMARY KEY CLUSTERED (id),
    CONSTRAINT FK_sys_dict_data_category FOREIGN KEY (category_id)
        REFERENCES dbo.sys_dict_category (id) ON DELETE CASCADE
);
GO
CREATE INDEX IX_sys_dict_data_category ON dbo.sys_dict_data (category_id);
GO

/* ---------------------------- sys_file_record 文件上传记录表 ---------------------------- */
CREATE TABLE dbo.sys_file_record (
    id          BIGINT          NOT NULL,
    create_by   BIGINT          NOT NULL,
    create_time DATETIME2(3)    NOT NULL,
    update_by   BIGINT          NULL,
    update_time DATETIME2(3)    NULL,
    remark      NVARCHAR(1000)  NULL,
    file_name   NVARCHAR(100)   NOT NULL,
    file_size   DECIMAL(10, 0)  NOT NULL,
    file_ext    NVARCHAR(10)    NOT NULL,
    CONSTRAINT PK_sys_file_record PRIMARY KEY CLUSTERED (id)
);
GO

/* ---------------------------- sys_function 功能表 ---------------------------- */
CREATE TABLE dbo.sys_function (
    id          BIGINT          NOT NULL,
    create_by   BIGINT          NOT NULL,
    create_time DATETIME2(3)    NOT NULL,
    update_by   BIGINT          NULL,
    update_time DATETIME2(3)    NULL,
    remark      NVARCHAR(1000)  NULL,
    name        NVARCHAR(20)    NOT NULL,
    code        NVARCHAR(40)    NOT NULL,
    parent_id   BIGINT          NULL,
    CONSTRAINT PK_sys_function PRIMARY KEY CLUSTERED (id),
    CONSTRAINT UK_sys_FUNCTION_CODE UNIQUE (code)
);
GO

/* ---------------------------- sys_interface_group 接口分组表 ---------------------------- */
CREATE TABLE dbo.sys_interface_group (
    id          BIGINT          NOT NULL,
    create_by   BIGINT          NOT NULL,
    create_time DATETIME2(3)    NOT NULL,
    update_by   BIGINT          NULL,
    update_time DATETIME2(3)    NULL,
    remark      NVARCHAR(1000)  NULL,
    name        NVARCHAR(20)    NULL,
    code        NVARCHAR(40)    NOT NULL,
    CONSTRAINT PK_sys_interface_group PRIMARY KEY CLUSTERED (id),
    CONSTRAINT UK_sys_IG_CODE UNIQUE (code)
);
GO

/* ---------------------------- sys_interface 接口表 ---------------------------- */
CREATE TABLE dbo.sys_interface (
    id              BIGINT          NOT NULL,
    create_by       BIGINT          NOT NULL,
    create_time     DATETIME2(3)    NOT NULL,
    update_by       BIGINT          NULL,
    update_time     DATETIME2(3)    NULL,
    remark          NVARCHAR(1000)  NULL,
    name            NVARCHAR(20)    NOT NULL,
    path            NVARCHAR(200)   NOT NULL,
    request_method  NVARCHAR(20)    NOT NULL,
    group_id        BIGINT          NULL,
    CONSTRAINT PK_sys_interface PRIMARY KEY CLUSTERED (id),
    CONSTRAINT uk_interface_pathmethod UNIQUE (path, request_method),
    CONSTRAINT FK_sys_interface_group FOREIGN KEY (group_id)
        REFERENCES dbo.sys_interface_group (id) ON DELETE CASCADE
);
GO
CREATE INDEX IX_sys_interface_group ON dbo.sys_interface (group_id);
GO

/* ---------------------------- sys_function_interface 页面接口表 ---------------------------- */
CREATE TABLE dbo.sys_function_interface (
    id              BIGINT          NOT NULL,
    create_by       BIGINT          NOT NULL,
    create_time     DATETIME2(3)    NOT NULL,
    update_by       BIGINT          NULL,
    update_time     DATETIME2(3)    NULL,
    remark          NVARCHAR(1000)  NULL,
    interface_id    BIGINT          NULL,
    function_id     BIGINT          NULL,
    CONSTRAINT PK_sys_function_interface PRIMARY KEY CLUSTERED (id),
    CONSTRAINT FK_sys_fi_function FOREIGN KEY (function_id)
        REFERENCES dbo.sys_function (id) ON DELETE CASCADE,
    CONSTRAINT FK_sys_fi_interface FOREIGN KEY (interface_id)
        REFERENCES dbo.sys_interface (id) ON DELETE CASCADE
);
GO
CREATE INDEX IX_sys_fi_function ON dbo.sys_function_interface (function_id);
GO
CREATE INDEX IX_sys_fi_interface ON dbo.sys_function_interface (interface_id);
GO

/* ---------------------------- sys_notice 通知公告表 ---------------------------- */
CREATE TABLE dbo.sys_notice (
    id          BIGINT          NOT NULL,
    create_by   BIGINT          NOT NULL,
    create_time DATETIME2(3)    NOT NULL,
    update_by   BIGINT          NULL,
    update_time DATETIME2(3)    NULL,
    remark      NVARCHAR(1000)  NULL,
    title       NVARCHAR(40)    NOT NULL,
    content     NVARCHAR(MAX)   NULL,
    notice_type BIGINT          NOT NULL,
    level       BIGINT          NULL,
    CONSTRAINT PK_sys_notice PRIMARY KEY CLUSTERED (id)
);
GO

/* ---------------------------- sys_notice_record 通知公告记录表 ---------------------------- */
CREATE TABLE dbo.sys_notice_record (
    id          BIGINT          NOT NULL,
    create_by   BIGINT          NOT NULL,
    create_time DATETIME2(3)    NOT NULL,
    update_by   BIGINT          NULL,
    update_time DATETIME2(3)    NULL,
    remark      NVARCHAR(1000)  NULL,
    receiver    BIGINT          NOT NULL,
    is_read     DECIMAL(1, 0)   NOT NULL,
    notice_id   BIGINT          NOT NULL,
    CONSTRAINT PK_sys_notice_record PRIMARY KEY CLUSTERED (id),
    CONSTRAINT FK_sys_notice_record_notice FOREIGN KEY (notice_id)
        REFERENCES dbo.sys_notice (id) ON DELETE CASCADE
);
GO
CREATE INDEX IX_sys_notice_record_notice ON dbo.sys_notice_record (notice_id);
GO

/* ---------------------------- sys_organization 组织机构 ---------------------------- */
CREATE TABLE dbo.sys_organization (
    id          BIGINT          NOT NULL,
    create_by   BIGINT          NOT NULL,
    create_time DATETIME2(3)    NOT NULL,
    update_by   BIGINT          NULL,
    update_time DATETIME2(3)    NULL,
    remark      NVARCHAR(1000)  NULL,
    name        NVARCHAR(100)   NOT NULL,
    parent_id   BIGINT          NULL,
    telephone   NVARCHAR(20)    NULL,
    leader      NVARCHAR(20)    NULL,
    sort        DECIMAL(10, 0)  NULL,
    CONSTRAINT PK_sys_organization PRIMARY KEY CLUSTERED (id),
    CONSTRAINT UK_sys_ORG_NAME_PID UNIQUE (name, parent_id),
    CONSTRAINT FK_sys_organization_parent FOREIGN KEY (parent_id)
        REFERENCES dbo.sys_organization (id)
);
GO
CREATE INDEX IX_sys_organization_parent ON dbo.sys_organization (parent_id);
GO

/* ---------------------------- sys_profile_system 系统文件表 ---------------------------- */
CREATE TABLE dbo.sys_profile_system (
    id          BIGINT          NOT NULL,
    create_by   BIGINT          NOT NULL,
    create_time DATETIME2(3)    NOT NULL,
    update_by   BIGINT          NULL,
    update_time DATETIME2(3)    NULL,
    remark      NVARCHAR(1000)  NULL,
    name        NVARCHAR(20)    NOT NULL,
    code        NVARCHAR(40)    NOT NULL,
    file_id     BIGINT          NOT NULL,
    CONSTRAINT PK_sys_profile_system PRIMARY KEY CLUSTERED (id),
    CONSTRAINT UK_sys_FILESYSTEM_CODE UNIQUE (code),
    CONSTRAINT FK_sys_profile_system_file FOREIGN KEY (file_id)
        REFERENCES dbo.sys_file_record (id) ON DELETE CASCADE
);
GO
CREATE INDEX IX_sys_profile_system_file ON dbo.sys_profile_system (file_id);
GO

/* ---------------------------- sys_request_log 请求日志表 ---------------------------- */
CREATE TABLE dbo.sys_request_log (
    id                  BIGINT          NOT NULL,
    create_by           BIGINT          NOT NULL,
    create_time         DATETIME2(3)    NOT NULL,
    update_by           BIGINT          NULL,
    update_time         DATETIME2(3)    NULL,
    remark              NVARCHAR(1000)  NULL,
    controller_name     NVARCHAR(100)   NULL,
    action_name         NVARCHAR(100)   NULL,
    request_method      NVARCHAR(10)    NULL,
    environment_name    NVARCHAR(20)    NULL,
    elapsed_time        DECIMAL(16, 0)  NULL,
    client_ip           NVARCHAR(20)    NULL,
    CONSTRAINT PK_sys_request_log PRIMARY KEY CLUSTERED (id)
);
GO

/* ---------------------------- sys_role 角色 ---------------------------- */
CREATE TABLE dbo.sys_role (
    id          BIGINT          NOT NULL,
    create_by   BIGINT          NOT NULL,
    create_time DATETIME2(3)    NOT NULL,
    update_by   BIGINT          NULL,
    update_time DATETIME2(3)    NULL,
    remark      NVARCHAR(1000)  NULL,
    name        NVARCHAR(20)    NOT NULL,
    description NVARCHAR(200)   NULL,
    parent_id   BIGINT          NULL,
    CONSTRAINT PK_sys_role PRIMARY KEY CLUSTERED (id),
    CONSTRAINT UK_sys_ROLE_NAME UNIQUE (name)
);
GO

/* ---------------------------- sys_role_function 角色功能表 ---------------------------- */
CREATE TABLE dbo.sys_role_function (
    id          BIGINT          NOT NULL,
    create_by   BIGINT          NOT NULL,
    create_time DATETIME2(3)    NOT NULL,
    update_by   BIGINT          NULL,
    update_time DATETIME2(3)    NULL,
    remark      NVARCHAR(1000)  NULL,
    role_id     BIGINT          NULL,
    function_id BIGINT          NULL,
    CONSTRAINT PK_sys_role_function PRIMARY KEY CLUSTERED (id),
    CONSTRAINT FK_sys_rf_role FOREIGN KEY (role_id)
        REFERENCES dbo.sys_role (id) ON DELETE CASCADE,
    CONSTRAINT FK_sys_rf_function FOREIGN KEY (function_id)
        REFERENCES dbo.sys_function (id) ON DELETE CASCADE
);
GO
CREATE INDEX IX_sys_rf_role ON dbo.sys_role_function (role_id);
GO
CREATE INDEX IX_sys_rf_function ON dbo.sys_role_function (function_id);
GO

/* ---------------------------- sys_config 系统配置表 ---------------------------- */
CREATE TABLE dbo.sys_config (
    id          BIGINT          NOT NULL,
    create_by   BIGINT          NOT NULL,
    create_time DATETIME2(3)    NOT NULL,
    update_by   BIGINT          NULL,
    update_time DATETIME2(3)    NULL,
    remark      NVARCHAR(1000)  NULL,
    name        NVARCHAR(20)    NULL,
    config_code NVARCHAR(40)    NOT NULL,
    config_value NVARCHAR(1000) NULL,
    CONSTRAINT PK_sys_config PRIMARY KEY CLUSTERED (id),
    CONSTRAINT UK_sys_CONFIG_CODE UNIQUE (config_code)
);
GO

/* ---------------------------- sys_user 用户 ---------------------------- */
CREATE TABLE dbo.sys_user (
    id              BIGINT          NOT NULL,
    create_by       BIGINT          NOT NULL,
    create_time     DATETIME2(3)    NOT NULL,
    update_by       BIGINT          NULL,
    update_time     DATETIME2(3)    NULL,
    remark          NVARCHAR(1000)  NULL,
    account         NVARCHAR(36)    NOT NULL,
    password        NVARCHAR(100)   NOT NULL,
    name            NVARCHAR(20)    NOT NULL,
    telephone       NVARCHAR(11)    NULL,
    email           NVARCHAR(20)    NULL,
    avatar          VARBINARY(MAX)  NULL,
    status          DECIMAL(10, 0)  NULL,
    organization_id BIGINT          NOT NULL,
    CONSTRAINT PK_sys_user PRIMARY KEY CLUSTERED (id),
    CONSTRAINT UK_sys_USER_ACCOUNT UNIQUE (account),
    CONSTRAINT FK_sys_user_organization FOREIGN KEY (organization_id)
        REFERENCES dbo.sys_organization (id)
);
GO
CREATE INDEX IX_sys_user_organization ON dbo.sys_user (organization_id);
GO

/* ---------------------------- sys_user_role 用户角色 ---------------------------- */
CREATE TABLE dbo.sys_user_role (
    id          BIGINT          NOT NULL,
    create_by   BIGINT          NOT NULL,
    create_time DATETIME2(3)    NOT NULL,
    update_by   BIGINT          NULL,
    update_time DATETIME2(3)    NULL,
    remark      NVARCHAR(1000)  NULL,
    role_id     BIGINT          NOT NULL,
    user_id     BIGINT          NOT NULL,
    CONSTRAINT PK_sys_user_role PRIMARY KEY CLUSTERED (id),
    CONSTRAINT FK_sys_ur_role FOREIGN KEY (role_id)
        REFERENCES dbo.sys_role (id) ON DELETE CASCADE,
    CONSTRAINT FK_sys_ur_user FOREIGN KEY (user_id)
        REFERENCES dbo.sys_user (id) ON DELETE CASCADE
);
GO
CREATE INDEX IX_sys_ur_role ON dbo.sys_user_role (role_id);
GO
CREATE INDEX IX_sys_ur_user ON dbo.sys_user_role (user_id);
GO

/* ---------------------------- sys_oauth2_user OAUTH2用户 ---------------------------- */
CREATE TABLE dbo.sys_oauth2_user (
    persistence_id  BIGINT          NOT NULL,
    create_time     DATETIME2(3)    NOT NULL,
    id              BIGINT          NULL,
    name            NVARCHAR(20)    NULL,
    type            NVARCHAR(20)    NOT NULL,
    user_id         BIGINT          NULL,
    CONSTRAINT PK_sys_oauth2_user PRIMARY KEY CLUSTERED (persistence_id),
    CONSTRAINT FK_sys_oauth2_user_user FOREIGN KEY (user_id)
        REFERENCES dbo.sys_user (id) ON DELETE CASCADE
);
GO
CREATE INDEX IX_sys_oauth2_user_user ON dbo.sys_oauth2_user (user_id);
GO

/* ---------------------------- wf_auditing_record 流程审批记录 ---------------------------- */
CREATE TABLE dbo.wf_auditing_record (
    id                  BIGINT          NOT NULL,
    execution_pointer_id BIGINT         NOT NULL,
    auditing_time       DATETIME2(3)    NOT NULL,
    auditor             BIGINT          NOT NULL,
    auditor_name        NVARCHAR(40)    NULL,
    auditing_opinion    NVARCHAR(MAX)   NULL,
    is_agree            BIT             NOT NULL,
    CONSTRAINT PK_wf_auditing_record PRIMARY KEY CLUSTERED (id)
);
GO

/* ---------------------------- wf_definition 流程定义 ---------------------------- */
CREATE TABLE dbo.wf_definition (
    id                  BIGINT          NOT NULL,
    create_by           BIGINT          NOT NULL,
    create_time         DATETIME2(3)    NOT NULL,
    update_by           BIGINT          NULL,
    update_time         DATETIME2(3)    NULL,
    remark              NVARCHAR(1000)  NULL,
    name                NVARCHAR(20)    NOT NULL,
    definition_id       NVARCHAR(36)    NOT NULL,
    workflow_content    NVARCHAR(MAX)   NOT NULL,
    designs_content     NVARCHAR(MAX)   NOT NULL,
    form_content        NVARCHAR(MAX)   NOT NULL,
    version             INT             NOT NULL,
    is_locked           BIT             NOT NULL,
    CONSTRAINT PK_wf_definition PRIMARY KEY CLUSTERED (id),
    CONSTRAINT uk_workflow_code UNIQUE (definition_id)
);
GO

/* ---------------------------- wf_event 事件 ---------------------------- */
CREATE TABLE dbo.wf_event (
    persistence_id  BIGINT          NOT NULL,
    event_id        NVARCHAR(36)    NOT NULL,
    event_name      NVARCHAR(200)   NULL,
    event_key       NVARCHAR(200)   NULL,
    event_data      NVARCHAR(MAX)   NULL,
    event_time      DATETIME2(3)    NOT NULL,
    is_processed    BIT             NOT NULL,
    CONSTRAINT PK_wf_event PRIMARY KEY CLUSTERED (persistence_id),
    CONSTRAINT IX_Event_EventId UNIQUE (event_id)
);
GO
CREATE INDEX IX_Event_EventTime ON dbo.wf_event (event_time);
GO
CREATE INDEX IX_Event_IsProcessed ON dbo.wf_event (is_processed);
GO
CREATE INDEX IX_Event_EventName_EventKey ON dbo.wf_event (event_name, event_key);
GO

/* ---------------------------- wf_workflow 工作流程 ---------------------------- */
CREATE TABLE dbo.wf_workflow (
    persistence_id          BIGINT          NOT NULL,
    complete_time           DATETIME2(3)    NULL,
    create_by               BIGINT          NOT NULL,
    create_time             DATETIME2(3)    NOT NULL,
    data                    NVARCHAR(MAX)   NULL,
    description             NVARCHAR(500)   NULL,
    instance_id             NVARCHAR(36)    NOT NULL,
    next_execution          BIGINT          NULL,
    status                  INT             NOT NULL,
    version                 INT             NOT NULL,
    workflow_definition_id  NVARCHAR(36)    NOT NULL,
    reference               NVARCHAR(200)   NULL,
    remark                  NVARCHAR(1000)  NULL,
    CONSTRAINT PK_wf_workflow PRIMARY KEY CLUSTERED (persistence_id)
);
GO
CREATE INDEX IX_Workflow_InstanceId ON dbo.wf_workflow (instance_id);
GO
CREATE INDEX IX_Workflow_NextExecution ON dbo.wf_workflow (next_execution);
GO

/* ---------------------------- wf_execution_pointer 步骤 ---------------------------- */
CREATE TABLE dbo.wf_execution_pointer (
    persistence_id      BIGINT          NOT NULL,
    workflow_id         BIGINT          NOT NULL,
    id                  NVARCHAR(36)    NULL,
    start_time          DATETIME2(3)    NULL,
    end_time            DATETIME2(3)    NULL,
    active              BIT             NOT NULL,
    event_key           NVARCHAR(100)   NULL,
    event_name          NVARCHAR(100)   NULL,
    event_data          NVARCHAR(MAX)   NULL,
    event_published     BIT             NOT NULL,
    persistence_data    NVARCHAR(MAX)   NULL,
    sleep_until         DATETIME2(3)    NULL,
    step_id             INT             NOT NULL,
    step_name           NVARCHAR(100)   NULL,
    children            NVARCHAR(MAX)   NULL,
    context_item        NVARCHAR(MAX)   NULL,
    predecessor_id      NVARCHAR(100)   NULL,
    outcome             NVARCHAR(MAX)   NULL,
    scope               NVARCHAR(MAX)   NULL,
    retry_count         INT             NOT NULL,
    status              INT             NOT NULL,
    CONSTRAINT PK_wf_execution_pointer PRIMARY KEY CLUSTERED (persistence_id),
    CONSTRAINT FK_ExecutionPointer_Workflow_WorkflowId FOREIGN KEY (workflow_id)
        REFERENCES dbo.wf_workflow (persistence_id) ON DELETE CASCADE
);
GO
CREATE INDEX IX_ExecutionPointer_WorkflowId ON dbo.wf_execution_pointer (workflow_id);
GO

/* ---------------------------- wf_execution_attribute 自定义属性 ---------------------------- */
CREATE TABLE dbo.wf_execution_attribute (
    persistence_id          BIGINT          NOT NULL,
    attribute_key           NVARCHAR(100)   NULL,
    attribute_value         NVARCHAR(MAX)   NULL,
    execution_pointer_id    BIGINT          NOT NULL,
    CONSTRAINT PK_wf_execution_attribute PRIMARY KEY CLUSTERED (persistence_id),
    CONSTRAINT FK_ExtensionAttribute_ExecutionPointer_ExecutionPointerId FOREIGN KEY (execution_pointer_id)
        REFERENCES dbo.wf_execution_pointer (persistence_id) ON DELETE CASCADE
);
GO
CREATE INDEX IX_ExtensionAttribute_ExecutionPointerId ON dbo.wf_execution_attribute (execution_pointer_id);
GO

/* ---------------------------- wf_execution_error 执行异常 ---------------------------- */
CREATE TABLE dbo.wf_execution_error (
    persistence_id          BIGINT          NOT NULL,
    error_time              DATE            NOT NULL,
    execution_pointer_id    NVARCHAR(100)   NULL,
    message                 NVARCHAR(MAX)   NULL,
    workflow_id             NVARCHAR(100)   NULL,
    CONSTRAINT PK_wf_execution_error PRIMARY KEY CLUSTERED (persistence_id)
);
GO

/* ---------------------------- wf_scheduled_command 计划命令 ---------------------------- */
CREATE TABLE dbo.wf_scheduled_command (
    persistence_id  BIGINT          NOT NULL,
    command_name    NVARCHAR(200)   NULL,
    data            NVARCHAR(500)   NULL,
    execute_time    BIGINT          NOT NULL,
    CONSTRAINT PK_wf_scheduled_command PRIMARY KEY CLUSTERED (persistence_id),
    CONSTRAINT IX_ScheduledCommand_CommandName_Data UNIQUE (command_name, data)
);
GO
CREATE INDEX IX_ScheduledCommand_ExecuteTime ON dbo.wf_scheduled_command (execute_time);
GO

/* ---------------------------- wf_subscription 订阅 ---------------------------- */
CREATE TABLE dbo.wf_subscription (
    persistence_id          BIGINT          NOT NULL,
    event_key               NVARCHAR(200)   NULL,
    event_name              NVARCHAR(200)   NULL,
    step_id                 INT             NOT NULL,
    subscription_id         NVARCHAR(36)    NOT NULL,
    workflow_id             NVARCHAR(200)   NULL,
    subscribe_as_of         DATETIME2(3)    NOT NULL,
    subscription_data       NVARCHAR(MAX)   NULL,
    execution_pointer_id    NVARCHAR(200)   NULL,
    external_token          NVARCHAR(200)   NULL,
    external_token_expiry   DATETIME2(3)    NULL,
    external_worker_id      NVARCHAR(200)   NULL,
    CONSTRAINT PK_wf_subscription PRIMARY KEY CLUSTERED (persistence_id),
    CONSTRAINT IX_Subscription_SubscriptionId UNIQUE (subscription_id)
);
GO
CREATE INDEX IX_Subscription_EventKey ON dbo.wf_subscription (event_key);
GO
CREATE INDEX IX_Subscription_EventName ON dbo.wf_subscription (event_name);
GO

/* ---------------------------- wf_waiting_pointer 待审核步骤 ---------------------------- */
CREATE TABLE dbo.wf_waiting_pointer (
    id          BIGINT          NOT NULL,
    user_id     BIGINT          NOT NULL,
    pointer_id  NVARCHAR(36)    NOT NULL,
    CONSTRAINT PK_wf_waiting_pointer PRIMARY KEY CLUSTERED (id)
);
GO

/* ---------------------------- wf_instance 流程实例 ---------------------------- */
CREATE TABLE dbo.wf_instance (
    id                  BIGINT          NOT NULL,
    create_by           BIGINT          NOT NULL,
    create_time         DATETIME2(3)    NOT NULL,
    update_by           BIGINT          NULL,
    update_time         DATETIME2(3)    NULL,
    remark              NVARCHAR(1000)  NULL,
    wf_id               BIGINT          NOT NULL,
    scheme_id           BIGINT          NOT NULL,
    form_data           NVARCHAR(MAX)   NOT NULL,
    current_node        BIGINT          NULL,
    current_node_type   INT             NOT NULL,
    status              INT             NOT NULL,
    CONSTRAINT PK_wf_instance PRIMARY KEY CLUSTERED (id),
    CONSTRAINT FK_wf_instance_workflow FOREIGN KEY (wf_id)
        REFERENCES dbo.wf_workflow (persistence_id),
    CONSTRAINT FK_wf_instance_definition FOREIGN KEY (scheme_id)
        REFERENCES dbo.wf_definition (id)
);
GO
CREATE INDEX IX_wf_instance_workflow ON dbo.wf_instance (wf_id);
GO
CREATE INDEX IX_wf_instance_definition ON dbo.wf_instance (scheme_id);
GO
