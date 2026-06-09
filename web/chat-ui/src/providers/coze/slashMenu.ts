import type { SlashCommandItem, SlashMenuContext } from '../types'
import { workflowMenuDesc } from './workflowInputHint'

export function buildCozeSlashMenu(
  ctx: SlashMenuContext,
  filter: string
): SlashCommandItem[] {
  const cozeDomains = ctx.domains.filter((d) => d.provider === 'coze')
  const items: SlashCommandItem[] = [
    {
      id: 'coze-refresh-wf',
      kind: 'action',
      label: ctx.cozeWorkflowsLoading ? '正在加载工作流…' : '刷新工作流列表',
      desc: '从 Coze GET /v1/workflows 拉取最新列表',
      category: '工作流',
      providerId: 'coze',
      action: 'refresh-workflows',
    },
    {
      id: 'coze-upload',
      kind: 'action',
      label: '上传文件并对话',
      desc: '支持图片、PDF、文档等，由网关转发给当前 Bot',
      category: 'Coze',
      providerId: 'coze',
      action: 'upload',
    },
    {
      id: 'coze-new',
      kind: 'action',
      label: '新建 Coze 会话',
      desc: '清空当前对话并保留 Coze 领域',
      category: 'Coze',
      providerId: 'coze',
      action: 'new-chat',
    },
  ]

  if (ctx.cozeWorkflowsError && !ctx.cozeWorkflows.length) {
    items.push({
      id: 'coze-wf-error',
      kind: 'action',
      label: '工作流加载失败',
      desc: ctx.cozeWorkflowsError.slice(0, 80),
      category: '工作流',
      providerId: 'coze',
    })
  } else if (!ctx.cozeWorkflowsLoading && !ctx.cozeWorkflows.length) {
    items.push({
      id: 'coze-wf-empty',
      kind: 'action',
      label: '暂无已发布工作流',
      desc: '请在领域配置 coze.workspaceId，或检查 PAT 空间权限',
      category: '工作流',
      providerId: 'coze',
    })
  }

  for (const wf of ctx.cozeWorkflows) {
    items.push({
      id: `coze-wf-${wf.workflowId}`,
      kind: 'action',
      label: wf.displayName,
      desc: workflowMenuDesc(wf),
      category: '工作流',
      providerId: 'coze',
      action: 'run-workflow',
      workflowId: wf.workflowId,
    })
  }

  for (const bot of ctx.cozeBots) {
    items.push({
      id: `coze-bot-${bot.domainId}`,
      kind: 'domain',
      label: bot.displayName,
      desc: `Bot ${bot.botId}`,
      category: '切换 Bot',
      providerId: 'coze',
      domainId: bot.domainId,
      action: 'switch-domain',
    })
  }

  for (const d of cozeDomains) {
    if (ctx.cozeBots.some((b) => b.domainId === d.id)) continue
    items.push({
      id: `coze-domain-${d.id}`,
      kind: 'domain',
      label: d.displayName,
      desc: d.coze?.botId ? `Bot ${d.coze.botId}` : undefined,
      category: '切换 Bot',
      providerId: 'coze',
      domainId: d.id,
      action: 'switch-domain',
    })
  }

  void filter
  return items
}

export function stripCozeSlashTokens(message: string): string {
  return message.replace(/^\/coze(?:\s+\S+)?\s*/i, '').trim()
}
