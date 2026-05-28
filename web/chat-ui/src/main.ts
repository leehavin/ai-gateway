import { createApp } from 'vue'
import MateChat from '@matechat/core'
import { Icon } from 'vue-devui/icon'
import '@devui-design/icons/icomoon/devui-icon.css'
import 'vue-devui/icon/style.css'
import App from './App.vue'
import './style.css'

const app = createApp(App)
app.component('DIcon', Icon)
app.use(MateChat).mount('#app')
