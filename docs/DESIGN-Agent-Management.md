# 智能体（Agent）通用管理 & 权限方案

## 目标

在 **ai-admin** 统一管理多平台智能体（Coze / DB-GPT / 自研 HTTP / OpenAI 兼容等），按 **角色** 授权后，**chat-ui** 用户登录仅可见、可用被分配的智能体及其子资源（工作流等）。

> `db/admin` 的 `wf_*` = AIAdmin **内部审批流**；本文 `dc_agent_resource.resource_type=workflow` = **对话侧子能力**（如 Coze 工作流），二者无关。

---

## 设计原则：通用模型 + Provider 扩展

不把表名、列名绑死在 Coze 上。差异配置放进 JSON，Gateway 按 `provider` 组装现有 `DomainProfile`。

```
dc_provider_account     连接器（密钥、endpoint）
        │
        ▼
dc_agent                智能体（id = Gateway domain）
        │
        ▼
dc_agent_resource       子资源（workflow / tool / skill …）
```

新增一种 Agent 时通常只需：

1. `dc_provider_account.provider` 枚举扩展（或沿用 custom）
2. 约定 `dc_agent.config_json` 字段形状
3. Gateway 增加/复用 `IChatProvider`
4. Admin 增加对应表单模板

**无需改权限表结构。**

---

## 架构

```
ai-admin                              chat-ui
  ├─ 连接器管理 (dc_provider_account)     ├─ 登录 JWT
  ├─ 智能体管理 (dc_agent)                ├─ @ 智能体（过滤）
  ├─ 子资源管理 (dc_agent_resource)       └─ /coze 等工作流（过滤）
  └─ 角色授权 (dc_role_agent / dc_role_resource)
                    │
                    ▼
            DataChat.Gateway
  ├─ JWT → sys_user → sys_role
  ├─ GET /v1/domains          授权智能体 → 组装 DomainProfile
  ├─ GET /v1/coze/workflows   授权 + resource_type=workflow
  └─ stream / workflow/*      二次鉴权
```

---

## 数据模型（`db/chat/04-agent-management.sql`）

| 表 | 说明 |
|----|------|
| `dc_provider_account` | 连接器：`provider` + endpoint + 密钥 |
| `dc_agent` | 智能体：`provider` + `chat_mode` + `config_json` |
| `dc_agent_resource` | 子资源：`resource_type` + `external_id` + `config_json` |
| `dc_role_agent` | 角色 ↔ 智能体 |
| `dc_role_resource` | 角色 ↔ 子资源（细粒度，可选） |
| `dc_user_agent` | 用户直授（可选） |

### Provider 与 config_json 约定

| provider | chat_mode 示例 | config_json 主要字段 |
|----------|----------------|----------------------|
| `coze` | CozeAgent | botId, workspaceId, autoSaveHistory, userIdPrefix |
| `dbgpt` | DbGptData / DbGptApp | chatMode, datasourceId, appId |
| `custom` | DomainCustom | endpoint, apiKeyRef, adapter |
| `openai` | — | baseUrl, apiKeyRef, model |

### resource_type 约定

| 类型 | 说明 | Chat 表现 |
|------|------|-----------|
| `workflow` | Coze 工作流等 | `/coze`、workflow stream API |
| `tool` | 预留 MCP/插件 | 后续扩展 |
| `skill` | 预留组合能力 | 后续扩展 |

---

## 权限规则

| 资源 | 规则 |
|------|------|
| **智能体** | `dc_role_agent` 命中 或 SuperAdmin → 可见 |
| **子资源** | 需先有智能体权限；若该智能体**无**任何 `dc_role_resource` → 继承全部已启用子资源；否则仅授权项 |
| **调用** | `domain` / `workflowId` / `resource` 必须在授权集内 |

---

## 与 `dc_domain` 过渡

| 阶段 | Gateway 行为 |
|------|----------------|
| 过渡期 | 可读 `dc_domain`；Admin 写入 `dc_agent` |
| 迁移 | `05-migrate-dc-domain-to-agent.sql` |
| 终态 | `IDomainCatalog` 从 `dc_agent` 组装 `DomainProfile`（含 Coze.Workflows 来自 `dc_agent_resource`） |

---

## 管理端 API（P0 已实现）

Swagger 分组：**智能体管理** (`agent`)

| 服务 | 主要路由 |
|------|----------|
| `ProviderAccountService` | `GET/POST /v1/provider-account`，`GET/PUT/DELETE /v1/provider-account/{id}`，`GET /v1/provider-account?provider=`（下拉列表） |
| `AgentService` | `GET/POST /v1/agent`，`GET/PUT/DELETE /v1/agent/{id}`，`GET /v1/agent?provider=&enabled=`（下拉列表） |
| `AgentResourceService` | `GET/POST /v1/agent-resource`，`GET/PUT/DELETE /v1/agent-resource/{id}`，`GET /v1/agent-resource/by-agent/{agentId}` |
| `AgentAccessService` | `GET /v1/agent-access/{roleId}/agents`，`POST /v1/agent-access/{roleId}/assign-agents`；`GET /v1/agent-access/{roleId}/resources`，`POST /v1/agent-access/{roleId}/assign-resources` |
| `CozeDiscoveryService` | `GET /v1/coze-discovery/{providerAccountId}/workspaces`；`GET .../bots?space=`；`GET .../workflows?space=&bot=` |

菜单权限码（`--seed`）：`agent.*`、`agent.provider.*`、`agent.manage.*`、`agent.resource.*`、`agent.access.*`

| 菜单 | 表 |
|------|-----|
| 连接器管理 | `dc_provider_account` |
| 智能体管理 | `dc_agent`（按 provider 动态表单） |
| 子资源管理 | `dc_agent_resource` |
| 智能体授权 | `dc_role_agent` + `dc_role_resource` |

---

## SQL 执行顺序

```powershell
sqlcmd -S <host> -d chat -i db\chat\04-agent-management.sql
sqlcmd -S <host> -d chat -i db\chat\05-migrate-dc-domain-to-agent.sql   # 可选
```

---

## 实施阶段

| 阶段 | 内容 |
|------|------|
| P0 | 表结构 + Admin 连接器/智能体/子资源 CRUD |
| P1 | 角色授权 UI |
| P2 | Gateway：JWT + 过滤 + 从 `dc_agent` 组装 DomainProfile |
| P3 | chat-ui 登录与列表联调 |
| P4 | 接入第二种 Provider（如启用示例 `data-query` dbgpt 智能体） |
