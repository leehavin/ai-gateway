# DataChat 数据库脚本

表结构由 SQL 脚本维护，**应用启动不会自动建表**。部署前请按数据库类型执行对应目录下的脚本。

## 目录

| 目录 | 说明 |
|------|------|
| [sqlserver/](sqlserver/) | Microsoft SQL Server |
| [mysql/](mysql/) | MySQL 8.x / MariaDB（utf8mb4） |
| [sqlite/](sqlite/) | 本地 SQLite 文件库 |

每个目录包含：

| 文件 | 说明 |
|------|------|
| `01-schema.sql` | 建表、索引、外键 |
| `02-seed-domains.sql` | 可选：初始全局配置与领域数据（与 `config/domains.json.example` 对齐） |

## SQL Server 示例

```sql
-- 在 SSMS 或 sqlcmd 中先建库（库名可自定）
CREATE DATABASE DataChat;
GO
```

```bash
sqlcmd -S 192.168.106.207,1433 -U sa -P "***" -d DataChat -i db/sqlserver/01-schema.sql
sqlcmd -S 192.168.106.207,1433 -U sa -P "***" -d DataChat -i db/sqlserver/02-seed-domains.sql
```

连接串写入 `appsettings.Development.local.json`（见 `*.example`），`Gateway:DbProvider` 设为 `SqlServer`，`DomainsSource` 设为 `Database`。

## MySQL 示例

```bash
mysql -h host -u user -p -e "CREATE DATABASE IF NOT EXISTS DataChat CHARACTER SET utf8mb4 COLLATE utf8mb4_unicode_ci;"
mysql -h host -u user -p DataChat < db/mysql/01-schema.sql
mysql -h host -u user -p DataChat < db/mysql/02-seed-domains.sql
```

> 当前 Gateway 运行时仅内置 **Sqlite / SqlServer** 驱动；MySQL 脚本供统一运维或后续接入使用，表结构与 SqlSugar 实体字段一致。

## SQLite 示例

```bash
mkdir -p data
sqlite3 data/datachat.db < db/sqlite/01-schema.sql
sqlite3 data/datachat.db < db/sqlite/02-seed-domains.sql
```

`Gateway:DatabasePath` 指向上述 `.db` 文件；领域表模式需 `DomainsSource=Database` 且使用 SqlSugar（与会话共用库时配置连接串或 SqlServer）。

## 表一览

| 表 | 用途 |
|----|------|
| `dc_global_defaults` | 全局默认（DB-GPT 地址、Coze 域名、超时等），主键固定 `id = 1` |
| `dc_domain` | 领域配置；`provider_options_json` 按 `provider` 存扩展 JSON |
| `chat_session` | 会话元数据（Gateway SqlSugar / WinForms 本地库） |
| `chat_message` | 会话消息，外键 `session_id` → `chat_session.id` |

修改领域数据后可 `POST /v1/domains/reload` 或重启 Gateway。详见 [docs/Domains-Database.md](../docs/Domains-Database.md)。
