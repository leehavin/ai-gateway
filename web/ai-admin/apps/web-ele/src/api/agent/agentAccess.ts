import { requestClient as http } from '#/api/request';

export function getRoleAgents(roleId: number) {
  return http.request(`/agent-access/${roleId}/agents`, { method: 'GET' });
}

export function assignRoleAgents(roleId: number, agentIds: string[]) {
  return http.request(`/agent-access/${roleId}/assign-agents`, {
    method: 'POST',
    data: { agentIds },
  });
}

export function getRoleResources(
  roleId: number,
  agentId?: string,
  resourceType = 'workflow',
) {
  return http.request(`/agent-access/${roleId}/resources`, {
    method: 'GET',
    params: { agent: agentId, resourceType },
  });
}

export function assignRoleResources(roleId: number, resourceRowIds: number[]) {
  return http.request(`/agent-access/${roleId}/assign-resources`, {
    method: 'POST',
    data: { resourceRowIds },
  });
}
