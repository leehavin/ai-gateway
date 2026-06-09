import { requestClient as http } from '#/api/request';

export function getPageList(params: any) {
  return http.request('/agent/paged-list', { method: 'GET', params });
}

export function getList(provider?: string, enabled = true) {
  return http.request('/agent', {
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
