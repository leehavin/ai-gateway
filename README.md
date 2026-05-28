# DataChat

企业级 AI 对话中台：统一接入 **DB-GPT**、**扣子（Coze）** 与公司自研 HTTP 领域服务，为 **WinForms 桌面客户端** 与 **Web 嵌入式侧栏** 提供同一套流式对话 API。

---

## 目录

- [产品定位](#产品定位)
- [架构概览](#架构概览)
- [已锁定选型](#已锁定选型)
- [环境要求](#环境要求)
- [快速开始](#快速开始)
- [Gateway 网关](#gateway-网关)
- [领域配置 domainsjson](#领域配置-domainsjson)
- [Provider 说明](#provider-说明)
- [Web 嵌入](#web-嵌入)
- [WinForms 桌面端](#winforms-桌面端)
- [API 参考](#api-参考)
- [生产部署建议](#生产部署建议)
- [常见问题](#常见问题)
- [项目结构](#项目结构)
- [延伸阅读](#延伸阅读)

---

## 产品定位

| 场景 | 说明 |
|------|------|
| 专利 / 业务系统 Web 页 | 引入 `datachat-embed.js`，右侧弹出 AI 侧栏 |
| 内部桌面工具 | WinForms + WebView2 对话界面 |
| 多 Agent 统一入口 | 一个 Gateway，按 `domain` 路由到不同后端 |
| 数据问数 | 对接 DB-GPT Text2SQL |
| 工作流 Bot | 对接 Coze 已发布智能体 |

**设计原则**：业务系统只认 DataChat Gateway；DB-GPT / Coze / 自研 API 的密钥与地址由 Gateway 保管，不暴露给浏览器。

---

## 架构概览

```
┌─────────────────┐     ┌─────────────────┐
│  WinForms 客户端 │     │  Web 业务系统     │
│  (WebView2)     │     │  + embed.js     │
└────────┬────────┘     └────────┬────────┘
         │                       │
         │  Bearer Token         │  Bearer Token
         └───────────┬───────────┘
                     ▼
         ┌───────────────────────┐
         │   DataChat.Gateway    │  :5080
         │   /v1/chat/stream     │
         └───────────┬───────────┘
                     │ domains.json 路由
       ┌─────────────┼─────────────┬──────────────┐
       ▼             ▼             ▼              ▼
  Custom HTTP    DB-GPT        Coze Bot      (Mock 演示)
  自研领域 API   :5670         api.coze.cn
```

**对话上下文**：历史消息由客户端（WinForms SQLite / Web localStorage）维护，Gateway 按请求体 `messages` 拼上下文转发，**不依赖** DB-GPT `conv_uid` 或 Coze 会话持久化到业务库（Coze 侧可选 `autoSaveHistory`）。

---

## 已锁定选型

| # | 决定 |
|---|------|
| 1 | DB-GPT 部署在公司统一服务器（`defaults.dbgptBaseUrl`） |
| 2 | UI：WinForms WebView2 + Web Shadow DOM 嵌入 |
| 3 | 上下文：**仅本地 history**，Gateway 拼消息后转发 |
| 4 | 领域：以 **自研 HTTP** 为主；DB-GPT / Coze 为辅 |
| 5 | 认证：Gateway **Bearer Token**；后端 **ApiKeys**（V1 无 AD/SSO） |

---

## 环境要求

| 组件 | 版本 |
|------|------|
| .NET SDK | 8.0+ |
| Windows（WinForms） | 10+，需 WebView2 Runtime |
| Node.js（可选） | 用于本地静态服务 demo 页面 |
| DB-GPT | 已部署可访问实例（示例 `http://42.193.110.76:5670`） |
| Coze | [扣子开放平台](https://www.coze.cn/) PAT/SAT + Bot ID |

---

## 快速开始

### 1. 克隆并构建

```powershell
cd E:\AIHUB
dotnet build src\DataChat.Gateway\DataChat.Gateway.csproj
```

### 2. 配置密钥与领域

编辑 `src\DataChat.Gateway\appsettings.json`：

```json
{
  "Gateway": {
    "UseMock": false,
    "ValidTokens": [ "demo-token" ]
  },
  "ApiKeys": {
    "dbgpt-main": "",
    "coze-main": "pat_你的扣子令牌"
  }
}
```

编辑 `src\DataChat.Gateway\domains.json`：将 `YOUR_COZE_BOT_ID` 换成真实 Bot ID；确认 `dbgptBaseUrl` 指向你的 DB-GPT。

### 3. 启动 Gateway

```powershell
cd src\DataChat.Gateway
dotnet run
```

默认地址：**http://127.0.0.1:5080**  
Swagger：**http://127.0.0.1:5080/swagger**

### 4. 验证健康检查

```powershell
curl.exe http://127.0.0.1:5080/v1/health
```

期望：`useMock: false`，`dbgptReachable: true`（DB-GPT 可达时）。

### 5. 启动 Web 演示

**终端 A** — Gateway（上一步已启动）

**方式 A — MateChat 全屏客户端（推荐）**

```powershell
cd E:\AIHUB\web\chat-ui
npm install
npm run dev
```

浏览器打开：**http://127.0.0.1:5173**（局域网可用本机 IP，如 `http://192.168.x.x:5173`，需 `npm run dev` 已配置 `host: true`）

环境变量见 `web/chat-ui/.env.development`（`VITE_DATACHAT_TOKEN`、`VITE_DEFAULT_DOMAIN`）。开发模式默认通过 Vite 代理 `/v1` → Gateway `:5080`。

**方式 B — 嵌入式侧栏演示**

```powershell
npx --yes http-server E:\AIHUB\web\embed -p 5500 -c-1
```

浏览器打开：**http://127.0.0.1:5500/demo.html**

> 请用 `http://` 访问 demo，不要用 `file://`，否则跨域请求 Gateway 会失败。

### 6. 启动 WinForms（可选）

```powershell
cd src\DataChat.WinForms
dotnet run
```

本地 SQLite：`%LocalAppData%\DataChat\chat.db`

---

## Gateway 网关

### appsettings.json

| 配置项 | 说明 |
|--------|------|
| `Gateway:UseMock` | `true` 时仅返回模拟回复，不调用外部服务 |
| `Gateway:ValidTokens` | 允许访问 Gateway 的 Bearer Token 列表 |
| `Gateway:AllowedOrigins` | CORS；开发可用 `*`，生产改为业务域名 |
| `Gateway:DomainsFile` | 领域配置文件名，默认 `domains.json` |
| `Gateway:TimeoutSeconds` | HTTP 超时（秒） |
| `Gateway:MaxMessageLength` | 单条用户消息最大长度 |
| `ApiKeys:*` | 后端服务密钥，通过 `apiKeyRef` 引用 |

**环境覆盖**：`appsettings.Development.json` 可覆盖 `UseMock` 等项。

### 认证

除 `GET /v1/health` 与 Swagger 外，所有 API 需请求头：

```http
Authorization: Bearer demo-token
```

Token 必须与 `Gateway:ValidTokens` 中某项一致。Web 嵌入通过 `getToken()` 注入，**不要**在页面写死后端 ApiKeys。

### Mock 模式

`UseMock: true` 适用于前端联调、无 DB-GPT/Coze 环境。返回固定演示文案，不消耗外部配额。

---

## 领域配置 domains.json

文件位置：

- Gateway：`src/DataChat.Gateway/domains.json`（运行时在输出目录复制）
- WinForms：`src/DataChat.WinForms/domains.json`

### 全局 defaults

| 字段 | 说明 |
|------|------|
| `dbgptBaseUrl` | DB-GPT 服务地址，如 `http://42.193.110.76:5670` |
| `cozeEndpoint` | Coze API 域名，国内 `api.coze.cn`，国际 `api.coze.com` |
| `timeoutSeconds` | 默认超时 |
| `maxHistoryTurns` | 转发时保留的最大对话轮数 |

### 领域通用字段

| 字段 | 必填 | 说明 |
|------|------|------|
| `id` | 是 | 领域 ID，Web `domain` 参数、API `domain` 字段 |
| `displayName` | 是 | 展示名称 |
| `chatMode` | 是 | `CozeAgent` / `DbGptApp` / `DbGptData` / `DomainCustom` 等 |
| `provider` | 是 | `coze` / `dbgpt` / `custom` |
| `model` | DB-GPT | 须与 DB-GPT 已注册模型一致，如 `deepseek-ai/DeepSeek-V3` |
| `systemPrompt` | 否 | 系统提示词（拼入本地 history） |
| `placeholder` | 否 | 输入框占位符 |
| `quickPrompts` | 否 | 快捷问题 chips |

---

## Provider 说明

### 1. Coze（扣子）— `provider: "coze"`

基于 NuGet **[CozeNet](https://www.nuget.org/packages/CozeNet/)**（社区 SDK，封装 Coze Open API v3）。

**配置示例：**

```json
{
  "id": "patent-coze",
  "displayName": "专利助手（Coze）",
  "chatMode": "CozeAgent",
  "provider": "coze",
  "coze": {
    "botId": "73428668123456",
    "apiKeyRef": "coze-main",
    "autoSaveHistory": true,
    "userIdPrefix": "patent"
  }
}
```

| coze 字段 | 说明 |
|-----------|------|
| `botId` | Bot 开发页 URL 中 `bot/` 后的数字 ID |
| `apiKeyRef` | 对应 `appsettings` → `ApiKeys` 中的 PAT/SAT |
| `endpoint` | 可选，覆盖 `defaults.cozeEndpoint` |
| `autoSaveHistory` | `true` 时 Coze 侧保存多轮；仅发当前用户句 |
| `userIdPrefix` | Coze `user_id` = `{prefix}:{sessionId}`，便于隔离 |
| `customVariables` | Bot 内 `{{变量}}` 传参 |

**获取 Bot ID 与 Token：**

1. 登录 [扣子](https://www.coze.cn/) 创建并发布 Bot  
2. 开发页 URL → 提取 Bot ID  
3. [开放平台 PAT](https://www.coze.cn/open/oauth/pats) → 写入 `ApiKeys:coze-main`

**新增多个 Coze Agent**：复制 `domains[]` 条目，改 `id`、`botId`、`apiKeyRef` 即可。

**探测 API：**

```powershell
curl.exe -H "Authorization: Bearer demo-token" http://127.0.0.1:5080/v1/coze/bots
curl.exe -H "Authorization: Bearer demo-token" http://127.0.0.1:5080/v1/coze/ping
```

---

### 2. DB-GPT — `provider: "dbgpt"`

对接公司统一部署的 [DB-GPT](http://docs.dbgpt.cn/)（端口 5670，`/api/v2`）。

**普通对话：**

```json
{
  "id": "patent",
  "provider": "dbgpt",
  "model": "deepseek-ai/DeepSeek-V3",
  "dbgpt": { "chatMode": "chat_normal" }
}
```

**数据问数（Text2SQL + 表格）：**

```json
{
  "id": "data-query",
  "provider": "dbgpt",
  "model": "deepseek-ai/DeepSeek-V3",
  "dbgpt": {
    "chatMode": "chat_data",
    "datasourceId": "Walmart_Sales"
  }
}
```

> `datasourceId` 填 DB-GPT 数据源**名称**（如 `Walmart_Sales`），不是数字 id。  
> 数据问数场景 Gateway 使用**非流式**向 DB-GPT 取 `<chart-view>` 结果，再格式化为 SQL + Markdown 表格推给前端。

**DB-GPT 资源代理：**

| Gateway | 说明 |
|---------|------|
| `GET /v1/dbgpt/ping` | 探测连通 |
| `GET /v1/dbgpt/datasources` | 数据源列表 |
| `GET /v1/dbgpt/apps` | 应用列表 |
| `GET /v1/dbgpt/knowledge/spaces` | 知识库空间 |
| `POST /v1/dbgpt/proxy/{path}` | 透传 `/api/v2/*` |

模型列表以 DB-GPT 实例为准（`/api/v2/serve/model/models`）。当前示例环境模型为 **`deepseek-ai/DeepSeek-V3`**。

---

### 3. 自研 HTTP — `provider: "custom"`

将请求 POST 到公司内部领域 API（与 Gateway SSE 契约一致）。

```json
{
  "id": "patent-api",
  "provider": "custom",
  "custom": {
    "endpoint": "https://patent-api.corp.example.com/v1/chat/stream",
    "apiKeyRef": "patent-key"
  }
}
```

适合专利业务、工单、审批等**自研智能体**，与 Coze/DB-GPT 并列挂载。

---

## Web 嵌入

### 文件

| 文件 | 说明 |
|------|------|
| `web/chat-ui/` | Vue 3 + MateChat 全屏客户端（见 [web/chat-ui/README.md](web/chat-ui/README.md)） |
| `web/embed/datachat-embed.js` | 单文件 Shadow DOM 侧栏，可 CDN 发布 |
| `web/embed/demo.html` | 专利系统风格演示页 |

### 最简集成

```html
<script src="/static/datachat-embed.js"></script>
<script>
  DataChatEmbed.init({
    gatewayUrl: 'http://127.0.0.1:5080',
    getToken: function () { return window.__AUTH_TOKEN__; },
    domain: 'patent-coze',
    userName: '李优优',
    autoOpen: true,
    loadConfigFromGateway: true
  });
</script>
```

### init 参数

| 参数 | 必填 | 说明 |
|------|------|------|
| `gatewayUrl` | 是 | Gateway 基地址 |
| `domain` | 是 | `domains.json` 中的 `id` |
| `getToken` / `token` | 建议 | 返回 Gateway Bearer Token |
| `userName` | 否 | 欢迎语称呼 |
| `quickPrompts` | 否 | 不传则从 `GET /v1/domains/{id}` 加载 |
| `loadConfigFromGateway` | 否 | 默认 `true`，自动拉 placeholder / quickPrompts |
| `width` | 否 | 侧栏宽度，默认 400 |
| `autoOpen` | 否 | 是否默认展开 |

### 会话存储

Web 端使用 `localStorage`，键名 `datachat:session:{domain}`，含 `sessionId` 与 `messages`。切换 `domain` 会切换独立会话。

### 渲染能力

- 普通 Markdown 文本流式输出  
- 代码块（```sql）  
- DB-GPT 数据问数返回的 **Markdown 表格**

---

## WinForms 桌面端

### 运行

```powershell
cd src\DataChat.Gateway    # 若领域走 Gateway
dotnet run

cd src\DataChat.WinForms
dotnet run
```

WinForms 可**直连** DB-GPT / Coze Provider（读本地 `domains.json`），也可将 `custom.endpoint` 指向 Gateway。

### 本地数据

| 路径 | 内容 |
|------|------|
| `%LocalAppData%\DataChat\chat.db` | SQLite 会话与消息 |
| DPAPI 加密密钥存储 | `FileApiKeyStore`（演示 seed：`dbgpt-main`、`gateway-token`） |

### UI

消息区为嵌入式 WebView2（`wwwroot/chat.html`），与 Gateway SSE 协议一致。

---

## API 参考

基地址：`http://127.0.0.1:5080`

### 健康检查（无需 Token）

```http
GET /v1/health
```

```json
{
  "status": "ok",
  "service": "DataChat.Gateway",
  "useMock": false,
  "dbgptReachable": true,
  "dbgptBaseUrl": "http://42.193.110.76:5670",
  "cozeReachable": true,
  "cozeEndpoint": "api.coze.cn",
  "cozeDomainCount": 1,
  "domainCount": 5
}
```

### 领域列表

```http
GET /v1/domains
GET /v1/domains/{domainId}
Authorization: Bearer demo-token
```

### 流式对话

```http
POST /v1/chat/stream
Authorization: Bearer demo-token
Content-Type: application/json
```

**请求体：**

```json
{
  "sessionId": "a1b2c3d4",
  "domain": "data-query",
  "message": "Walmart_Sales 表有多少行？",
  "stream": true,
  "messages": [
    { "role": "user", "content": "..." },
    { "role": "assistant", "content": "..." }
  ]
}
```

**响应：** `text/event-stream`

```
data: {"delta":"片段"}
data: {"delta":"..."}
data: [DONE]
```

错误：`data: {"error":"..."}`

### Coze

```http
GET /v1/coze/ping
GET /v1/coze/bots
```

### DB-GPT

见 [docs/API-Gateway.md](docs/API-Gateway.md)、[docs/DB-GPT-API-Coverage.md](docs/DB-GPT-API-Coverage.md)。

VS / Rider 可使用 `src/DataChat.Gateway/DataChat.Gateway.http` 调试。

---

## 生产部署建议

### Gateway

```powershell
$env:ASPNETCORE_ENVIRONMENT = "Production"
dotnet publish src/DataChat.Gateway -c Release -o ./publish/gateway
```

| 项 | 建议 |
|----|------|
| `UseMock` | `false` |
| `ValidTokens` | 每租户/每系统独立 Token，定期轮换 |
| `AllowedOrigins` | 仅业务系统域名，不用 `*` |
| `ApiKeys` | 环境变量或密钥管理服务注入，勿提交 Git |
| DB-GPT | 内网访问 + `api_keys`；勿公网裸奔 5670 |
| Coze | 企业版 SAT；按 Bot 配置权限 |
| HTTPS | 前置 Nginx / IIS 终止 TLS |
| Coze 会话映射 | 默认内存；多实例部署请实现 `ICozeConversationStore`（Redis） |

### Web 嵌入

将 `datachat-embed.js` 发布到 CDN 或静态资源目录；`gatewayUrl` 指向生产 Gateway HTTPS 地址。

### 端口占用

```powershell
netstat -ano | findstr ":5080"
taskkill /PID <PID> /F
```

---

## 常见问题

### Web 报 `[错误] Failed to fetch`

1. Gateway 是否已启动（5080）  
2. demo 是否通过 `http://127.0.0.1:5500` 打开（非 `file://`）  
3. Token 是否为 `ValidTokens` 中的值  
4. 若曾遇 CORS 问题，确认 Gateway 已启用 `UseCors` 且 OPTIONS 预检放行（当前版本已修复）

### DB-GPT 报模型不存在

检查 `domains.json` 中 `model` 是否与实例一致。查询：

```powershell
curl.exe http://42.193.110.76:5670/api/v2/serve/model/models
```

### 数据问数无表格

- 确认 `datasourceId` 为数据源**名称**  
- `chat_data` 场景依赖 DB-GPT 非流式 `<chart-view>` 响应；embed 会渲染 Markdown 表格  

### 对话不是流式

- 普通 DB-GPT / Coze 对话为真流式  
- **数据问数**先等 DB-GPT 算完再分块推送（含表格），体感可能略慢  

### Coze 报未配置密钥

在 `appsettings.json` 填写 `ApiKeys:coze-main`，并与 `domains.json` 中 `apiKeyRef` 一致。

### 5080 端口被占用

关闭旧 Gateway 进程后再 `dotnet run`（见上文端口命令）。

---

## 项目结构

```
AIHUB/
├── README.md                          # 本文档
├── docs/
│   ├── DESIGN-WinForms-DataChat.md    # WinForms 详细设计
│   ├── DESIGN-Web-Embed.md            # Web 嵌入设计
│   ├── API-Gateway.md                 # Gateway API 详表
│   └── DB-GPT-API-Coverage.md         # DB-GPT 对接矩阵
├── web/
│   ├── chat-ui/                       # Vue 3 + MateChat 全屏对话客户端
│   └── embed/
│       ├── datachat-embed.js          # 可嵌入侧栏组件
│       └── demo.html                  # 专利系统风格演示页
└── src/
    ├── DataChat.Core                  # 实体、接口、Orchestrator、配置模型
    ├── DataChat.Application           # 应用层 ChatService
    ├── DataChat.Infrastructure        # SQLite、domains 加载、FileApiKeyStore
    ├── DataChat.Providers             # Provider 实现
    │   ├── Custom/                    # 自研 HTTP
    │   ├── Dbgpt/                     # DB-GPT v2
    │   └── Coze/                      # 扣子 CozeNet SDK
    ├── DataChat.Gateway               # ASP.NET Core 网关 :5080
    └── DataChat.WinForms              # 桌面客户端 WebView2
```

### Provider 路由逻辑

`ChatOrchestrator` 按 `domain.provider` 选择首个匹配的 `IChatProvider`：

| provider | 类 | 用途 |
|----------|-----|------|
| `custom` | `CustomDomainProvider` | 自研 HTTP SSE |
| `dbgpt` | `DbGptChatProvider` | DB-GPT `/api/v2/chat/completions` |
| `coze` | `CozeChatProvider` | Coze `/v3/chat` 流式 |

---

## 延伸阅读

| 文档 | 内容 |
|------|------|
| [DESIGN-WinForms-DataChat.md](docs/DESIGN-WinForms-DataChat.md) | 桌面端架构、SQLite、里程碑 |
| [DESIGN-Web-Embed.md](docs/DESIGN-Web-Embed.md) | Shadow DOM、布局、SSE 协议 |
| [API-Gateway.md](docs/API-Gateway.md) | 完整 REST 列表 |
| [DB-GPT-API-Coverage.md](docs/DB-GPT-API-Coverage.md) | 已对接 / 未对接 API |
| [DB-GPT 官方文档](http://docs.dbgpt.cn/) | 部署与 API |
| [扣子 Open API](https://www.coze.cn/docs/developer_guides/coze_api_overview) | Coze 鉴权与 Bot 对话 |
| [CozeNet GitHub](https://github.com/xxzl0130/CozeNet) | C# SDK 来源 |

---

## 许可证

内部项目；第三方依赖见各 `.csproj`（含 CozeNet MIT、Microsoft 组件等）。
