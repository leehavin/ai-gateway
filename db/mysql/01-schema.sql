/*
  DataChat — MySQL / MariaDB 表结构
  执行前: CREATE DATABASE Chat CHARACTER SET utf8mb4 COLLATE utf8mb4_unicode_ci;
  执行: mysql -u user -p DataChat < 01-schema.sql
*/

SET NAMES utf8mb4;
SET FOREIGN_KEY_CHECKS = 0;

DROP TABLE IF EXISTS `chat_message`;
DROP TABLE IF EXISTS `chat_session`;
DROP TABLE IF EXISTS `dc_domain`;
DROP TABLE IF EXISTS `dc_global_defaults`;

CREATE TABLE `dc_global_defaults` (
    `id`                  INT             NOT NULL,
    `version`             INT             NOT NULL DEFAULT 1,
    `dbgpt_base_url`      VARCHAR(512)    NOT NULL,
    `coze_endpoint`       VARCHAR(128)    NOT NULL,
    `timeout_seconds`     INT             NOT NULL DEFAULT 120,
    `max_history_turns`   INT             NOT NULL DEFAULT 20,
    `updated_at`          BIGINT          NOT NULL,
    PRIMARY KEY (`id`)
) ENGINE=InnoDB DEFAULT CHARSET=utf8mb4 COLLATE=utf8mb4_unicode_ci;

CREATE TABLE `dc_domain` (
    `id`                      VARCHAR(64)     NOT NULL,
    `display_name`            VARCHAR(128)    NOT NULL,
    `chat_mode`               VARCHAR(64)     NOT NULL,
    `provider`                VARCHAR(32)     NOT NULL,
    `model`                   VARCHAR(128)    NULL,
    `system_prompt`           LONGTEXT        NULL,
    `placeholder`             VARCHAR(512)    NULL,
    `quick_prompts_json`      LONGTEXT        NULL,
    `provider_options_json`   LONGTEXT        NULL,
    `sort_order`              INT             NOT NULL DEFAULT 0,
    `enabled`                 TINYINT(1)      NOT NULL DEFAULT 1,
    `updated_at`              BIGINT          NOT NULL,
    PRIMARY KEY (`id`),
    KEY `IX_dc_domain_enabled_sort` (`enabled`, `sort_order`)
) ENGINE=InnoDB DEFAULT CHARSET=utf8mb4 COLLATE=utf8mb4_unicode_ci;

CREATE TABLE `chat_session` (
    `id`              VARCHAR(64)     NOT NULL,
    `title`           VARCHAR(256)    NOT NULL,
    `domain_id`       VARCHAR(64)     NOT NULL,
    `chat_mode`       VARCHAR(64)     NOT NULL,
    `model`           VARCHAR(128)    NULL,
    `resource_id`     VARCHAR(128)    NULL,
    `created_at`      BIGINT          NOT NULL,
    `updated_at`      BIGINT          NOT NULL,
    `user_id`         VARCHAR(128)    NULL,
    PRIMARY KEY (`id`),
    KEY `IX_chat_session_updated_at` (`updated_at`),
    KEY `IX_chat_session_user_id` (`user_id`)
) ENGINE=InnoDB DEFAULT CHARSET=utf8mb4 COLLATE=utf8mb4_unicode_ci;

CREATE TABLE `chat_message` (
    `id`              VARCHAR(64)     NOT NULL,
    `session_id`      VARCHAR(64)     NOT NULL,
    `role`            VARCHAR(32)     NOT NULL,
    `content`         LONGTEXT        NOT NULL,
    `extras_json`     LONGTEXT        NULL,
    `created_at`      BIGINT          NOT NULL,
    PRIMARY KEY (`id`),
    KEY `IX_chat_message_session_id` (`session_id`),
    CONSTRAINT `FK_chat_message_session`
        FOREIGN KEY (`session_id`) REFERENCES `chat_session` (`id`) ON DELETE CASCADE
) ENGINE=InnoDB DEFAULT CHARSET=utf8mb4 COLLATE=utf8mb4_unicode_ci;

SET FOREIGN_KEY_CHECKS = 1;
