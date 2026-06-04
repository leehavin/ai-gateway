/*
  DataChat — SQLite 表结构
  执行: sqlite3 data/datachat.db < 01-schema.sql
*/

PRAGMA foreign_keys = ON;

DROP TABLE IF EXISTS chat_message;
DROP TABLE IF EXISTS chat_session;
DROP TABLE IF EXISTS dc_domain;
DROP TABLE IF EXISTS dc_global_defaults;

CREATE TABLE dc_global_defaults (
    id                  INTEGER NOT NULL PRIMARY KEY,
    version             INTEGER NOT NULL DEFAULT 1,
    dbgpt_base_url      TEXT    NOT NULL,
    coze_endpoint       TEXT    NOT NULL,
    timeout_seconds     INTEGER NOT NULL DEFAULT 120,
    max_history_turns   INTEGER NOT NULL DEFAULT 20,
    updated_at          INTEGER NOT NULL
);

CREATE TABLE dc_domain (
    id                      TEXT    NOT NULL PRIMARY KEY,
    display_name            TEXT    NOT NULL,
    chat_mode               TEXT    NOT NULL,
    provider                TEXT    NOT NULL,
    model                   TEXT,
    system_prompt           TEXT,
    placeholder             TEXT,
    quick_prompts_json      TEXT,
    provider_options_json   TEXT,
    sort_order              INTEGER NOT NULL DEFAULT 0,
    enabled                 INTEGER NOT NULL DEFAULT 1,
    updated_at              INTEGER NOT NULL
);

CREATE INDEX IX_dc_domain_enabled_sort ON dc_domain (enabled, sort_order);

CREATE TABLE chat_session (
    id              TEXT    NOT NULL PRIMARY KEY,
    title           TEXT    NOT NULL,
    domain_id       TEXT    NOT NULL,
    chat_mode       TEXT    NOT NULL,
    model           TEXT,
    resource_id     TEXT,
    created_at      INTEGER NOT NULL,
    updated_at      INTEGER NOT NULL
);

CREATE INDEX IX_chat_session_updated_at ON chat_session (updated_at DESC);

CREATE TABLE chat_message (
    id              TEXT    NOT NULL PRIMARY KEY,
    session_id      TEXT    NOT NULL,
    role            TEXT    NOT NULL,
    content         TEXT    NOT NULL,
    extras_json     TEXT,
    created_at      INTEGER NOT NULL,
    FOREIGN KEY (session_id) REFERENCES chat_session (id) ON DELETE CASCADE
);

CREATE INDEX IX_chat_message_session_id ON chat_message (session_id);
