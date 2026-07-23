export const CALL_CONFIG = {
  enableSystemAudioSharing: true,
} as const;

export const supportsSystemAudio = (): boolean => {
  if (typeof navigator === 'undefined' || !navigator.mediaDevices?.getDisplayMedia) return false;
  return !/Safari|Firefox/i.test(navigator.userAgent);
};

/**
 * Base RTCConfiguration for voice/video/screen-share.
 * Default iceServers is empty so browsers use host (LAN) candidates only —
 * required when the deployment has no international internet / public STUN.
 * Servers are loaded from GET /Call/ice-servers (optional internal STUN/TURN).
 */
export const createBaseRtcConfig = (iceServers: RTCIceServer[] = []): RTCConfiguration => ({
  iceServers,
  bundlePolicy: 'max-bundle',
  rtcpMuxPolicy: 'require',
});
