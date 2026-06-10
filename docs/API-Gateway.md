# DataChat Gateway API

基地址示例：`http://127.0.0.1:5080`

认证：除健康检查与登录入口外，请求头 `Authorization: Bearer {token}`。

- **用户会话 Token**：`POST /v1/auth/login` 或宿主 `POST /v1/auth/token` 签发（HMAC，需 `Gateway:Auth:SigningKey`）。
- **静态 Token**：`Gateway:ValidTokens`（共享模式，会话不按用户隔离）。
- **WinForms 嵌入**：宿主用 `ServiceKey` 换用户 Token，再 `postMessage` 注入 chat-ui。
- **案件文档带入**：宿主 `POST /v1/files/register` 登记本地五书路径 → `postMessage` `datachat:runWorkflow` 触发 PatentOA 等工作流。

## 认证

| 方法 | 路径 | 说明 |
|------|------|------|
| POST | `/v1/auth/login` | 独立 Web：`{ username, password }` → 用户 Token |
| POST | `/v1/auth/token` | 宿主嵌入：`ServiceKey` + `{ userId, userName }` → 用户 Token |
| GET | `/v1/auth/me` | 当前 Token 对应用户（需 Bearer） |

## 健康检查

`GET /v1/health` — 无需 Token

```json
{
  "status": "ok",
  "service": "DataChat.Gateway",
  "useMock": true,
  "dbgptReachable": false,
  "domainCount": 3
}
```

## 领域配置

`GET /v1/domains` — 嵌入组件可拉取快捷问题与占位符

`POST /v1/domains/reload` — 改库后热重载（需 Token）

`GET /v1/coze/bots` — 已配置的 Coze 领域 Bot 列表

## 流式对话

`POST /v1/chat/stream`  
`Content-Type: application/json`  
响应：`text/event-stream`

请求体：

```json
{
  "sessionId": "uuid",
  "domain": "patent",
  "message": "用户输入",
  "stream": true,
  "messages": [
    { "role": "user", "content": "..." },
    { "role": "assistant", "content": "..." }
  ],
  "parameters": {
    "temperature": 0.7,
    "topP": 0.9,
    "maxTokens": 4096
  }
}
```

可选 `parameters` 会映射到 `ChatGenerationParameters`；自定义 HTTP 领域在请求体中收到 `parameters` 对象（DB-GPT / Coze 路径是否生效取决于对应 Provider）。

SSE 事件（推荐带 `type` 字段；仍兼容 `{"delta":"..."}`）：

- `data: {"type":"thinking","delta":"思考片段"}`
- `data: {"type":"delta","delta":"正文片段"}`
- `data: {"type":"citations","citations":[{"title":"...","url":"...","snippet":"..."}]}`
- `data: {"error":"..."}`
- `data: [DONE]`

流式结束后若 `Gateway:PersistSessions=true`，会将本轮 user/assistant 写入 SQLite。

## 会话同步（Web 可选）

| 方法 | 路径 | 说明 |
|------|------|------|
| GET | `/v1/sessions?domain={id}` | 会话列表 |
| GET | `/v1/sessions/{id}` | 会话详情含消息 |
| POST | `/v1/sessions` | 创建空会话 |
| PUT | `/v1/sessions/{id}` | 全量同步消息 |
| DELETE | `/v1/sessions/{id}` | 删除 |

需 `Gateway:EnableSessionApi=true`，数据文件见 `Gateway:DatabasePath`（默认 `data/datachat.db`）。

## 消息反馈

`POST /v1/feedback`

```json
{
  "sessionId": "uuid",
  "messageId": "uuid",
  "domain": "patent",
  "rating": "up",
  "comment": "可选备注"
}
```

`rating` 为 `up` 或 `down`。记录追加到 `data/feedback.jsonl`（路径相对 Gateway 工作目录）。

DB-GPT 连通性由 `GET /v1/health` 的 `dbgptReachable` 探测；对话走 `POST /v1/chat/stream`（`provider=dbgpt`）。管理类 serve API 请直连 DB-GPT 或后续管理后台。

## 配置

| 配置项 | 说明 |
|--------|------|
| `Gateway:UseMock` | `true` 演示模式，不调用外部服务 |
| `Gateway:ValidTokens` | 静态 Bearer Token（共享模式） |
| `Gateway:Auth:SigningKey` | 用户会话 Token 签名密钥 |
| `Gateway:Auth:ServiceKey` | WinForms 宿主换 Token 的服务密钥 |
| `Gateway:Auth:Users` | 本地用户表（可替换为其他 `IHostAuthProvider`） |
| `Gateway:AllowedOrigins` | CORS，生产改为业务域名 |
| `ApiKeys:dbgpt-main` | DB-GPT 服务端密钥 |
| `ApiKeys:patent-key` | 专利领域自研服务密钥 |
| `domains.json` | 领域路由（`DomainsSource=File` 时） |
| `DomainsSource` | `File` 或 `Database`（Sqlite 表 `dc_domain`） |
| `Gateway:DatabasePath` | 会话 + 领域配置共用 SQLite 路径 |

领域数据库说明见 [Domains-Database.md](Domains-Database.md).

## 文件（工作流 doc 等）

| 方法 | 路径 | 说明 |
|------|------|------|
| POST | `/v1/files/upload` | 浏览器 / 宿主 multipart 上传，需用户 Bearer Token |
| POST | `/v1/files/register` | 宿主登记**本机已有文件路径**，需 `X-Service-Key` 或 body `serviceKey` |
| GET | `/v1/files/{fileId}` | 下载已登记文件 |

`register` 请求体：`{ "path": "D:\\cases\\环头.doc" }` → 返回 `{ fileId, name, url, ... }`。

chat-ui 嵌入协议（`postMessage`）：

```json
{
  "type": "datachat:runWorkflow",
  "domainId": "patent-oa",
  "workflowId": "7649037302673063970",
  "input": "请对本案五书进行核稿",
  "files": [{ "fileId": "...", "name": "环头.doc" }]
}
```

| `Gateway:PersistSessions` | 流式后写入会话 |
| `Gateway:EnableSessionApi` | 是否开放 `/v1/sessions` |

生产部署：设置 `ASPNETCORE_ENVIRONMENT=Production`，`UseMock=false`，填写真实 `ApiKeys`。
