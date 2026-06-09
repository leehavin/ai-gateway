/// <reference types="vite/client" />

interface ImportMetaEnv {
  readonly VITE_GATEWAY_URL: string
  readonly VITE_DATACHAT_TOKEN?: string
  readonly VITE_DEFAULT_DOMAIN: string
  readonly VITE_REQUIRE_LOGIN?: string
  readonly VITE_USE_SERVER_SESSIONS?: string
}

interface ImportMeta {
  readonly env: ImportMetaEnv
}
