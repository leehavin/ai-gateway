# WinForms 智能数据聊天客户端 — 系统设计

> 版本：**1.1（已确认选型）**  
> 场景：桌面对话框聊天，接入 DB-GPT，并扩展多个领域模型/应用。

---

## 0. 已确认选型（锁定）

| # | 项 | 决定 | 设计影响 |
|---|-----|------|----------|
| 1 | DB-GPT | **公司统一服务器** | `appsettings` 配置内网 `Dbgpt:BaseUrl`；启动健康检查；无需本机安装 DB-GPT |
| 2 | 消息区 | **WebView2** | `wwwroot/chat.html` + JS 桥；Markdown/代码块/折叠 Agent 块 |
| 3 | 上下文 | **只本地拼 history** | 每次请求携带完整/截断后的 `messages[]`；**不使用** `conv_uid` |
| 4 | 领域 | **大量自研 HTTP** | `CustomDomainProvider` 为主路径；DB-GPT 为辅（问数/App）；`domains.json` 重点配 `custom` 节点 |
| 5 | 登录 | **仅 API Key** | Bearer / `X-Api-Key`；Windows Credential 存密钥；无 AD/SSO（V1） |

---

## 1. 文档目的与范围

### 1.1 目标

构建 Windows 桌面应用（WinForms），在**统一聊天界面**中：

- 与 **DB-GPT** 交互：数据问数（Text2SQL）、Agent/AWEL 应用、知识库 RAG；
- 与 **其他领域能力** 交互：财务、法务、运维等（不同模型、Prompt、后端服务）；
- 支持**流式输出**、**多会话**、**可取消**、**本地历史**。

### 1.2 非目标（V1 不做）

- 在 WinForms 进程内嵌入 Python / DB-GPT 源码运行；
- 替代 DB-GPT Web 管理端（数据源、知识库维护仍在 DB-GPT 完成）；
- 移动端原生 App。

### 1.3 Web 客户端（已纳入）

- 使用 **`web/chat-ui`**（Vue 3）构建；可部署为静态站点或嵌入 **WebView2 / CefSharp**。
- 浏览器 **经 DataChat.Gateway** 访问领域/DB-GPT，不在页面暴露主密钥。详见 [web/chat-ui/README.md](../web/chat-ui/README.md)。

### 1.4 假设

| 项 | 假设 |
|----|------|
| DB-GPT | **公司内网统一节点**（如 `https://dbgpt.corp.example.com`），由运维部署维护 |
| 领域服务 | **多个自研 HTTP/SSE 微服务**，契约由 `domains.json` + Provider 适配 |
| 上下文 | 会话与多轮消息**仅存本地 SQLite**，请求时组装 `history` |
| 认证 | 各服务 **API Key**（Bearer 或自定义 Header），密钥不进仓库 |
| 客户端 | .NET 8、Windows 10+、**必须安装 WebView2 Runtime** |
| 用户 | 企业内网用户，单用户桌面会话为主 |

---

## 2. 逻辑架构

```
┌─────────────────────────────────────────────────────────────────┐
│                    Presentation (WinForms)                       │
│  MainForm → ChatPanel → SessionList / MessageList / InputBar    │
└────────────────────────────┬────────────────────────────────────┘
                             │ events / async
┌────────────────────────────▼────────────────────────────────────┐
│                    Application (Use Cases)                       │
│  SendMessageHandler │ StopGenerationHandler │ SwitchDomainHandler│
│  LoadSessionsHandler │ CreateSessionHandler                      │
└────────────────────────────┬────────────────────────────────────┘
                             │
┌────────────────────────────▼────────────────────────────────────┐
│                    Domain (Core)                                 │
│  ChatSession │ ChatMessage │ DomainProfile │ IChatProvider       │
│  ChatOrchestrator │ IConversationRepository                      │
└────────────────────────────┬────────────────────────────────────┘
                             │
        ┌────────────────────┼────────────────────┐
        ▼                    ▼                    ▼
┌───────────────┐  ┌─────────────────┐  ┌──────────────────┐
│ DbGptProvider │  │ OpenAiCompat    │  │ CustomDomain     │
│ (5670 SSE)    │  │ (8100/云 API)   │  │ (自研 REST)      │
└───────┬───────┘  └────────┬────────┘  └────────┬─────────┘
        │                   │                     │
        └───────────────────┼─────────────────────┘
                            ▼
              ┌─────────────────────────┐
              │  DB-GPT / 云 LLM / 内网  │
              └─────────────────────────┘
```

**原则**：UI 只认识 `ChatOrchestrator` 和 `DomainProfile`；所有 HTTP/SSE 细节下沉到 Provider。

---

## 3. 部署拓扑

### 3.1 生产（已确认）

```
[用户 PC]  WinForms + WebView2 + SQLite
      │
      ├──────────────────────────────────────┐
      │ HTTPS (内网)                          │ HTTPS
      ▼                                       ▼
[公司 DB-GPT 集群]                    [领域服务集群]
  https://dbgpt.corp/...                /api/legal/chat
  :5670 应用层 API                       /api/finance/chat
  数据源/知识库/App                       /api/ops/chat  ...
      ▲                                       ▲
      │ 辅：问数、AWEL App                   │ 主：大部分领域
      └──────── CustomDomainProvider ─────────┘
                DbGptChatProvider（少数模式）
```

### 3.2 开发机

- `appsettings.Development.json` 可将 `Dbgpt:BaseUrl` 指向公司测试环境或 VPN 后的统一地址；
- 自研领域服务用 Mock Server 或测试网关。

### 3.3 健康检查（启动时）

WinForms 启动后后台探测：

- `GET {DbgptBaseUrl}/docs` 或 `GET /api/v2/serve/apps` → DB-GPT 是否可用；
- 配置的各 `OpenAi:BaseUrl` → 可选探测 `GET /v1/models`；
- 失败：状态栏黄灯 +「数据服务未连接」，聊天发送前二次确认。

---

## 4. 功能模块

### 4.1 模块清单

| 模块 | 职责 |
|------|------|
| **会话管理** | 新建/删除/重命名会话，列表持久化 |
| **消息展示** | 用户/助手气泡，流式追加，Markdown/代码块 |
| **领域切换** | 下拉选择领域，绑定 Provider + 参数 |
| **模型选择** | 领域内可选模型列表（配置或动态拉取） |
| **DB-GPT 资源** | 应用 App、数据源、知识空间（按模式显示） |
| **流式聊天** | SSE 解析，停止生成 |
| **附件（P2）** | 上传 CSV/Excel 路径，传给 DB-GPT 分析模式 |
| **设置** | 服务地址、API Key、代理、超时 |
| **日志审计（P3）** | 操作日志落本地文件（不含密钥） |

### 4.2 聊天模式（ChatMode）

| ChatMode | 用户可见名称 | 后端 | 必填参数 |
|----------|--------------|------|----------|
| `General` | 通用对话 | OpenAI 兼容 / DB-GPT 8100 | `model` |
| `DbGptApp` | 智能应用 | DB-GPT `chat_app` | `app_id` |
| `DbGptData` | 数据问数 | DB-GPT `chat_data` | `datasource_id` |
| `DbGptKnowledge` | 知识问答 | DB-GPT 知识/chat 对应 mode | `space_name` |
| `DomainCustom` | 领域-{名} | Custom Provider | `endpoint` 等 |

---

## 5. 界面设计

### 5.1 窗体结构

**MainForm**（单窗体多面板，或 MDI/侧边栏）

```
┌──────────┬──────────────────────────────────────────────────────┐
│ 会话列表  │ 工具栏                                                │
│ 200px    │ [领域 ▼] [模式 ▼] [模型 ▼] [数据源/App ▼] [⚙设置]   │
│          ├──────────────────────────────────────────────────────┤
│ + 新会话 │                                                      │
│ ──────── │              MessageHost (WebView2 Panel)            │
│ > 问数-1 │                                                      │
│   财务-2 │                                                      │
│          ├──────────────────────────────────────────────────────┤
│          │ [📎] 多行输入 TextBox                    [停止][发送] │
│          │ 状态栏: 已连接 DB-GPT | 领域: 财务分析 | tokens...    │
└──────────┴──────────────────────────────────────────────────────┘
```

### 5.2 控件与绑定

| 控件 | 绑定数据 |
|------|----------|
| 会话列表 `ListBox` | `IReadOnlyList<ChatSessionSummary>` |
| 领域 `ComboBox` | `domains.json` → `DomainProfile` |
| 模式 `ComboBox` | 随领域过滤可用 `ChatMode` |
| 资源 `ComboBox` | `DbGptApp` / `Datasource` 缓存列表 |
| 消息区 | `WebView2` 加载 `chat.html`，JS 桥接 `postMessage` |
| 输入框 | `Send` 快捷键 Ctrl+Enter |

### 5.3 消息渲染约定

每条消息 JSON 传给 WebView：

```json
{
  "id": "msg-uuid",
  "role": "user|assistant|system|tool",
  "content": "markdown string",
  "status": "streaming|done|error",
  "extras": {
    "sql": "SELECT ...",
    "agentPlan": [],
    "chartPath": null
  }
}
```

DB-GPT 返回中的 `agent-plans` / `agent-messages` 块解析后填入 `extras`，UI 折叠展示「执行计划」。

---

## 6. 领域配置（核心扩展点）

### 6.1 `domains.json` 结构

```json
{
  "version": 1,
  "defaults": {
    "dbgptBaseUrl": "http://127.0.0.1:5670",
    "timeoutSeconds": 120
  },
  "domains": [
    {
      "id": "general",
      "displayName": "通用助手",
      "icon": "chat",
      "chatMode": "General",
      "provider": "openai-compat",
      "model": "gpt-4o",
      "openAi": {
        "baseUrl": "http://127.0.0.1:8100/v1",
        "apiKeyRef": "openai-default"
      },
      "systemPrompt": "你是企业智能助手。"
    },
    {
      "id": "data-query",
      "displayName": "数据问数",
      "chatMode": "DbGptData",
      "provider": "dbgpt",
      "model": "gpt-4o",
      "dbgpt": {
        "chatMode": "chat_data",
        "datasourceId": "",
        "convUidOptional": true
      }
    },
    {
      "id": "finance",
      "displayName": "财务分析",
      "chatMode": "DbGptApp",
      "provider": "dbgpt",
      "model": "gpt-4o",
      "dbgpt": {
        "chatMode": "chat_app",
        "appId": "00000000-0000-0000-0000-000000000001"
      }
    },
    {
      "id": "legal",
      "displayName": "法务问答",
      "chatMode": "DomainCustom",
      "provider": "legal-service",
      "custom": {
        "endpoint": "https://internal/api/legal/chat",
        "apiKeyRef": "legal-key"
      }
    }
  ]
}
```

### 6.2 领域 → Provider 路由表

| provider 字段 | 实现类 | 说明 |
|---------------|--------|------|
| `dbgpt` | `DbGptChatProvider` | 5670，`/api/v2/chat/completions` |
| `openai-compat` | `OpenAiCompatibleProvider` | 标准 Chat Completions + SSE |
| `legal-service` | `LegalDomainProvider` | 实现 `IChatProvider`，内部 HTTP |

新增领域 = **编辑 JSON +（可选）新增 Provider 类**。

---

## 7. DB-GPT 集成规格

### 7.1 认证

```
Authorization: Bearer {DBGPT_API_KEY}
```

密钥存 `CredentialStore`（Windows DPAPI），配置里只写 `apiKeyRef` 名字。

### 7.2 流式对话请求（App 模式示例）

```http
POST /api/v2/chat/completions HTTP/1.1
Host: 127.0.0.1:5670
Authorization: Bearer dbgpt
Content-Type: application/json

{
  "model": "gpt-4o",
  "messages": "用户最后一句话或序列化历史",
  "chat_mode": "chat_app",
  "chat_param": "{app_id}",
  "stream": true,
  "conv_uid": "{可选，延续会话}"
}
```

> 具体字段以部署版本 OpenAPI (`/docs`) 为准；实现时封装 `DbGptRequestBuilder`。

### 7.3 管理类 API（启动/切换领域时缓存）

| 用途 | 方法 | 路径 |
|------|------|------|
| 应用列表 | GET | `/api/v2/serve/apps` |
| 应用详情 | GET | `/api/v2/serve/apps/{app_id}` |
| 数据源列表 | GET | `/api/v2/serve/datasources` |

缓存 5 分钟，设置页提供「刷新资源」。

### 7.4 SSE 解析规则

1. 按行读取，`data: ` 前缀；
2. `data: [DONE]` → 结束；
3. JSON 取 `choices[0].delta.content` 追加；
4. 含 `` ```agent-plans `` 的片段 → 解析为 `AgentPlan` 对象，不直接当正文（或双轨展示）；
5. 异常行记录 Debug 日志，不崩溃 UI。

### 7.5 上下文策略（已确认：仅本地 history）

| 项 | 做法 |
|----|------|
| 存储 | 全部消息在 SQLite `chat_message` |
| 请求组装 | `BuildHistory(sessionId, maxTurns, maxTokens)` → `messages[]` |
| DB-GPT | **不传** `conv_uid`；每请求自带 history |
| 自研 HTTP | 统一契约见 **§8.2** `CustomChatRequest` |
| 截断 | 超过 token 预算时：保留 system + 最近 N 轮（配置项） |

> `ChatSession` 表**不再使用** `dbgpt_conv_uid` 字段（V1 可省略该列）。

---

## 8. 自研 HTTP 领域集成（主路径）

### 8.0 设计原则

- 每个领域一个 **endpoint** + **apiKeyRef** + 可选 **request/response 适配器**（`adapter`: `default` | `openai` | `dbgpt-like`）；
- WinForms **不实现** 领域业务，只做：组 history、流式读响应、展示；
- 新增领域：**只改 `domains.json`**，若契约特殊再加一个薄 `XxxDomainProvider` 类。

### 8.1 推荐统一契约（公司与各团队对齐）

**请求** `POST {endpoint}`，`Content-Type: application/json`：

```json
{
  "sessionId": "local-guid",
  "domain": "legal",
  "message": "用户本轮输入",
  "stream": true,
  "messages": [
    { "role": "system", "content": "..." },
    { "role": "user", "content": "..." },
    { "role": "assistant", "content": "..." }
  ]
}
```

**响应（SSE）**：

```
data: {"delta":"你"}
data: {"delta":"好"}
data: [DONE]
```

或 OpenAI 兼容格式时设 `"adapter": "openai"`，复用 `OpenAiCompatibleProvider`。

**认证**：`Authorization: Bearer {key}` 或配置 `headerName: X-Api-Key`。

### 8.2 OpenAI 兼容（通用 / 备用）

```
POST {baseUrl}/chat/completions
{
  "model": "...",
  "messages": [ {"role":"system","content":"..."}, ... ],
  "stream": true
}
```

同一 `OpenAiCompatibleProvider`，通过配置连接：

- DB-GPT Model API `http://127.0.0.1:8100/v1`
- Azure OpenAI、DeepSeek、通义、Ollama 等

### 8.2 自研领域服务（模板）

请求：

```http
POST /api/chat/stream
{ "sessionId", "domain": "legal", "message", "history": [...] }
```

响应：SSE 或 NDJSON，Provider 统一转换为 `ChatChunk`。

### 8.3 领域与 DB-GPT 组合策略

| 策略 | 说明 |
|------|------|
| **仅 DB-GPT** | 领域 = 不同 App / 数据源 / 知识空间 |
| **仅云 API** | 领域 = 不同 system prompt + model |
| **混合** | 法务用自研 RAG，问数用 DB-GPT；WinForms 只切换 `DomainProfile` |

推荐：**能在 DB-GPT 配置的尽量放 DB-GPT**，减少 WinForms 业务逻辑。

---

## 9. 数据模型（本地）

### 9.1 SQLite 表

**chat_session**

| 列 | 类型 | 说明 |
|----|------|------|
| id | TEXT PK | GUID |
| title | TEXT | 列表显示，首条消息摘要 |
| domain_id | TEXT | 关联 domains.json |
| chat_mode | TEXT | 冗余快照 |
| model | TEXT | |
| resource_id | TEXT | app_id / datasource_id |
| dbgpt_conv_uid | TEXT | 可空 |
| created_at | INTEGER | Unix ms |
| updated_at | INTEGER | |

**chat_message**

| 列 | 类型 | 说明 |
|----|------|------|
| id | TEXT PK | |
| session_id | TEXT FK | |
| role | TEXT | user/assistant/system |
| content | TEXT | 完整 Markdown |
| extras_json | TEXT | SQL、Agent 计划等 |
| created_at | INTEGER | |

**app_setting**

| 列 | 类型 | 说明 |
|----|------|------|
| key | TEXT PK | 如 `dbgpt.baseUrl` |
| value | TEXT | |

### 9.2 实体（C#）

```csharp
public sealed class ChatSession
{
    public string Id { get; init; }
    public string Title { get; set; }
    public string DomainId { get; set; }
    public string ChatMode { get; set; }
    public string? Model { get; set; }
    public string? ResourceId { get; set; }
    public string? DbgptConvUid { get; set; }
}

public sealed class ChatMessage
{
    public string Id { get; init; }
    public string SessionId { get; init; }
    public string Role { get; init; }
    public string Content { get; set; }
    public string? ExtrasJson { get; set; }
}
```

---

## 10. 核心接口（C# 契约）

```csharp
public interface IChatProvider
{
    string ProviderId { get; }
    bool CanHandle(DomainProfile domain);
    IAsyncEnumerable<ChatChunk> StreamChatAsync(
        ChatContext context,
        CancellationToken cancellationToken);
}

public sealed class ChatContext
{
    public ChatSession Session { get; init; }
    public DomainProfile Domain { get; init; }
    public IReadOnlyList<ChatMessage> History { get; init; }
    public string UserMessage { get; init; }
}

public sealed class ChatChunk
{
    public string? TextDelta { get; init; }
    public bool IsCompleted { get; init; }
    public string? DbgptConvUid { get; init; }
    public ChatExtras? Extras { get; init; }
    public string? Error { get; init; }
}

public interface IConversationRepository
{
    Task<IReadOnlyList<ChatSession>> ListSessionsAsync();
    Task<ChatSession?> GetSessionAsync(string id);
    Task SaveSessionAsync(ChatSession session);
    Task<IReadOnlyList<ChatMessage>> GetMessagesAsync(string sessionId);
    Task AppendMessageAsync(ChatMessage message);
    Task UpdateMessageContentAsync(string messageId, string content);
}
```

```csharp
public sealed class ChatOrchestrator
{
    public async IAsyncEnumerable<ChatChunk> SendAsync(
        ChatContext context,
        [EnumeratorCancellation] CancellationToken ct)
    {
        var provider = _providers.First(p => p.CanHandle(context.Domain));
        await foreach (var chunk in provider.StreamChatAsync(context, ct))
            yield return chunk;
    }
}
```

---

## 11. 关键流程

### 11.1 发送消息

```
用户点击发送
  → 校验非空、非并发发送中
  → 持久化 User 消息
  → 创建空 Assistant 消息（status=streaming）
  → ChatOrchestrator.SendAsync
  →  foreach chunk: 更新 UI + 定时批量写库（每 500ms）
  → 完成：status=done，更新 Session.Title / DbgptConvUid
```

### 11.2 切换领域

```
用户切换领域 ComboBox
  → 若当前会话已有消息：提示「新建会话以切换领域」
  → 或自动 NewSession(domain)
  → 刷新资源下拉（Apps / Datasources）
```

### 11.3 停止生成

```
用户点击停止
  → CancellationTokenSource.Cancel
  → 当前 Assistant 消息标记为 done（内容为已生成部分）
```

---

## 12. 解决方案结构

```
DataChat.sln
├── DataChat.WinForms          # UI、Presenter、WebView2 资源
├── DataChat.Application       # Handlers、DTO
├── DataChat.Core              # 实体、接口、Orchestrator
├── DataChat.Infrastructure    # SQLite、HttpClient、Credential、配置
├── DataChat.Providers.Dbgpt
├── DataChat.Providers.OpenAi
├── DataChat.Providers.Custom  # 各领域适配
└── DataChat.Tests
```

**NuGet 建议**：`Microsoft.Extensions.Hosting`、`Microsoft.Data.Sqlite`、`System.Text.Json`、`Microsoft.Web.WebView2`。

---

## 13. 配置与密钥

### 13.1 `appsettings.json`（可提交）

```json
{
  "Dbgpt": {
    "BaseUrl": "http://127.0.0.1:5670",
    "ApiKeyRef": "dbgpt-main"
  },
  "Http": {
    "TimeoutSeconds": 120,
    "Proxy": null
  },
  "DomainsFile": "domains.json"
}
```

### 13.2 密钥（不可提交）

| apiKeyRef | 存储 |
|-----------|------|
| dbgpt-main | Credential Manager |
| openai-default | 同上 |

---

## 14. 异常与体验

| 场景 | 处理 |
|------|------|
| DB-GPT 未启动 | 发送前检测，提示启动命令 |
| 401 | 设置页检查 API Key |
| 超时 | 可重试，保留 partial 回复 |
| 流中断 | 标记消息 `error`，显示「连接中断」 |
| 大段 SQL | 代码块折叠 + 一键复制 |

---

## 15. 安全

- 业务库账号在 DB-GPT 侧配置只读；
- WinForms 不保存数据库密码，只保存 API Key 引用；
- 本地 SQLite 可选 DPAPI 加密（企业策略）；
- 日志脱敏：SQL 结果 >N 行截断。

---

## 16. 实施路线图

| 阶段 | 内容 | 验收 |
|------|------|------|
| **M1** | 解决方案骨架 + MainForm + SQLite 会话 | 能新建会话、本地存消息 |
| **M2** | OpenAiCompatibleProvider 流式 + WebView2 渲染 | 对 8100 或 Ollama 能聊 |
| **M3** | DbGptChatProvider + Apps 列表 + chat_app | 能选 App 并流式对话 |
| **M4** | DbGptData + 数据源下拉 + conv_uid | 能多轮问数 |
| **M5** | domains.json 多领域 + Custom Provider 模板 | 配置新增领域无需改 UI |
| **M6** | 设置、健康检查、日志、安装包 | 可交付内网试用 |

---

## 17. 已确认选项（2026-05-21）

见文档开头 **§0**。V2 再评估 SSO。

---

## 18. 参考链接

- DB-GPT 文档：https://docs.dbgpt.cn/docs/overview
- API（v2 chat）：`POST /api/v2/chat/completions`
- 仓库：https://github.com/eosphoros-ai/DB-GPT

---

*本文档为实施级设计，可直接按第 12、16 节拆任务开发。*
