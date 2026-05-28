/// <reference types="vite/client" />

interface ImportMetaEnv {
  readonly VITE_GATEWAY_URL: string
  readonly VITE_DATACHAT_TOKEN: string
  readonly VITE_DEFAULT_DOMAIN: string
}

interface ImportMeta {
  readonly env: ImportMetaEnv
}
