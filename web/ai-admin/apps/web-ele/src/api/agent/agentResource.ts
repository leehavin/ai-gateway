import { requestClient as http } from '#/api/request';

export function getPageList(params: any) {
  return http.request('/agent-resource/paged-list', { method: 'GET', params });
}

export function getByAgent(agentId: string, resourceType?: string) {
  return http.request(`/agent-resource/${agentId}/by-agent`, {
    method: 'GET',
    params: { resourceType },
  });
}

export const submitData = (params: any) =>
  http.request(`/agent-resource/${params.id ?? ''}`, {
    method: params.id ? 'PUT' : 'POST',
    data: params,
  });

export const getSingle = (id: number) =>
  http.request(`/agent-resource/${id}`, { method: 'GET' });

export const deleteData = (id: number) =>
  http.request(`/agent-resource/${id}`, { method: 'DELETE' });
