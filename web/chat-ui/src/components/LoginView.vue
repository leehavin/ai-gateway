<script setup lang="ts">
import { ref } from 'vue'
import { BRAND } from '../constants/brand'

const props = defineProps<{
  loading?: boolean
  error?: string
}>()

const emit = defineEmits<{
  submit: [username: string, password: string]
}>()

const username = ref('')
const password = ref('')

function onSubmit() {
  if (!username.value.trim() || !password.value) return
  emit('submit', username.value.trim(), password.value)
}
</script>

<template>
  <div class="login-shell">
    <form class="login-card" @submit.prevent="onSubmit">
      <img class="login-logo" :src="BRAND.logo" :alt="BRAND.name" width="120" />
      <h1 class="login-title">{{ BRAND.name }}</h1>
      <p class="login-sub">登录后使用智能对话与数据助手</p>

      <label class="login-field">
        <span>用户名</span>
        <input
          v-model="username"
          type="text"
          autocomplete="username"
          placeholder="请输入用户名"
          :disabled="loading"
        />
      </label>

      <label class="login-field">
        <span>密码</span>
        <input
          v-model="password"
          type="password"
          autocomplete="current-password"
          placeholder="请输入密码"
          :disabled="loading"
        />
      </label>

      <p v-if="error" class="login-error">{{ error }}</p>

      <button class="login-btn" type="submit" :disabled="loading || !username.trim() || !password">
        {{ loading ? '登录中…' : '登录' }}
      </button>
    </form>
  </div>
</template>

<style scoped>
.login-shell {
  display: flex;
  align-items: center;
  justify-content: center;
  width: 100%;
  height: 100vh;
  background: var(--dc-bg);
}

.login-card {
  width: min(400px, calc(100% - 32px));
  padding: 32px 28px;
  border: 1px solid var(--dc-border);
  border-radius: var(--dc-radius-lg);
  background: #fff;
  box-shadow: var(--dc-shadow-md);
  display: flex;
  flex-direction: column;
  align-items: center;
  gap: 14px;
}

.login-logo {
  object-fit: contain;
}

.login-title {
  margin: 0;
  font-size: 22px;
  font-weight: 600;
  color: var(--dc-text);
}

.login-sub {
  margin: 0 0 8px;
  font-size: 13px;
  color: var(--dc-text-secondary);
}

.login-field {
  width: 100%;
  display: flex;
  flex-direction: column;
  gap: 6px;
  font-size: 13px;
  color: var(--dc-text-secondary);
}

.login-field input {
  padding: 10px 12px;
  border: 1px solid var(--dc-border);
  border-radius: var(--dc-radius-md);
  font-size: 14px;
  outline: none;
}

.login-field input:focus {
  border-color: rgba(94, 124, 224, 0.6);
  box-shadow: 0 0 0 3px rgba(94, 124, 224, 0.12);
}

.login-error {
  width: 100%;
  margin: 0;
  padding: 8px 10px;
  border-radius: var(--dc-radius-md);
  background: var(--dc-error-bg);
  color: var(--dc-error);
  font-size: 13px;
}

.login-btn {
  width: 100%;
  margin-top: 4px;
  padding: 11px 16px;
  border: none;
  border-radius: var(--dc-radius-md);
  background: var(--dc-brand);
  color: #fff;
  font-size: 14px;
  font-weight: 600;
  cursor: pointer;
}

.login-btn:disabled {
  opacity: 0.6;
  cursor: not-allowed;
}
</style>
