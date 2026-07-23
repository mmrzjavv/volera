/**
 * API client for backend. Uses NEXT_PUBLIC_API_URL and X-Company-Token when provided.
 * No hardcoded foreign API fallback — set NEXT_PUBLIC_API_URL explicitly.
 */

export const getBaseUrl = (): string => {
  const fromEnv = (process.env.NEXT_PUBLIC_API_URL ?? '').replace(/\/$/, '');
  if (fromEnv) return fromEnv;
  if (typeof window !== 'undefined') {
    console.error('NEXT_PUBLIC_API_URL is not set. Configure a domestically reachable API base URL.');
  }
  return '';
};

export interface ApiResponse<T> {
  success: boolean;
  operationDate: string;
  data: T | null;
  message?: string[];
}

export async function apiRequest<T>(
  path: string,
  options: RequestInit = {},
  companyToken?: string | null
): Promise<ApiResponse<T>> {
  const base = getBaseUrl();
  const url = path.startsWith('http') ? path : `${base}${path.startsWith('/') ? '' : '/'}${path}`;
  const headers: Record<string, string> = {
    'Content-Type': 'application/json',
    ...(typeof options.headers === 'object' && !Array.isArray(options.headers)
      ? Object.fromEntries(
          Object.entries(options.headers).map(([k, v]) => [k, String(v)])
        )
      : {}),
  };
  if (companyToken) {
    headers['X-Company-Token'] = companyToken;
  }
  const res = await fetch(url, { ...options, headers });
  const body = await res.json().catch(() => ({})) as ApiResponse<T>;
  if (!res.ok) {
    const msg = Array.isArray(body.message)
      ? body.message.join(' ')
      : typeof body.message === 'string'
        ? body.message
        : body.message?.[0] ?? `Request failed: ${res.status}`;
    throw new Error(msg);
  }
  return body;
}

/** Request with Authorization: Bearer token (e.g. for support user API). */
export async function apiRequestWithBearer<T>(
  path: string,
  bearerToken: string,
  options: RequestInit = {}
): Promise<ApiResponse<T>> {
  const base = getBaseUrl();
  const url = path.startsWith('http') ? path : `${base}${path.startsWith('/') ? '' : '/'}${path}`;
  const headers: Record<string, string> = {
    'Content-Type': 'application/json',
    Authorization: `Bearer ${bearerToken}`,
    ...(typeof options.headers === 'object' && !Array.isArray(options.headers)
      ? Object.fromEntries(
          Object.entries(options.headers).map(([k, v]) => [k, String(v)])
        )
      : {}),
  };
  const res = await fetch(url, { ...options, headers });
  const body = await res.json().catch(() => ({})) as ApiResponse<T>;
  if (!res.ok) {
    const msg = Array.isArray(body.message)
      ? body.message.join(' ')
      : typeof body.message === 'string'
        ? body.message
        : body.message?.[0] ?? `Request failed: ${res.status}`;
    throw new Error(msg);
  }
  return body;
}
