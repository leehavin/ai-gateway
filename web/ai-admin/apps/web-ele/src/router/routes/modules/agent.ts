import type { RouteRecordRaw } from 'vue-router';

import { BasicLayout } from '#/layouts';
import { $t } from '#/locales';

const routes: RouteRecordRaw[] = [
  {
    component: BasicLayout,
    meta: {
      icon: 'mdi:robot-outline',
      keepAlive: true,
      order: 800,
      authority: ['agent'],
      title: $t('page.agent.title'),
    },
    name: 'AgentCenter',
    path: '/agent',
    children: [
      {
        meta: {
          title: $t('page.agent.providerAccount.title'),
          authority: ['agent.provider'],
          icon: 'mdi:connection',
        },
        name: 'ProviderAccount',
        path: '/agent/provider-account',
        component: () => import('#/views/agent/providerAccount/index.vue'),
      },
      {
        meta: {
          title: $t('page.agent.manage.title'),
          authority: ['agent.manage'],
          icon: 'mdi:robot',
        },
        name: 'AgentManage',
        path: '/agent/manage',
        component: () => import('#/views/agent/manage/index.vue'),
      },
      {
        meta: {
          title: $t('page.agent.access.title'),
          authority: ['agent.access'],
          icon: 'mdi:shield-account-outline',
        },
        name: 'AgentAccess',
        path: '/agent/access',
        component: () => import('#/views/agent/access/index.vue'),
      },
    ],
  },
];

export default routes;
