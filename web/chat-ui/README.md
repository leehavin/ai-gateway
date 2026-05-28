# DataChat MateChat 客户端

基于 [MateChat vue-starter](https://matechat.gitcode.com/vue-starter/) 布局思路的 Vue 3 全屏对话界面：左侧**对话历史**（搜索、分组、删除）、**响应式**窄屏抽屉、消息区 **autoScroll**，对接 DataChat Gateway（`/v1/chat/stream` SSE）。

## 运行

1. 启动 Gateway：`cd src/DataChat.Gateway && dotnet run`（:5080）
2. 启动本前端：

```powershell
cd web/chat-ui
npm install
npm run dev
```

打开 http://127.0.0.1:5173

**局域网访问**（手机/同事电脑）：

```powershell
# 启动后 Vite 会打印 Network: http://192.168.x.x:5173/
npm run dev
```

需保持 `VITE_GATEWAY_URL` 为空（走本机 Vite 代理）。若仍无法访问，在 Windows 防火墙中放行 **5173**（前端）与 **5080**（Gateway，可选）。

## 品牌资源

| 文件 | 用途 |
|------|------|
| `public/logo.svg` | 欢迎页横版 Logo（由桌面 `logo.svg` 整理） |
| `public/logo-icon.svg` | 顶栏、侧栏、助手头像、浏览器图标 |
| `src/constants/brand.ts` | 统一引用路径 |

## 附件上传

- 输入框左侧 **+** 选择文件，或拖拽到对话区域
- 上传接口：`POST /v1/files/upload`（经 Vite 代理到 Gateway）
- 单文件最大 10MB，最多 5 个；`.txt/.md/.csv/.json` 等会提取正文拼入模型上下文
- 发送时随 `POST /v1/chat/stream` 的 `attachments` 字段一并提交

**注意**：更新 Gateway 代码后需重启 `dotnet run`，否则上传接口不可用。

## 配置（`.env.development`）

| 变量 | 说明 |
|------|------|
| `VITE_GATEWAY_URL` | 留空走 Vite 代理；生产填 Gateway HTTPS 根地址 |
| `VITE_DATACHAT_TOKEN` | Bearer Token，与 `Gateway:ValidTokens` 一致 |
| `VITE_DEFAULT_DOMAIN` | 默认领域 ID，如 `patent` |

## 构建

```powershell
npm run build
```

产物在 `dist/`，可部署到任意静态站点，并将 `VITE_GATEWAY_URL` 指向生产 Gateway。
