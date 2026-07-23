/** Decode JWT payload without verifying signature (client-side expiry checks only). */
export function decodeJwtPayload(token: string): Record<string, unknown> | null {
  const parts = token.split('.');
  if (parts.length < 2) return null;
  try {
    const base64 = parts[1].replace(/-/g, '+').replace(/_/g, '/');
    const padded = base64.padEnd(base64.length + ((4 - (base64.length % 4)) % 4), '=');
    return JSON.parse(atob(padded)) as Record<string, unknown>;
  } catch {
    return null;
  }
}

/** Access-token expiry in ms since epoch, or null if missing/undecodable. */
export function getAccessTokenExpiryMs(token: string): number | null {
  const payload = decodeJwtPayload(token);
  const exp = payload?.exp;
  return typeof exp === 'number' ? exp * 1000 : null;
}

/**
 * True when the access token is missing or past expiry (with skew).
 * Undecodable tokens are treated as not expired so the server can decide.
 */
export function isAccessTokenExpired(token: string | null | undefined, skewMs = 60_000): boolean {
  if (!token) return true;
  const expMs = getAccessTokenExpiryMs(token);
  if (expMs == null) return false;
  return Date.now() >= expMs - skewMs;
}
