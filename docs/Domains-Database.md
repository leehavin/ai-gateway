# 领域配置（数据库 / SqlSugar）

Gateway 与 WinForms 支持从数据库表加载领域（与 `domains.json` 等价），便于扩展 **DB-GPT / Coze / 自研 HTTP / OpenAI 兼容** 等领域。

**表结构不由应用自动创建**，请先执行仓库 [`db/`](../db/) 下对应数据库类型的 SQL 脚本。

## SQL Server

1. 建库（示例名 `DataChat`），执行脚本：

```bash
sqlcmd -S host,1433 -U user -P "***" -d DataChat -i db/sqlserver/01-schema.sql
sqlcmd -S host,1433 -U user -P "***" -d DataChat -i db/sqlserver/02-seed-domains.sql
```

2. 复制 `appsettings.Development.local.json.example` 为 `appsettings.Development.local.json`（已 `.gitignore`），填写连接串：

```json
{
  "ConnectionStrings": {
    "DataChat": "Server=host,1433;Database=DataChat;User Id=***;Password=***;TrustServerCertificate=True;Encrypt=False;"
  },
  "Gateway": {
    "DbProvider": "SqlServer",
    "DomainsSource": "Database",
    "SeedDomainsFromFileWhenEmpty": false
  }
}
```

建议用 `02-seed-domains.sql` 初始化领域数据，将 `SeedDomainsFromFileWhenEmpty` 设为 `false`。若仍为 `true`，仅在 `dc_domain` 为空时从 `domains.json` 导入（不建表）。

会话与领域可共用同一 SQL Server 库。

## MySQL

脚本位于 `db/mysql/`。当前运行时仅内置 Sqlite / SqlServer 驱动；MySQL 脚本供统一运维或后续接入，表结构与实体一致。

## SQLite

```bash
mkdir -p data
sqlite3 data/datachat.db < db/sqlite/01-schema.sql
sqlite3 data/datachat.db < db/sqlite/02-seed-domains.sql
```

## 配置

`appsettings.json` → `Gateway` 节点：

| 配置项 | 说明 |
|--------|------|
| `DomainsSource` | `File`（默认）或 `Database` |
| `DomainsFile` | 文件模式路径；`SeedDomainsFromFileWhenEmpty=true` 时在**表为空**作种子 |
| `DbProvider` | `Sqlite`（默认）或 `SqlServer` |
| `ConnectionStrings:DataChat` | SQL Server 连接串 |
| `DatabasePath` | SQLite 文件路径，默认 `data/datachat.db` |
| `SeedDomainsFromFileWhenEmpty` | 表已存在且为空时，是否从 `domains.json` 写入（不建表） |

## 表结构

| 表 | 说明 |
|----|------|
| `dc_global_defaults` | 全局默认（DB-GPT 地址、Coze 域名、超时、历史轮数） |
| `dc_domain` | 领域行；`provider` + `provider_options_json` 存扩展配置 |
| `chat_session` | 会话元数据 |
| `chat_message` | 消息，外键 `session_id` |

DDL 详见 [`db/README.md`](../db/README.md)。

`provider_options_json` 按 `provider` 反序列化：

| provider | JSON 对应类型 |
|----------|----------------|
| `coze` | `CozeDomainOptions` |
| `dbgpt` | `DbgptDomainOptions` |
| `custom` | `CustomDomainOptions` |
| `openai` | `OpenAiDomainOptions` |

## API

| 方法 | 路径 | 说明 |
|------|------|------|
| GET | `/v1/domains` | 列表（来自 `IDomainCatalog.Current`） |
| POST | `/v1/domains/reload` | 改库后热重载（需 Bearer Token） |

## 运维示例

1. 执行 `db/<engine>/01-schema.sql` 与可选 `02-seed-domains.sql`。
2. `DomainsSource=Database`，启动 Gateway。
3. 用 SQL 修改 `dc_domain`，`POST /v1/domains/reload` 或重启。
4. 前端刷新拉 `/v1/domains`。

仍可用 `DomainsSource=File` 完全回到 JSON 模式。
