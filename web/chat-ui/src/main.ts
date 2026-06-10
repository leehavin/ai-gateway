import { createApp } from 'vue'
import { Icon } from 'vue-devui/icon'
import '@devui-design/icons/icomoon/devui-icon.css'
import 'vue-devui/icon/style.css'
import App from './App.vue'
import { initHostAuthBridge } from './bridge/hostAuth'
import { initHostContextBridge } from './bridge/hostContext'
import './providers'
import './style.css'

initHostAuthBridge()
initHostContextBridge()

const app = createApp(App)
app.component('DIcon', Icon)
app.mount('#app')
