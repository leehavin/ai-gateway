/*
  DataChat — 为已有库增加 user_id（会话按用户隔离）
  执行: sqlite3 data/datachat.db < 03-add-user-id.sql
*/
ALTER TABLE chat_session ADD COLUMN user_id TEXT;
CREATE INDEX IF NOT EXISTS IX_chat_session_user_id ON chat_session (user_id);
