import { createApp } from 'vue'
import { Icon } from 'vue-devui/icon'
import '@devui-design/icons/icomoon/devui-icon.css'
import 'vue-devui/icon/style.css'
import App from './App.vue'
import { initHostAuthBridge } from './bridge/hostAuth'
import './style.css'

initHostAuthBridge()

const app = createApp(App)
app.component('DIcon', Icon)
app.mount('#app')
