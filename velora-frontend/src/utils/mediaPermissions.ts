/**
 * Mobile browsers only show the mic/camera prompt when getUserMedia runs
 * inside a user-gesture stack (the Accept / Call tap). Any await before
 * getUserMedia (API, ICE config, audio.play / AudioContext.resume) can make
 * the OS deny without prompting — especially on iOS Safari.
 *
 * LAN IP over plain HTTP is NOT a secure context on Chrome/Safari (desktop or
 * mobile). Docker serves HTTPS on :18262 — open https:// and accept the cert.
 */

export type MediaPermissionCode = 'insecure' | 'denied' | 'notfound' | 'unsupported' | 'unknown';

export type MediaPermissionResult =
  | { ok: true; stream: MediaStream }
  | { ok: false; code: MediaPermissionCode; message: string };

function isPrivateLanHost(hostname: string): boolean {
  if (hostname === 'localhost' || hostname === '127.0.0.1' || hostname === '[::1]') return true;
  if (/^192\.168\.\d{1,3}\.\d{1,3}$/.test(hostname)) return true;
  if (/^10\.\d{1,3}\.\d{1,3}\.\d{1,3}$/.test(hostname)) return true;
  if (/^172\.(1[6-9]|2\d|3[0-1])\.\d{1,3}\.\d{1,3}$/.test(hostname)) return true;
  return false;
}

function isInsecurePage(): boolean {
  return typeof window !== 'undefined' && !window.isSecureContext;
}

/** Suggested HTTPS origin for local Docker (port 18262) or current host on :443. */
export function suggestedHttpsOrigin(): string {
  if (typeof window === 'undefined') return 'https://localhost:18262';
  const { protocol, hostname, port } = window.location;
  if (protocol === 'https:') return window.location.origin;
  // Docker maps HTTPS to 18262; HTTP redirect listens on 18261
  if (port === '18261' || port === '18262' || port === '80' || port === '') {
    return `https://${hostname}:18262`;
  }
  return `https://${hostname}${port ? `:${port}` : ''}`;
}

/** Redirect LAN HTTP → HTTPS so getUserMedia can run (Chrome/Safari require secure context). */
export function redirectToHttpsIfNeeded(): boolean {
  if (typeof window === 'undefined') return false;
  if (window.isSecureContext) return false;
  const { hostname, port } = window.location;
  // Leave Vite/dev localhost HTTP alone only if mediaDevices already exists
  if (
    (hostname === 'localhost' || hostname === '127.0.0.1') &&
    typeof navigator.mediaDevices?.getUserMedia === 'function'
  ) {
    return false;
  }
  // Only auto-bounce private LAN / loopback HTTP → Docker HTTPS port
  if (!isPrivateLanHost(hostname)) return false;

  const target = suggestedHttpsOrigin() + window.location.pathname + window.location.search + window.location.hash;
  if (target.startsWith('https://') && window.location.href !== target) {
    window.location.replace(target);
    return true;
  }
  void port;
  return false;
}

/** iOS (incl. Chrome/Firefox on iOS — all use WebKit) needs a secure context for getUserMedia. */
export function isLikelyIosWebKit(): boolean {
  if (typeof navigator === 'undefined') return false;
  return /iPad|iPhone|iPod/i.test(navigator.userAgent) ||
    (navigator.platform === 'MacIntel' && navigator.maxTouchPoints > 1);
}

/**
 * True when the page can request mic/camera.
 * HTTPS / localhost always; never treat plain LAN HTTP as OK for calls
 * (Chrome desktop also blocks getUserMedia on http://192.168.x.x).
 */
export function canRequestMediaDevices(): boolean {
  if (typeof window === 'undefined') return false;
  if (window.isSecureContext && typeof navigator.mediaDevices?.getUserMedia === 'function') return true;
  return false;
}

export function insecureMediaMessage(): string {
  const httpsUrl = suggestedHttpsOrigin();
  return (
    `Calls need HTTPS. Open ${httpsUrl} (accept the certificate warning once), ` +
    'then try the call again. Plain http:// blocks the microphone on desktop and mobile.'
  );
}

export function describeMediaError(err: unknown): { code: MediaPermissionCode; message: string } {
  const insecure = isInsecurePage();
  const mediaMissing =
    typeof navigator === 'undefined' || !navigator.mediaDevices?.getUserMedia;

  // Missing mediaDevices on HTTP is almost always insecure-context — not "wrong browser".
  if (mediaMissing && insecure) {
    return { code: 'insecure', message: insecureMediaMessage() };
  }

  if (insecure) {
    return { code: 'insecure', message: insecureMediaMessage() };
  }

  const name = err && typeof err === 'object' && 'name' in err ? String((err as { name?: string }).name) : '';
  if (name === 'NotAllowedError' || name === 'PermissionDeniedError' || name === 'SecurityError') {
    return {
      code: 'denied',
      message:
        'Microphone access was blocked. Tap Allow when the browser asks, or enable Mic for this site in browser settings, then try again.',
    };
  }
  if (name === 'NotFoundError' || name === 'DevicesNotFoundError') {
    return {
      code: 'notfound',
      message: 'No microphone was found on this device.',
    };
  }
  if (name === 'NotReadableError' || name === 'TrackStartError') {
    return {
      code: 'unknown',
      message: 'Microphone is busy (another app may be using it). Close other apps and try again.',
    };
  }
  if (mediaMissing) {
    return {
      code: 'unsupported',
      message: insecureMediaMessage(),
    };
  }
  return {
    code: 'unknown',
    message: 'Could not access the microphone. Check browser permissions and try again.',
  };
}

/**
 * Request mic (+ optional camera). Call this as the first await in a click/tap handler.
 * Do not await AudioContext.resume, fetch, or SignalR before this on mobile.
 */
export async function requestCallMedia(isVideo: boolean): Promise<MediaPermissionResult> {
  if (typeof window !== 'undefined' && !window.isSecureContext) {
    return { ok: false, code: 'insecure', message: insecureMediaMessage() };
  }

  if (typeof navigator === 'undefined' || !navigator.mediaDevices?.getUserMedia) {
    const d = describeMediaError({ name: 'unsupported' });
    return { ok: false, code: d.code, message: d.message };
  }

  // Prefer simple constraints first — advanced audio constraints break some mobile WebViews.
  const attempts: MediaStreamConstraints[] = isVideo
    ? [
        { audio: true, video: { facingMode: 'user' } },
        { audio: true, video: true },
        { audio: true, video: false },
      ]
    : [
        { audio: true },
        {
          audio: {
            echoCancellation: true,
            noiseSuppression: true,
            autoGainControl: true,
          },
        },
      ];

  let lastError: unknown;
  for (const constraints of attempts) {
    try {
      const stream = await navigator.mediaDevices.getUserMedia(constraints);
      // Ensure tracks are live — some mobiles deliver disabled tracks until enabled.
      stream.getTracks().forEach((t) => {
        t.enabled = true;
      });
      return { ok: true, stream };
    } catch (err) {
      lastError = err;
      const name = err && typeof err === 'object' && 'name' in err ? String((err as { name: string }).name) : '';
      if (name === 'NotAllowedError' || name === 'PermissionDeniedError' || name === 'SecurityError') {
        break;
      }
    }
  }

  const d = describeMediaError(lastError);
  return { ok: false, code: d.code, message: d.message };
}
