# DataChat Gateway API

基地址示例：`http://127.0.0.1:5080`

认证：除健康检查外，请求头 `Authorization: Bearer {token}`（与 `appsettings` 中 `Gateway:ValidTokens` 匹配）。

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

`GET /v1/domains/{domainId}`

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
  ]
}
```

SSE 事件：

- `data: {"delta":"片段"}`
- `data: {"error":"..."}`
- `data: [DONE]`

## DB-GPT 资源代理（对齐官方 v2）

| 方法 | Gateway 路径 | 官方路径 |
|------|--------------|----------|
| GET | `/v1/dbgpt/ping` | 探测 `/docs` |
| GET | `/v1/dbgpt/apps` | `GET /api/v2/serve/apps` |
| GET | `/v1/dbgpt/apps/{id}` | `GET /api/v2/serve/apps/{id}` |
| GET | `/v1/dbgpt/datasources` | `GET /api/v2/serve/datasources` |
| GET | `/v1/dbgpt/datasources/{id}` | `GET /api/v2/serve/datasources/{id}` |
| GET | `/v1/dbgpt/knowledge/spaces` | `GET /api/v2/serve/knowledge/spaces` |
| POST/PUT/DELETE | `/v1/dbgpt/proxy/{path}` | 透传 `/api/v2/{path}` |

与官方全量 API 的对照表见 [DB-GPT-API-Coverage.md](DB-GPT-API-Coverage.md)。

## 配置

| 配置项 | 说明 |
|--------|------|
| `Gateway:UseMock` | `true` 演示模式，不调用外部服务 |
| `Gateway:ValidTokens` | 允许的 Bearer Token 列表 |
| `Gateway:AllowedOrigins` | CORS，生产改为业务域名 |
| `ApiKeys:dbgpt-main` | DB-GPT 服务端密钥 |
| `ApiKeys:patent-key` | 专利领域自研服务密钥 |
| `domains.json` | 领域路由与 endpoint |

生产部署：设置 `ASPNETCORE_ENVIRONMENT=Production`，`UseMock=false`，填写真实 `ApiKeys`。
