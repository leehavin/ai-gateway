import { requestClient as http } from '#/api/request';

export function getWorkspaces(providerAccountId: number) {
  return http.request(`/coze-discovery/${providerAccountId}/workspaces`, {
    method: 'GET',
  });
}

export function getBots(providerAccountId: number, space: string) {
  return http.request(`/coze-discovery/${providerAccountId}/bots`, {
    method: 'GET',
    params: { space },
  });
}

export function getWorkflows(
  providerAccountId: number,
  space: string,
  bot?: string,
  publishStatus = 'published_online',
) {
  return http.request(`/coze-discovery/${providerAccountId}/workflows`, {
    method: 'GET',
    params: { space, bot, publishStatus },
  });
}
