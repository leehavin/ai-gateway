/*
  DataChat — 为已有库增加 user_id（会话按用户隔离）
*/
SET @col_exists := (
    SELECT COUNT(*) FROM information_schema.COLUMNS
    WHERE TABLE_SCHEMA = DATABASE()
      AND TABLE_NAME = 'chat_session'
      AND COLUMN_NAME = 'user_id'
);
SET @sql := IF(@col_exists = 0,
    'ALTER TABLE `chat_session` ADD COLUMN `user_id` VARCHAR(128) NULL',
    'SELECT 1');
PREPARE stmt FROM @sql;
EXECUTE stmt;
DEALLOCATE PREPARE stmt;

SET @idx_exists := (
    SELECT COUNT(*) FROM information_schema.STATISTICS
    WHERE TABLE_SCHEMA = DATABASE()
      AND TABLE_NAME = 'chat_session'
      AND INDEX_NAME = 'IX_chat_session_user_id'
);
SET @sql := IF(@idx_exists = 0,
    'CREATE INDEX `IX_chat_session_user_id` ON `chat_session` (`user_id`)',
    'SELECT 1');
PREPARE stmt FROM @sql;
EXECUTE stmt;
DEALLOCATE PREPARE stmt;
