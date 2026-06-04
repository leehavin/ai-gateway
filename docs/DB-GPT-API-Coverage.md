# DB-GPT 官方 API 对接情况

> 对照 [DB-GPT 官方文档 v0.8](http://docs.dbgpt.cn/docs/application/advanced_tutorial/api)、[API 认证](http://docs.dbgpt.cn/docs/api/introduction)、[App](http://docs.dbgpt.cn/docs/api/app)、[Datasource](http://docs.dbgpt.cn/docs/api/datasource)、[Knowledge](http://docs.dbgpt.cn/docs/api/knowledge)。

## 结论（先说）

**没有全部对接。** 当前 DataChat 只覆盖产品必需的一小部分 **v2 稳定 API**；大量 **v1 遗留接口**、**Editor / Agent / AWEL / Model 管理** 未代理。

官方建议：启动 webserver 后打开 `{dbgpt}/docs` 查看完整 OpenAPI。

---

## 一、官方 API 体系（两套）

| 体系 | 端口 | 说明 |
|------|------|------|
| **应用服务层** | **5670** | Chat、App、Datasource、Knowledge 等，`/api/v1`、`/api/v2` |
| **Model API (SMMF)** | **8100** | OpenAI 兼容 `/api/v1`，多模型推理 |

DataChat Gateway 主要代理 **5670 / v2**。

---

## 二、v2 核心：统一对话入口

`POST /api/v2/chat/completions`（Bearer `DBGPT_API_KEY`）

通过 `chat_mode` + `chat_param` 切换场景（[ChatMode 官方枚举](https://github.com/eosphoros-ai/DB-GPT/blob/main/packages/dbgpt-client/src/dbgpt_client/schema.py)）：

| chat_mode | chat_param | 用途 |
|-----------|------------|------|
| `chat_normal` | — | 普通对话 |
| `chat_app` | app_id | AWEL/Agent 应用 |
| `chat_data` | 数据源名/id | 数据问数 Text2SQL |
| `chat_knowledge` | 知识空间名 | 知识库 RAG |
| `chat_flow` | flow id | AWEL Flow |
| `chat_with_db_qa` | — | 库问答 |
| `chat_dashboard` | — | 看板 |

**官方 `messages` 类型**：`string | string[]`（**不是** OpenAI 的 `{role, content}[]`）。  
多轮由服务端 `conv_uid` 或拼进单条 prompt；DataChat 选择 **本地 history 拼成单条字符串** 再转发。

---

## 三、对接矩阵

### 已对接（Gateway / Provider）

| 官方 API | DataChat 路径 | 状态 |
|----------|---------------|------|
| `POST /api/v2/chat/completions`（流式） | `DbGptChatProvider` → 领域 `provider=dbgpt` | ✅ 已修复 messages 格式 |
| `GET /api/v2/serve/apps` | Gateway 未代理（直连 DB-GPT 或管理后台） | — |
| `GET /api/v2/serve/datasources` | 同上 | — |
| `GET /api/v2/serve/knowledge/spaces` | 同上 | — |
| 健康探测 | `GET /v1/health` → `dbgptReachable` | ✅ |

### 未对接（按需排期）

| 分类 | 官方 API 示例 | 说明 |
|------|---------------|------|
| 对话非流式 | `POST /api/v2/chat/completions` stream=false | 未单独暴露 |
| 数据源 CRUD | POST/PUT/DELETE `/api/v2/serve/datasources` | 管理在 DB-GPT 控制台 |
| 知识库 CRUD | POST/PUT/DELETE `/api/v2/serve/knowledge/spaces` | 同上 |
| 知识库文档 | knowledge documents API | 测试阶段，文档称逐步开放 |
| **v1 Chat** | `/api/v1/chat/db/*`、`dialogue/*`、`prepare` | 旧版，建议用 v2 |
| **Editor** | `/api/v1/editor/*` | SQL/图表编辑器 |
| **LLM Manage** | `/api/v1/worker/*`、`/api/controller/*` | 模型集群运维 |
| **Agent** | `/api/v1/agent/*` | Agent 市场安装 |
| **AWEL** | `/api/v1/awel/*` | 工作流触发器 |
| **Model API 8100** | `http://host:8100/api/v1` | 未代理；可直连 SMMF |

### 自研领域 HTTP

| 路径 | 状态 |
|------|------|
| `POST /v1/chat/stream` → `CustomDomainProvider` | ✅ 与官方无关，公司内部契约 |

---

## 四、架构建议

```
Web/WinForms → DataChat Gateway (/v1/*)
                    ├─ custom → 自研领域服务
                    └─ dbgpt  → 仅代理官方 v2 子集（上表「已对接」）
                                    ↓
                         公司 DB-GPT :5670
```

**不必** 1:1 代理官方全部 API；未列能力继续在 DB-GPT Web 管理端操作，Gateway 只暴露聊天与资源**读**接口。

---

## 五、配置检查清单（接真实环境）

1. `dc_global_defaults.dbgpt_base_url` 指向公司 `https://dbgpt...:5670`
2. `ApiKeys:dbgpt-main` 与 DB-GPT 环境变量 `API_KEYS` 一致
3. `Gateway:UseMock` = `false`
4. 领域 `dbgpt.chatMode` / `chat_param` 与官方表一致（如 `chat_data` + 数据源 id）
5. 浏览器访问 `{dbgpt}/docs` 核对现场版本是否仍有字段差异

---

*文档随 Gateway 代码更新；以贵司部署的 DB-GPT 版本 OpenAPI 为准。*
