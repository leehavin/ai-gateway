# AI Gateway

企业级 AI 中台 monorepo：**DataChat** 统一对话网关（DB-GPT / Coze / 自研 HTTP）+ **AIAdmin** 管理后台（用户、角色、菜单、工作流）。

| 子系统 | 技术栈 | 默认端口 |
|--------|--------|----------|
| DataChat Gateway | .NET 8 | `5080` |
| AIAdmin API | .NET 10 | `5062` |
| 对话前端 `web/chat-ui` | Vue 3 + Vite | `5173` |
| 管理前端 `web/ai-admin` | Vben Admin 5 + Element Plus | `5777` |

**设计原则**：业务系统只认 Gateway；DB-GPT / Coze 密钥由 Gateway 保管。管理后台与 Gateway 可共用同一 SQL Server 库（表前缀不同：`dc_*` / `sys_*` / `wf_*`）。

---

## 目录

- [架构概览](#架构概览)
- [仓库结构](#仓库结构)
- [环境要求](#环境要求)
- [快速开始](#快速开始)
- [数据库初始化](#数据库初始化)
- [DataChat Gateway](#datachat-gateway)
- [AIAdmin 管理后台](#aiadmin-管理后台)
- [Web 对话客户端](#web-对话客户端)
- [WinForms 桌面端](#winforms-桌面端)
- [API 参考](#api-参考)
- [生产部署](#生产部署)
- [本地配置与 Git](#本地配置与-git)
- [常见问题](#常见问题)
- [延伸阅读](#延伸阅读)

---

## 架构概览

```
┌──────────────────┐   ┌──────────────────┐   ┌──────────────────┐
│ web/chat-ui      │   │ WinForms         │   │ web/ai-admin     │
│ 对话 Web 客户端   │   │ WebView2 桌面端   │   │ 管理后台 (vben)   │
└────────┬─────────┘   └────────┬─────────┘   └────────┬─────────┘
         │ Bearer               │ Bearer               │ JWT
         ▼                      ▼                      ▼
┌─────────────────────┐                              ┌─────────────────────┐
│ DataChat.Gateway    │ :5080                        │ AIAdmin.Api.Host    │ :5062
│ /v1/chat/stream     │                              │ 用户/角色/菜单/工作流  │
└─────────┬───────────┘                              └──────────┬──────────┘
          │ dc_domain 路由                                       │ sys_* / wf_*
    ┌─────┼─────┬──────────┐                                     ▼
    ▼     ▼     ▼          ▼                              SQL Server（chat）
 Custom DB-GPT  Coze    Mock
```

**对话上下文**：历史由客户端维护（Web `localStorage` / WinForms SQLite），Gateway 按请求体 `messages` 拼上下文转发。

---

## 仓库结构

```
ai-gateway/
├── Gateway.slnx                 # 统一解决方案（VS / Rider 打开此文件）
├── README.md
├── .gitignore
├── db/
│   ├── chat/                    # Gateway 表结构（dc_*）与领域种子
│   │   ├── 01-schema.sql
│   │   ├── 02-seed-domains.sql
│   │   └── 03-add-user-id.sql
│   └── admin/                   # 管理后台表结构（sys_* / wf_*）
│       ├── admin-sqlserver.sql
│       └── seed-admin-user.sql  # 最小登录（无菜单）
├── docs/                        # 设计文档与 API 说明
├── src/
│   ├── DataChat/                # 对话网关（net8.0）
│   │   ├── DataChat.Core
│   │   ├── DataChat.Infrastructure
│   │   ├── DataChat.Providers   # DB-GPT / Coze / Custom
│   │   ├── DataChat.Gateway     # API 入口 :5080
│   │   └── DataChat.WinForms    # 桌面客户端
│   └── AIAdmin/                 # 管理后台 API（net10.0）
│       ├── AIAdmin.Api.Host     # API 入口 :5062
│       ├── AIAdmin.Application
│       ├── AIAdmin.SqlSugar
│       ├── AIAdmin.Workflow
│       ├── AIAdmin.Zero         # 数据种子 --seed
│       └── AIAdmin.sln          # 可单独打开 Admin 子解决方案
└── web/
    ├── chat-ui/                 # Vue 3 对话 UI
    ├── ai-admin/                # Vben monorepo，对接 Admin API（web-ele）
    └── ai-admin-uni/            # uni-app 移动端（可选）
```

---

## 环境要求

| 组件 | 版本 |
|------|------|
| .NET SDK | **8.0+**（DataChat）+ **10.0+**（AIAdmin） |
| SQL Server | Gateway / Admin 推荐共用实例 |
| Node.js | 18+（前端构建） |
| pnpm | `web/ai-admin` 使用（`corepack enable`） |
| Windows（WinForms） | 10+，需 WebView2 Runtime |
| DB-GPT | 已部署实例（示例 `http://42.193.110.76:5670`） |
| Coze | [扣子开放平台](https://www.coze.cn/) PAT/SAT + Bot ID |

---

## 快速开始

### 1. 克隆并构建

```powershell
git clone <repo-url> ai-gateway
cd ai-gateway
dotnet build Gateway.slnx
```

### 2. 本地配置（勿提交密钥）

本地密钥与连接串请写入 `appsettings.Development.local.json`（已在 `.gitignore` 中忽略），或直接改各项目 `appsettings.json`（**勿提交含密码的版本**）。

```powershell
# Admin API — 可复制示例后填写
copy src\AIAdmin\AIAdmin.Api.Host\appsettings.Development.local.json.example `
     src\AIAdmin\AIAdmin.Api.Host\appsettings.Development.local.json

# Admin 种子工具
copy src\AIAdmin\AIAdmin.Zero\appsettings.Development.local.json.example `
     src\AIAdmin\AIAdmin.Zero\appsettings.Development.local.json
```

Gateway 编辑 `src\DataChat/DataChat.Gateway/appsettings.json` 中的 `ValidTokens`、`ApiKeys` 与数据库连接。

### 3. 初始化数据库

见 [数据库初始化](#数据库初始化)。

### 4. 启动 Gateway（对话 API）

```powershell
cd src\DataChat\DataChat.Gateway
dotnet run
```

- API：**http://127.0.0.1:5080**
- Swagger：**http://127.0.0.1:5080/swagger**
- 健康检查：`curl http://127.0.0.1:5080/v1/health`

### 5. 启动对话前端

```powershell
cd web\chat-ui
npm install
npm run dev
```

浏览器打开 **http://127.0.0.1:5173**。环境变量见 `web/chat-ui/.env.development`（`VITE_DATACHAT_TOKEN`、`VITE_DEFAULT_DOMAIN`）。

### 6. 启动管理后台（可选）

**终端 A — Admin API**

```powershell
cd src\AIAdmin\AIAdmin.Api.Host
dotnet run
```

Swagger：**http://127.0.0.1:5062/swagger**

**终端 B — 管理前端（Element Plus 版）**

```powershell
cd web\ai-admin
corepack enable
pnpm install
pnpm dev:ele
```

浏览器打开 **http://localhost:5777**。Vite 已将 `/api` 代理到 `http://localhost:5062`。

默认账号（需先执行完整种子）：`admin` / `123456`

---

## 数据库初始化

Gateway 与 Admin **可共用同一数据库**（如 `chat`），表前缀互不冲突。

### Gateway（`dc_*`）

按顺序执行 `db/chat/` 下脚本（SQL Server 示例）：

```powershell
sqlcmd -S <host> -d chat -i db\chat\01-schema.sql
sqlcmd -S <host> -d chat -i db\chat\02-seed-domains.sql
sqlcmd -S <host> -d chat -i db\chat\03-add-user-id.sql
```

在 `dc_domain` 中把 Coze 的 Bot ID 换成真实值，并核对 `dc_global_defaults.dbgpt_base_url`。

### Admin（`sys_*` / `wf_*`）

```powershell
sqlcmd -S <host> -d chat -i db\admin\admin-sqlserver.sql
```

**完整后台数据**（组织、用户、角色、菜单、字典，推荐）：

```powershell
dotnet run --project src\AIAdmin\AIAdmin.Zero -- --seed
```

仅应急最小登录（**无左侧菜单**）：

```powershell
sqlcmd -S <host> -d chat -i db\admin\seed-admin-user.sql
```

---

## DataChat Gateway

### 核心配置（`appsettings.json`）

| 配置项 | 说明 |
|--------|------|
| `Gateway:UseMock` | `true` 时返回模拟回复，不调外部服务 |
| `Gateway:ValidTokens` | 允许访问的 Bearer Token 列表 |
| `Gateway:AllowedOrigins` | CORS；生产改为业务域名 |
| `Gateway:DomainsSource` | 推荐 `Database`（见 [docs/Domains-Database.md](docs/Domains-Database.md)） |
| `Gateway:DbProvider` | `Sqlite` 或 `SqlServer` |
| `ApiKeys:*` | 后端密钥，领域配置通过 `apiKeyRef` 引用 |

### 认证

除 `GET /v1/health` 与 Swagger 外，需请求头：

```http
Authorization: Bearer demo-token
```

### Provider 路由

`ChatOrchestrator` 按 `domain.provider` 选择 `IChatProvider`：

| provider | 实现 | 用途 |
|----------|------|------|
| `custom` | `CustomDomainProvider` | 自研 HTTP SSE |
| `dbgpt` | `DbGptChatProvider` | DB-GPT `/api/v2/chat/completions` |
| `coze` | `CozeChatProvider` | Coze `/v3/chat` 流式 |

### Coze 领域示例

```json
{
  "id": "patent-coze",
  "displayName": "专利助手（Coze）",
  "chatMode": "CozeAgent",
  "provider": "coze",
  "coze": {
    "botId": "73428668123456",
    "apiKeyRef": "coze-main",
    "autoSaveHistory": true
  }
}
```

在 `appsettings.json` 配置 `ApiKeys:coze-main`，Bot ID 从 [扣子](https://www.coze.cn/) 开发页 URL 获取。

### DB-GPT 数据问数

`chatMode: chat_data` 时 `datasourceId` 填 DB-GPT 数据源**名称**（非数字 id）。Gateway 非流式取 `<chart-view>` 后格式化为 Markdown 表格推送前端。

更多 Provider 与领域字段见下文 [延伸阅读](#延伸阅读) 中的文档。

---

## AIAdmin 管理后台

| 模块 | 说明 |
|------|------|
| `AIAdmin.Api.Host` | REST API、JWT、Swagger、SignalR |
| `AIAdmin.Application` | 用户、角色、组织、菜单、字典、通知等 |
| `AIAdmin.Workflow` | 工作流引擎（`wf_*` 表） |
| `AIAdmin.Zero` | 代码生成与 `--seed` 数据初始化 |

### 配置要点

| 配置项 | 说明 |
|--------|------|
| `ConnectionOptions` | SqlServer 连接串（与 Gateway 可同库） |
| `JwtOptions:SecretKey` | JWT 签名密钥，生产务必更换 |
| `App:CorsOrigins` | 默认含 `http://localhost:5777` |
| `AIOptions` | 可选；未配置时登录 AI 助手返回友好提示 |

### 前端说明

- 对接 API 的版本：**`web/ai-admin/apps/web-ele`**（Element Plus）
- `web-antd`、`web-naive` 为 vben 模板示例，一般不用
- `web/ai-admin-uni` 为 uni-app 移动端，按需启用
- 工作流 UI 尚未完全迁入 vben，可按需从历史提交参考

---

## Web 对话客户端

路径：`web/chat-ui/` — Vue 3 自研对话 UI（`@` 选智能体、`/coze`、附件、Markdown 流式渲染）。

| 集成方式 | 说明 |
|----------|------|
| 浏览器开发 | `npm run dev`，Vite 代理 `/v1` → Gateway `:5080` |
| 生产 / 嵌入 | `npm run build`，部署 `dist/` 或嵌入 WebView2 / CefSharp |
| 会话存储 | `localStorage`，按 `domain` 隔离 |

详见 [web/chat-ui/README.md](web/chat-ui/README.md)。

---

## WinForms 桌面端

```powershell
cd src\DataChat\DataChat.WinForms
dotnet run
```

| 项 | 说明 |
|----|------|
| 本地库 | `%LocalAppData%\DataChat\chat.db`（SQLite 会话） |
| UI | 内嵌 WebView2（`wwwroot/chat.html`），协议与 Gateway SSE 一致 |
| 领域 | 与 Gateway 共用数据库领域配置（`DomainsSource=Database`） |

详见 [docs/DESIGN-WinForms-DataChat.md](docs/DESIGN-WinForms-DataChat.md)。

---

## API 参考

基地址：`http://127.0.0.1:5080`

| 方法 | 路径 | 说明 |
|------|------|------|
| GET | `/v1/health` | 健康检查（无需 Token） |
| GET | `/v1/domains` | 领域列表 |
| POST | `/v1/domains/reload` | 重载领域配置 |
| POST | `/v1/chat/stream` | 流式对话（SSE） |
| GET | `/v1/coze/ping` | Coze 连通性 |
| GET | `/v1/coze/bots` | Coze Bot 列表 |

**流式对话请求体：**

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

**SSE 响应：** `data: {"delta":"片段"}` … `data: [DONE]`

完整 API 列表见 [docs/API-Gateway.md](docs/API-Gateway.md)。VS 可用 `src/DataChat/DataChat.Gateway/DataChat.Gateway.http` 调试。

---

## 生产部署

### Gateway

```powershell
$env:ASPNETCORE_ENVIRONMENT = "Production"
dotnet publish src/DataChat/DataChat.Gateway -c Release -o ./publish/gateway
```

### Admin API

```powershell
dotnet publish src/AIAdmin/AIAdmin.Api.Host -c Release -o ./publish/admin-api
```

### 前端

| 项目 | 命令 | 产物 |
|------|------|------|
| chat-ui | `cd web/chat-ui && npm run build` | `web/chat-ui/dist/` |
| ai-admin | `cd web/ai-admin && pnpm build:ele` | `web/ai-admin/apps/web-ele/dist/` |

### 生产检查清单

| 项 | 建议 |
|----|------|
| `UseMock` | `false` |
| `ValidTokens` / JWT Secret | 独立轮换，勿用默认值 |
| `AllowedOrigins` / CORS | 仅业务域名 |
| `ApiKeys`、数据库密码 | 环境变量或密钥管理，勿提交 Git |
| HTTPS | Nginx / IIS 终止 TLS |
| Coze 多实例 | 实现 `ICozeConversationStore`（如 Redis） |

### 端口占用

```powershell
netstat -ano | findstr ":5080"
netstat -ano | findstr ":5062"
taskkill /PID <PID> /F
```

---

## 本地配置与 Git

根目录 `.gitignore` 已覆盖 `src/` 与 `web/` 常见产物，重点忽略：

| 类型 | 规则 |
|------|------|
| 构建产物 | `bin/`、`obj/`、`dist/`、`node_modules/` |
| 本地配置 | `appsettings.Development.json`、`* .local.json`、`.env.local` |
| 密钥文件 | `*.pfx`、`*.key`、`.env` |
| 运行时数据 | `*.db`、`logs/`、`uploads/` |
| IDE | `.vs/`、`.idea/`、`.cursor/` |

**会提交的文件**：`appsettings.json`（模板）、`*.example`、`pnpm-lock.yaml`、`.env.development`（vben 默认本地地址）。

---

## 常见问题

### Web 对话报 `Failed to fetch`

1. Gateway 是否已启动（5080）
2. Token 是否在 `ValidTokens` 中
3. 是否通过 `http://127.0.0.1:5173` 访问（非 `file://`）
4. Gateway `AllowedOrigins` 是否包含前端来源

### Admin 登录后无菜单

执行完整种子：`dotnet run --project src/AIAdmin/AIAdmin.Zero -- --seed`（不要用仅 `seed-admin-user.sql`）。

### Admin 登录报 Kernel / DI 错误

确认 `AIOptions` 未配置时仍可登录；AI 助手需配置 `AIOptions` 后才可用。

### DB-GPT 模型不存在

核对 `dc_domain.model` 与 DB-GPT 实例一致：

```powershell
curl http://<dbgpt-host>:5670/api/v2/serve/model/models
```

### Coze 未配置密钥

填写 `ApiKeys:coze-main`，并与领域 `apiKeyRef` 一致。

### 5080 / 5062 端口被占用

关闭旧进程后重新 `dotnet run`（见 [生产部署](#生产部署) 端口命令）。

---

## 延伸阅读

| 文档 | 内容 |
|------|------|
| [web/chat-ui/README.md](web/chat-ui/README.md) | 对话 UI、`@` / `/coze`、CefSharp |
| [docs/API-Gateway.md](docs/API-Gateway.md) | Gateway REST 完整列表 |
| [docs/Domains-Database.md](docs/Domains-Database.md) | 领域库表、SqlSugar |
| [docs/DB-GPT-API-Coverage.md](docs/DB-GPT-API-Coverage.md) | DB-GPT 对接矩阵 |
| [docs/DESIGN-WinForms-DataChat.md](docs/DESIGN-WinForms-DataChat.md) | WinForms 架构 |
| [DB-GPT 官方文档](http://docs.dbgpt.cn/) | 部署与 API |
| [扣子 Open API](https://www.coze.cn/docs/developer_guides/coze_api_overview) | Coze 鉴权与 Bot |

---

## 许可证

内部项目；第三方依赖见各 `.csproj`（含 CozeNet MIT、Vben Admin MIT 等）。
