import type { HostRunWorkflowPayload } from './hostContext'

/** 宿主通过 iframe URL 带入案件（部分发文系统用改 src 而非 postMessage）。 */
export function parseHostLaunchParams(search = window.location.search): HostRunWorkflowPayload | null {
  const params = new URLSearchParams(search)
  const action = params.get('hostAction') ?? params.get('action')
  if (action !== 'runWorkflow') return null

  const workflowId = params.get('workflowId') ?? params.get('workflow') ?? undefined
  const workflowName = params.get('workflowName') ?? undefined
  if (!workflowId && !workflowName) return null

  const filesRaw = params.get('files')
  const files = filesRaw
    ? filesRaw
        .split(',')
        .map((pair) => {
          const sep = pair.indexOf(':')
          if (sep <= 0) return null
          const fileId = pair.slice(0, sep).trim()
          const name = decodeURIComponent(pair.slice(sep + 1).trim())
          return fileId && name ? { fileId, name } : null
        })
        .filter((x): x is { fileId: string; name: string } => x !== null)
    : undefined

  return {
    domainId: params.get('domainId') ?? params.get('domain') ?? undefined,
    workflowId,
    workflowName,
    input: params.get('input') ?? undefined,
    files: files?.length ? files : undefined,
    newSession: params.get('newSession') !== '0',
    prefillOnly: params.get('prefillOnly') === '1',
  }
}
