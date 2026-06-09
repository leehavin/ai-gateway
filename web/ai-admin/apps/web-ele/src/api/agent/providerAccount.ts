import { requestClient as http } from '#/api/request';

export function getPageList(params: any) {
  return http.request('/provider-account/paged-list', { method: 'GET', params });
}

export function getList(provider?: string) {
  return http.request('/provider-account', {
    method: 'GET',
    params: { provider },
  });
}

export const submitData = (params: any) =>
  http.request(`/provider-account/${params.id ?? ''}`, {
    method: params.id ? 'PUT' : 'POST',
    data: params,
  });

export const getSingle = (id: number) =>
  http.request(`/provider-account/${id}`, { method: 'GET' });

export const deleteData = (id: number) =>
  http.request(`/provider-account/${id}`, { method: 'DELETE' });
