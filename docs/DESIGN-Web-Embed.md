# DataChat Web 嵌入式侧栏 — 设计说明

> 与 WinForms 共用同一套聊天网关与领域契约；业务系统只需引入 **一个 JS** 即可显示右侧 AI 对话框（见产品截图红框）。

---

## 1. 引入方式（业务方）

```html
<!-- 公司 CDN 或静态资源 -->
<script src="https://static.corp.example.com/datachat/v1/datachat-embed.js"></script>
<script>
  DataChatEmbed.init({
    gatewayUrl: 'https://api.corp.example.com/datachat',
    getToken: function () { return window.__AUTH_TOKEN__; },
    domain: 'patent',
    userName: '李优优',
    quickPrompts: [
      '今年新申请案件授权率',
      '智能客服',
      '客户回款情况',
      '专利业务知识库',
      '查看今年通过驳回复审，获得授权的案件'
    ],
    placeholder: '@智能体，可对他说今天新登记的官文有多少？'
  });
</script>
```

| 参数 | 必填 | 说明 |
|------|------|------|
| `gatewayUrl` | 是 | **聊天网关**根地址（禁止浏览器直连 DB-GPT 带主密钥） |
| `getToken` | 是 | 返回业务系统登录态 JWT / API Key，由网关校验 |
| `domain` | 是 | 领域 id，对应网关 `domains` 配置 |
| `userName` | 否 | 问候语「{name}，下午好！」 |
| `quickPrompts` | 否 | 快捷问题 chips |
| `placeholder` | 否 | 输入框占位 |
| `zIndex` | 否 | 默认 99999 |
| `locale` | 否 | `zh-CN` |

---

## 2. 为何必须经网关（安全）

```
业务 Web 页 ──► datachat-embed.js ──► DataChat Gateway ──► 自研领域 / DB-GPT
                     │                      │
                     │ 仅带用户 Token       │ 服务端持 API Key
                     └ 不暴露 dbgpt-main    └ 拼 history、限流、审计
```

- 浏览器 **不得** 写死 DB-GPT / 领域主密钥。
- WinForms 用 DPAPI 存 Key；Web 用业务 **Token + 网关代签**。

---

## 3. 网关 API（与 WinForms Custom 契约一致）

### 3.1 流式对话

```http
POST {gatewayUrl}/v1/chat/stream
Authorization: Bearer {用户Token}
Content-Type: application/json

{
  "sessionId": "uuid",
  "domain": "patent",
  "message": "用户输入",
  "stream": true,
  "messages": [
    { "role": "system", "content": "..." },
    { "role": "user", "content": "..." },
    { "role": "assistant", "content": "..." }
  ]
}
```

响应：`text/event-stream`，`data: {"delta":"..."}` 或 OpenAI chunk，结束 `data: [DONE]`。

### 3.2 会话（可选，V1 本地为主）

V1 会话与 history **存浏览器 localStorage**（与 WinForms SQLite 策略一致：客户端拼 history）。  
V2 可增加 `GET/POST /v1/sessions` 服务端持久化。

### 3.3 健康检查

```http
GET {gatewayUrl}/v1/health
```

---

## 4. 组件 UI（对齐截图）

| 区域 | 行为 |
|------|------|
| 顶栏 | 新建会话、展开/收起、关闭（销毁或隐藏） |
| 欢迎区 | 渐变球体 + 「{userName}，{时段}好！」 |
| 快捷 chips | 点击填入输入框并发送 |
| 输入区 | 多行、附件按钮（V1 占位）、发送 |
| 消息列表 | 用户右对齐 / 助手左对齐，流式打字 |
| 底栏 | 「AI生成内容可能有误… \| 隐私」 |

**样式隔离**：Shadow DOM + 内置 CSS，避免污染专利系统等宿主页面。

**挂载**：默认 `position: fixed; right: 0; top: 0; height: 100%`，宽 400px；可配置 `container` 挂到指定 DOM。

---

## 5. 与 WinForms 代码复用

| 层 | WinForms | Web Embed |
|----|----------|-----------|
| UI | WinForms + WebView2 | `datachat-embed.js` |
| 会话 | SQLite | `localStorage` |
| history 组装 | `ChatHistoryBuilder` (C#) | JS 同等逻辑 |
| 后端 | Provider 直连（桌面可信） | **仅经 Gateway** |
| 领域配置 | `domains.json` | 网关同一份配置 |

---

## 6. 部署清单

| 资产 | 路径 | 说明 |
|------|------|------|
| 嵌入脚本 | `web/embed/datachat-embed.js` | 拷贝到公司 CDN |
| 演示页 | `web/embed/demo.html` | 联调参考 |
| 网关服务 | `src/DataChat.Gateway` | ASP.NET 反向代理 + Key |
| 设计 | 本文档 + `DESIGN-WinForms-DataChat.md` §0 | |

---

## 7. 实施阶段

| 阶段 | 内容 |
|------|------|
| W1 | `datachat-embed.js` + demo.html + localStorage |
| W2 | `DataChat.Gateway` 代理到自研领域 SSE |
| W3 | 网关接 DB-GPT、附件上传 |
| W4 | `@智能体` 提及、隐私页链接、埋点 |
