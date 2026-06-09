import { computed, onMounted, onUnmounted, ref } from 'vue'
import { fetchMe, login as apiLogin, toDisplayUser } from '../api/auth'
import {
  clearAuth,
  getAuthToken,
  getHostUser,
  isEmbeddedHost,
  isUserSessionToken,
  onAuthTokenChange,
  onHostUserChange,
} from '../bridge/hostAuth'

const requireLoginEnv = () => import.meta.env.VITE_REQUIRE_LOGIN === 'true'

export function useAuth() {
  const ready = ref(false)
  const checking = ref(true)
  const loggingIn = ref(false)
  const error = ref('')
  const authenticated = ref(false)
  const hostUser = ref(getHostUser())
  const token = ref(getAuthToken())

  const isEmbedded = computed(() => isEmbeddedHost())
  const displayName = computed(() => toDisplayUser(hostUser.value))
  const needsLogin = computed(
    () => ready.value && !checking.value && !authenticated.value && !isEmbedded.value
  )

  function syncFromBridge() {
    hostUser.value = getHostUser()
    token.value = getAuthToken()
  }

  async function verifySession() {
    checking.value = true
    error.value = ''
    syncFromBridge()

    if (isEmbedded.value && hostUser.value) {
      authenticated.value = true
      checking.value = false
      ready.value = true
      return
    }

    try {
      const me = await fetchMe()
      if (me) {
        authenticated.value = true
        if (!me.isSharedToken) {
          hostUser.value = { userId: me.userId, userName: me.userName }
        }
        checking.value = false
        ready.value = true
        return
      }
    } catch {
      if (isUserSessionToken()) clearAuth()
      syncFromBridge()
    }

    if (!requireLoginEnv() && getAuthToken() === 'demo-token') {
      authenticated.value = true
    } else if (!requireLoginEnv() && getAuthToken()) {
      authenticated.value = true
    } else {
      authenticated.value = false
    }

    checking.value = false
    ready.value = true
  }

  async function login(username: string, password: string) {
    loggingIn.value = true
    error.value = ''
    try {
      const result = await apiLogin(username, password)
      hostUser.value = { userId: result.userId, userName: result.userName }
      token.value = result.token
      authenticated.value = true
    } catch (e) {
      error.value = e instanceof Error ? e.message : '登录失败'
      throw e
    } finally {
      loggingIn.value = false
    }
  }

  function logout() {
    clearAuth()
    syncFromBridge()
    authenticated.value = false
  }

  let offToken = () => {}
  let offUser = () => {}

  onMounted(() => {
    offToken = onAuthTokenChange(() => {
      syncFromBridge()
      if (isEmbedded.value && getHostUser()) authenticated.value = true
    })
    offUser = onHostUserChange(() => {
      syncFromBridge()
      if (isEmbedded.value && getHostUser()) authenticated.value = true
    })
    void verifySession()
  })

  onUnmounted(() => {
    offToken()
    offUser()
  })

  return {
    ready,
    checking,
    loggingIn,
    error,
    hostUser,
    displayName,
    isEmbedded,
    isAuthenticated: authenticated,
    needsLogin,
    login,
    logout,
    verifySession,
  }
}
