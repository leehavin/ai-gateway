# DataChat Web 客户端

Vue 3 全屏对话界面：左侧**对话历史**、响应式窄屏抽屉、自研对话 UI（`src/ui`，参考 MateChat 布局思路）、对接 DataChat Gateway（`/v1/chat/stream` SSE）。

## 技术栈

| 层级 | 说明 |
|------|------|
| 框架 | Vue 3 + Vite + TypeScript |
| 对话 UI | `src/ui`（自研对话组件，Markdown / 代码高亮 / 思考块等） |
| Markdown | markdown-it + DOMPurify + highlight.js（流式节流、代码复制） |
| 图标 / 按钮 | vue-devui + DevUI Icons |
| 业务 | Gateway SSE、localStorage 会话、`@` / `/coze` 命令 |

## 运行

1. 启动 Gateway：`cd src/DataChat/DataChat.Gateway && dotnet run`（:5080）
2. 启动本前端：

```powershell
cd web/chat-ui
npm install
npm run dev
```

打开 http://127.0.0.1:5173

**局域网访问**（手机/同事电脑）：

```powershell
npm run dev
# Vite 会打印 Network: http://192.168.x.x:5173/
```

需保持 `VITE_GATEWAY_URL` 为空（走本机 Vite 代理）。若仍无法访问，在 Windows 防火墙中放行 **5173**（前端）与 **5080**（Gateway，可选）。

## 自研 UI（`src/ui`）

| 组件 | 说明 |
|------|------|
| `DcChatLayout` / `DcChatScroll` / `DcChatFooter` | 布局、滚底 |
| `DcMessageBubble` / `DcMarkdown` | 气泡与 Markdown |
| `DcThinkingBlock` / `DcCitationList` | 思考过程、参考来源 |
| `DcPromptChips` / `DcFileChipList` / `DcAttachmentTrigger` | 快捷问题、附件 |

## 品牌资源

| 文件 | 用途 |
|------|------|
| `public/logo.svg` | 欢迎页横版 Logo |
| `public/logo-icon.svg` | 顶栏、助手头像 |
| `public/user-avatar.svg` | 用户消息头像 |
| `src/constants/brand.ts` | 统一引用路径 |

## 输入交互（@ / 命令）

| 操作 | 说明 |
|------|------|
| 顶栏 **智能体** | 切换领域 / Bot（主入口） |
| 输入 `@` | 弹出智能体列表，选中后切换 `domain` |
| **Coze 领域** | 顶栏 **新会话**；输入 `/` 或 `/coze` 打开 Coze 菜单（上传、新建、切 Bot）；底栏 **/** 按钮同效 |
| 其他领域 | 输入 `/` 仅提示 `@ 切换智能体` |
| **附件** | 底栏上传，经 `POST /v1/files/upload` 随 stream 提交 |

键盘：`↑` `↓` 选择菜单项，`Enter` 确认，`Esc` 关闭。

## 消息与会话（迭代 1）

| 操作 | 说明 |
|------|------|
| 悬停消息 | **复制** / **编辑**（用户）/ **重新生成**（助手） |
| 编辑用户消息 | 内容回填输入框，发送后删除其后回复并重新流式生成 |
| 粘贴图片 | 输入框 `Ctrl+V` 粘贴截图/图片，走附件上传 |
| 侧栏 | **双击标题**或按钮 **重命名**；**置顶**（星标）；置顶会话排在最前 |
| 引用回复 | 助手消息 **引用**（优先选中文本，否则引用正文前 400 字） |
| 思考 / 来源 | SSE `thinking`、`citations`（演示模式 Mock 有样例） |
| 服务端会话 | `VITE_USE_SERVER_SESSIONS=true` 时走 `GET/PUT /v1/sessions` |

### 服务端会话

Gateway 配置 `EnableSessionApi`、`PersistSessions`（见 `appsettings.json`）。前端：

```env
VITE_USE_SERVER_SESSIONS=true
```

流式结束后 Gateway 会追加本轮 user/assistant 到 SQLite；前端 debounce 同步全量消息作为备份。置顶/自定义标题仍存本机 `localStorage` 叠加层。

## 迭代 3（反馈、导出、参数）

| 操作 | 说明 |
|------|------|
| 助手消息 **赞 / 踩** | 写入会话 `feedback` 字段，并 `POST /v1/feedback`（`data/feedback.jsonl`） |
| 顶栏 **导出** | Markdown / JSON / **PDF**（PDF 走浏览器打印→另存为 PDF，含 Markdown 渲染） |
| 顶栏 **生成参数** | Temperature / Top P / Max tokens，按智能体存 `localStorage`，随 stream 提交 `parameters` |
| 自定义领域 | `CustomDomainProvider` 在下游 JSON 中附带 `parameters` |

## CefSharp / WebView2 嵌入

```powershell
npm run build
```

将 `dist/` 作为本地静态服务或 WebView 根目录；生产构建时设置 `VITE_GATEWAY_URL`。

### 注入用户身份（WinForms / WebView2）

| 方式 | 说明 |
|------|------|
| 加载前脚本 | `__DATACHAT_TOKEN__` + `__DATACHAT_USER_ID__` + `__DATACHAT_USER_NAME__` |
| 运行时 postMessage | `{ type: 'datachat:setUser', userId, userName, token }` |
| 宿主换 Token | `POST /v1/auth/token`（`ServiceKey` + `userId`） |

仅 Token、无用户信息：`{ type: 'datachat:setToken', token: '...' }`（兼容旧集成）。

页面加载后会向父窗口发送 `{ type: 'datachat:ready' }`。

### 宿主带入案件文档并执行工作流（五书核稿等）

宿主先将案件文档登记到 Gateway（`POST /v1/files/register`，Header `X-Service-Key`）或 `POST /v1/files/upload`（用户 Token），再 `postMessage`：

```json
{
  "type": "datachat:runWorkflow",
  "domainId": "patent-oa",
  "workflowId": "7649037302673063970",
  "input": "请对本案五书进行核稿",
  "files": [{ "fileId": "...", "name": "环头.doc" }],
  "newSession": true
}
```

chat-ui 会切换智能体、新开会话并自动执行工作流；成功时父窗口收到 `{ type: 'datachat:runWorkflowAck', ok: true }`。

WinForms 可参考 `HostChatBridge.RunWorkflowFromLocalFilesAsync`（`DataChat.WinForms`）。

### 独立 Web 登录

配置 Gateway `Auth:SigningKey` 与 `Auth:Users` 后，设置 `.env`：

```
VITE_REQUIRE_LOGIN=true
```

浏览器将显示登录页，调用 `POST /v1/auth/login`。

## 附件上传

- 输入框 **附件** 或拖拽到对话区域
- `POST /v1/files/upload`（Vite 代理到 Gateway）
- 单文件最大 10MB，最多 5 个

## 配置（`.env.development`）

| 变量 | 说明 |
|------|------|
| `VITE_GATEWAY_URL` | 留空走 Vite 代理 |
| `VITE_DATACHAT_TOKEN` | Bearer Token |
| `VITE_DEFAULT_DOMAIN` | 默认领域 ID |

## 构建

```powershell
npm run build
```

产物在 `dist/`。
