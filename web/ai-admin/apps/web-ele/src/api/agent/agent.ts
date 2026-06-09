import { requestClient as http } from '#/api/request';

export function getPageList(params: any) {
  return http.request('/agent/paged-list', { method: 'GET', params });
}

export function getList(provider?: string, enabled = true) {
  return http.request('/agent/agents', {
    method: 'GET',
    params: { provider, enabled },
  });
}

export const submitData = (params: any, editId?: string) => {
  if (editId) {
    return http.request(`/agent/${editId}`, { method: 'PUT', data: params });
  }
  return http.request('/agent', { method: 'POST', data: params });
};

export const getSingle = (id: string) =>
  http.request(`/agent/${id}`, { method: 'GET' });

export const deleteData = (id: string) =>
  http.request(`/agent/${id}`, { method: 'DELETE' });

export function getAgentResources(agentId: string, resourceType?: string) {
  return http.request(`/agent/${agentId}/resources`, {
    method: 'GET',
    params: { resourceType },
  });
}

export function submitAgentResource(params: any) {
  if (params.id) {
    return http.request(`/agent/${params.id}/resource`, {
      method: 'PUT',
      data: params,
    });
  }
  return http.request('/agent/resource', { method: 'POST', data: params });
}

export function deleteAgentResource(id: number) {
  return http.request(`/agent/${id}/resource`, { method: 'DELETE' });
}

export function syncCozeWorkflows(agentId: string) {
  return http.request(`/agent/${agentId}/sync-coze-workflows`, { method: 'POST' });
}
