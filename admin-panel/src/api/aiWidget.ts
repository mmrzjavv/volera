import { apiRequest, getBaseUrl } from '@/lib/api';

const PREFIX = '/api/v1/company/ai-widget';

export interface SetupAiWidgetResponse {
  aiWidgetId: string;
  tenantId: string;
}

export interface SubmitContentResponse {
  jobId: string;
  contentBlockId: string;
  status: string;
}

export interface CompanyContentBlockDto {
  id: string;
  contentSnippet: string;
  status: string;
  jobId: string | null;
  errorMessage: string | null;
  createdAt: string;
}

export interface EmbedInfo {
  scriptUrl: string;
  branchId: string;
  dataBranch: string;
  dataColor: string;
  dataPosition: string;
  isActive: boolean;
}

export interface AiWidgetListItem {
  branchId: string;
  branchName: string;
  isActive: boolean;
}

export const aiWidgetApi = {
  setup: (companyToken: string, branchId: string) =>
    apiRequest<SetupAiWidgetResponse>(`${PREFIX}/setup`, {
      method: 'POST',
      body: JSON.stringify({ branchId }),
    }, companyToken),

  submitContent: (companyToken: string, branchId: string, content: string) =>
    apiRequest<SubmitContentResponse>(`${PREFIX}/content`, {
      method: 'POST',
      body: JSON.stringify({ branchId, content }),
    }, companyToken),

  getContentList: (companyToken: string, branchId: string) =>
    apiRequest<CompanyContentBlockDto[]>(`${PREFIX}/content?branchId=${encodeURIComponent(branchId)}`, {
      method: 'GET',
    }, companyToken),

  getEmbedInfo: (companyToken: string, branchId: string) =>
    apiRequest<EmbedInfo>(`${PREFIX}/embed-info?branchId=${encodeURIComponent(branchId)}`, {
      method: 'GET',
    }, companyToken),

  getWidgetList: (companyToken: string) =>
    apiRequest<AiWidgetListItem[]>(`${PREFIX}/list`, { method: 'GET' }, companyToken),
};

export function getAiWidgetScriptUrl(): string {
  const base = getBaseUrl().replace(/\/api.*$/, '');
  return `${base}/ai-widget.js`;
}

export function getAiWidgetEmbedScript(branchId: string, color = '0d9488', position = 'bottom-right'): string {
  const base = getBaseUrl().replace(/\/api.*$/, '');
  if (!base) throw new Error('NEXT_PUBLIC_API_URL is not set');
  const scriptUrl = `${base}/ai-widget.js`;
  return `<script src="${scriptUrl}" data-branch="${branchId}" data-color="${color.replace('#', '')}" data-position="${position}"></script>`;
}
