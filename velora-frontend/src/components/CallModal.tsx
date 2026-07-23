import { useCallback, useEffect, useRef, useState } from 'react';
import { useCallStore } from '../store/useCallStore';
import { PhoneOff, Mic, MicOff, PhoneIncoming, Video, VideoOff, Lock, Minimize2, Maximize2, Monitor, MonitorOff, Volume2 } from 'lucide-react';
import { clsx } from 'clsx';
import { ringtoneManager } from '../utils/RingtoneManager';
import { getInitials } from '../utils/getInitials';
import { supportsSystemAudio } from '../config/call';
import { canRequestMediaDevices, insecureMediaMessage, suggestedHttpsOrigin } from '../utils/mediaPermissions';
import { useToastStore } from '../store/useToastStore';

function attachMediaStream(
  el: HTMLMediaElement | null,
  stream: MediaStream | null | undefined,
  options?: { play?: boolean; muted?: boolean; volume?: number }
) {
  if (!el || !stream) return;
  if (el.srcObject !== stream) {
    el.srcObject = stream;
  }
  if (typeof options?.muted === 'boolean') el.muted = options.muted;
  if (typeof options?.volume === 'number') el.volume = options.volume;
  if (options?.play !== false) {
    void el.play().catch(() => {});
  }
}

export const CallModal = () => {
  const { 
    status, 
    callerName, 
    receiverName, 
    otherPartyProfilePicture,
    isCaller, 
    isVideo,
    isEncrypted,
    isMinimized,
    localStream, 
    remoteStream,
    remoteScreenStream,
    isScreenSharing,
    isScreenShareLoading,
    remoteScreenShareUserId,
    isScreenShareWithSystemAudio,
    acceptCall, 
    rejectCall, 
    endCall,
    setMinimized,
    screenShareView,
    setScreenShareView,
    toggleMute,
    toggleVideo,
    isMuted,
    startScreenShare,
    stopScreenShare,
  } = useCallStore();

  const [includeSystemAudio, setIncludeSystemAudio] = useState(false);
  const supportsScreenShare = typeof navigator !== 'undefined' && !!navigator.mediaDevices?.getDisplayMedia;
  const mediaAvailable = canRequestMediaDevices();

  const localAudioRef = useRef<HTMLAudioElement>(null);
  const remoteAudioRef = useRef<HTMLAudioElement>(null);
  const localVideoRef = useRef<HTMLVideoElement>(null);
  const remoteVideoRef = useRef<HTMLVideoElement>(null);
  const remoteScreenRef = useRef<HTMLVideoElement>(null);
  const fullPageScreenRef = useRef<HTMLVideoElement>(null);
  const minimizedScreenRef = useRef<HTMLVideoElement>(null);
  const [isVideoEnabled, setIsVideoEnabled] = useState(true);
  const [isAcceptingUi, setIsAcceptingUi] = useState(false);

  const showingRemoteScreen = !!(remoteScreenShareUserId && !isScreenSharing);
  const hasLocalVideoTrack = !!localStream?.getVideoTracks().some((t) => t.readyState !== 'ended');

  // Keep PIP srcObject in sync when the <video> mounts after localStream is already set
  // (caller path: ensureLocalMedia runs while status === 'idle' and CallModal returns null).
  const setLocalVideoRef = useCallback(
    (el: HTMLVideoElement | null) => {
      localVideoRef.current = el;
      attachMediaStream(el, localStream, { muted: true, play: true });
    },
    [localStream]
  );

  // Ringtone Management
  useEffect(() => {
    if (status === 'incoming') {
      if (isVideo) {
        ringtoneManager.playVideoIncomingRing();
      } else {
        ringtoneManager.playIncomingRing();
      }
    } else if (status === 'calling') {
      ringtoneManager.playOutgoingRing();
    } else {
      ringtoneManager.stop();
    }

    return () => {
      ringtoneManager.stop();
    };
  }, [status, isVideo]);

  useEffect(() => {
    attachMediaStream(localAudioRef.current, localStream, { muted: true, play: true });
    attachMediaStream(localVideoRef.current, localStream, { muted: true, play: true });
  }, [localStream, isVideo, status]);

  // Stream to show for remote video/screen: prefer dedicated screen stream when remote is sharing
  const remoteDisplayStream = (remoteScreenShareUserId && remoteScreenStream) ? remoteScreenStream : remoteStream;
  const voiceScreenStream = (remoteScreenShareUserId && remoteScreenStream) ? remoteScreenStream : (remoteStream?.getVideoTracks().length ? remoteStream : null);

  useEffect(() => {
    if (remoteAudioRef.current && remoteStream) {
      remoteAudioRef.current.srcObject = remoteStream;
      remoteAudioRef.current.volume = 1;
      remoteAudioRef.current.muted = false;
      remoteAudioRef.current.play().catch(() => {});
    }
    if (remoteVideoRef.current && remoteDisplayStream) {
      remoteVideoRef.current.srcObject = remoteDisplayStream;
      remoteVideoRef.current.play().catch(() => {});
    }
    if (remoteScreenRef.current && voiceScreenStream) {
      remoteScreenRef.current.srcObject = voiceScreenStream;
      remoteScreenRef.current.play().catch(() => {});
    }
    if (fullPageScreenRef.current && screenShareView === 'fullpage' && remoteDisplayStream) {
      fullPageScreenRef.current.srcObject = remoteDisplayStream;
      fullPageScreenRef.current.play().catch(() => {});
    }
    if (minimizedScreenRef.current && screenShareView === 'minimized' && remoteDisplayStream) {
      minimizedScreenRef.current.srcObject = remoteDisplayStream;
      minimizedScreenRef.current.play().catch(() => {});
    }
  }, [remoteStream, remoteScreenStream, remoteScreenShareUserId, isVideo, voiceScreenStream, screenShareView, remoteDisplayStream]);

  const handleAccept = async () => {
    if (isAcceptingUi || status !== 'incoming') return;
    setIsAcceptingUi(true);
    try {
      // Stop ringtone only (sync). Do NOT unlock/resume AudioContext before getUserMedia —
      // on iOS that can consume the user gesture and deny the mic without prompting.
      ringtoneManager.stop();
      await acceptCall();
      ringtoneManager.unlock();
      if (remoteAudioRef.current) {
        remoteAudioRef.current.muted = false;
        remoteAudioRef.current.volume = 1;
        await remoteAudioRef.current.play().catch(() => {});
      }
    } finally {
      setIsAcceptingUi(false);
    }
  };

  const handleToggleMute = () => {
    toggleMute();
  };

  const handleToggleVideo = () => {
      toggleVideo();
      setIsVideoEnabled(!isVideoEnabled);
  };

  if (status === 'idle') return null;

  const otherName = isCaller ? receiverName : callerName;
  const callLabel = isVideo ? 'Video call' : 'Voice call';
  const showMinimizedBar = status === 'connected' && isMinimized;
  const showScreenShareMinimizedBar = status === 'connected' && showingRemoteScreen && screenShareView === 'minimized';
  const showScreenShareFullPage = status === 'connected' && showingRemoteScreen && screenShareView === 'fullpage';

  return (
    <>
      {/* Screen share: full-page overlay – full viewport, responsive, safe areas */}
      {showScreenShareFullPage && (
        <div
          className="fixed inset-0 z-[70] flex flex-col bg-black"
          style={{ paddingTop: 'env(safe-area-inset-top)', paddingBottom: 'env(safe-area-inset-bottom)', paddingLeft: 'env(safe-area-inset-left)', paddingRight: 'env(safe-area-inset-right)' }}
        >
          <video
            ref={fullPageScreenRef}
            autoPlay
            playsInline
            className="w-full h-full min-h-0 object-contain"
          />
          <div className="absolute inset-x-0 top-0 flex items-center justify-between p-3 sm:p-4 bg-gradient-to-b from-black/70 to-transparent" style={{ paddingTop: 'max(0.75rem, env(safe-area-inset-top))' }}>
            <span className="flex items-center gap-2 text-white text-sm font-medium">
              <Monitor size={18} />
              <span className="truncate">{otherName}&apos;s screen</span>
            </span>
            <div className="flex items-center gap-2">
              <button
                type="button"
                onClick={() => setScreenShareView('inline')}
                className="min-w-[44px] min-h-[44px] rounded-full bg-white/20 hover:bg-white/30 flex items-center justify-center text-white touch-manipulation active:scale-95"
                title="Back to call"
                aria-label="Back to call"
              >
                <Minimize2 size={20} />
              </button>
              <button
                type="button"
                onClick={() => setScreenShareView('minimized')}
                className="min-w-[44px] min-h-[44px] rounded-full bg-white/20 hover:bg-white/30 flex items-center justify-center text-white touch-manipulation active:scale-95"
                title="Minimize to bar"
                aria-label="Minimize screen share"
              >
                <Minimize2 size={20} />
              </button>
            </div>
          </div>
          <div className="absolute inset-x-0 bottom-0 flex justify-center gap-3 p-3 sm:p-4 bg-gradient-to-t from-black/70 to-transparent" style={{ paddingBottom: 'max(0.75rem, env(safe-area-inset-bottom))' }}>
            <button
              type="button"
              onClick={() => setScreenShareView('inline')}
              className="px-4 py-2.5 rounded-xl bg-white/20 hover:bg-white/30 text-white text-sm font-medium touch-manipulation"
            >
              Back to call
            </button>
            <button
              type="button"
              onClick={endCall}
              className="min-w-[52px] min-h-[52px] rounded-full bg-red-500 hover:bg-red-600 flex items-center justify-center text-white touch-manipulation"
              aria-label="End call"
            >
              <PhoneOff size={24} />
            </button>
          </div>
        </div>
      )}

      {/* Screen share: minimized floating bar – tap to expand */}
      {showScreenShareMinimizedBar && (
        <div
          className="fixed left-2 right-2 bottom-2 z-[65] flex items-center gap-2 sm:left-4 sm:right-4 sm:bottom-4 rounded-xl overflow-hidden shadow-lg border border-amber-500/50 bg-gray-900 min-h-[64px] sm:min-h-[72px]"
          style={{ paddingBottom: 'max(0.25rem, env(safe-area-inset-bottom))', paddingLeft: 'max(0.5rem, env(safe-area-inset-left))', paddingRight: 'max(0.5rem, env(safe-area-inset-right))' }}
          role="button"
          tabIndex={0}
          onClick={() => setScreenShareView('fullpage')}
          onKeyDown={(e) => e.key === 'Enter' && setScreenShareView('fullpage')}
          aria-label="Expand screen share"
        >
          <div className="w-20 h-12 sm:w-24 sm:h-14 shrink-0 rounded-lg overflow-hidden bg-black flex items-center justify-center">
            <video ref={minimizedScreenRef} autoPlay playsInline muted className="w-full h-full object-contain" />
          </div>
          <div className="min-w-0 flex-1 py-2">
            <p className="font-semibold text-white text-sm truncate">{otherName}&apos;s screen</p>
            <p className="text-amber-400 text-xs">Tap to expand</p>
          </div>
          <div className="flex items-center gap-1.5 shrink-0 pr-1">
            <button
              type="button"
              onClick={(e) => { e.stopPropagation(); setScreenShareView('fullpage'); }}
              className="w-10 h-10 sm:w-11 sm:h-11 rounded-full bg-white/20 hover:bg-white/30 flex items-center justify-center text-white touch-manipulation"
              title="Expand"
              aria-label="Expand screen share"
            >
              <Maximize2 size={18} />
            </button>
            <button
              type="button"
              onClick={(e) => { e.stopPropagation(); endCall(); }}
              className="w-10 h-10 sm:w-11 sm:h-11 rounded-full bg-red-500 hover:bg-red-600 flex items-center justify-center text-white touch-manipulation"
              title="End call"
              aria-label="End call"
            >
              <PhoneOff size={18} />
            </button>
          </div>
        </div>
      )}

      {/* Minimized bar: visible when minimized; responsive + safe area on mobile */}
      {showMinimizedBar && !showScreenShareMinimizedBar && (
        <div
          className="fixed left-2 right-2 bottom-2 z-[60] flex items-center justify-between gap-2 sm:left-4 sm:right-4 sm:bottom-4 sm:gap-3 px-3 py-2.5 sm:px-4 sm:py-3 rounded-xl shadow-lg border border-gray-200 dark:border-gray-700 bg-white dark:bg-gray-900 min-h-[56px] sm:min-h-0"
          style={{ paddingBottom: 'max(0.5rem, env(safe-area-inset-bottom))' }}
          role="button"
          tabIndex={0}
          onClick={() => setMinimized(false)}
          onKeyDown={(e) => e.key === 'Enter' && setMinimized(false)}
          aria-label="Expand call"
        >
          <div className="flex items-center gap-2 sm:gap-3 min-w-0 flex-1">
            <div className="w-10 h-10 sm:w-10 sm:h-10 min-w-[40px] min-h-[40px] rounded-full bg-[var(--volera-accent)]/15 flex items-center justify-center shrink-0 overflow-hidden">
              {otherPartyProfilePicture ? (
                <img src={otherPartyProfilePicture} alt={otherName ?? ''} className="w-full h-full object-cover" />
              ) : (
                <span className="text-base sm:text-lg font-bold text-[var(--volera-accent)]">
                  {getInitials(otherName)}
                </span>
              )}
            </div>
            <div className="min-w-0 flex-1">
              <p className="font-semibold text-gray-900 dark:text-white truncate text-sm sm:text-base">{otherName || 'Call'}</p>
              <p className="text-xs text-gray-500 dark:text-gray-400 truncate">{callLabel} · Tap to expand</p>
            </div>
          </div>
          <div className="flex items-center gap-1.5 sm:gap-2 shrink-0">
            <button
              type="button"
              onClick={(e) => { e.stopPropagation(); setMinimized(false); }}
              className="w-11 h-11 sm:w-10 sm:h-10 min-w-[44px] min-h-[44px] rounded-full bg-gray-200 dark:bg-gray-700 flex items-center justify-center text-gray-700 dark:text-gray-200 hover:bg-gray-300 dark:hover:bg-gray-600 transition-colors active:scale-95"
              title="Expand call"
              aria-label="Expand call"
            >
              <Maximize2 size={18} />
            </button>
            <button
              type="button"
              onClick={(e) => { e.stopPropagation(); endCall(); }}
              className="w-11 h-11 sm:w-10 sm:h-10 min-w-[44px] min-h-[44px] rounded-full bg-red-500 hover:bg-red-600 flex items-center justify-center text-white transition-colors active:scale-95"
              title="End call"
              aria-label="End call"
            >
              <PhoneOff size={18} />
            </button>
          </div>
        </div>
      )}

      {/* Full call UI: hidden when call or screen share is minimized; full viewport on mobile with safe areas, padded on tablet/desktop */}
      <div
        className={clsx(
          "fixed inset-0 z-50 flex items-center justify-center bg-black/80 backdrop-blur-sm transition-all pt-[env(safe-area-inset-top)] pr-[env(safe-area-inset-right)] pb-[env(safe-area-inset-bottom)] pl-[env(safe-area-inset-left)] sm:p-3 md:p-4",
          (showMinimizedBar || showScreenShareMinimizedBar) && "invisible pointer-events-none"
        )}
      >
      <div
          className={clsx(
            "bg-white dark:bg-gray-900 shadow-2xl overflow-hidden flex flex-col items-center relative transition-all duration-300 w-full h-full sm:h-auto",
            isVideo
              ? "sm:max-w-3xl sm:h-[75vh] sm:rounded-2xl sm:max-h-[85vh] md:max-w-4xl md:h-[80vh] lg:max-w-5xl bg-gray-900"
              : "sm:max-w-sm sm:h-auto sm:rounded-2xl sm:p-4 md:max-w-md md:p-6 p-4 min-h-0"
          )}
          style={!isVideo ? {
            paddingTop: 'max(1rem, env(safe-area-inset-top))',
            paddingBottom: 'max(1rem, env(safe-area-inset-bottom))',
            paddingLeft: 'max(1rem, env(safe-area-inset-left))',
            paddingRight: 'max(1rem, env(safe-area-inset-right))',
          } : undefined}
      >
        
        {/* Hidden Audio Elements (Always needed for audio) — playsInline helps mobile WebViews */}
        <audio ref={localAudioRef} autoPlay muted playsInline />
        <audio ref={remoteAudioRef} autoPlay playsInline />

        {!mediaAvailable && (status === 'incoming' || status === 'calling' || status === 'connected') && (
          <div className="w-full mb-3 px-3 py-2.5 rounded-xl bg-amber-500/15 border border-amber-500/40 text-amber-900 dark:text-amber-100 text-xs sm:text-sm text-center space-y-2">
            <p>{insecureMediaMessage()}</p>
            <a
              href={suggestedHttpsOrigin() + window.location.pathname + window.location.search}
              className="inline-flex items-center justify-center px-3 py-1.5 rounded-lg bg-amber-500 text-black font-semibold touch-manipulation"
            >
              Open secure app
            </a>
          </div>
        )}

        {/* Video Mode UI */}
        {isVideo ? (
            <div className="relative w-full h-full flex items-center justify-center bg-gray-900">
                {/* Remote Video or Shared Screen (Main) – key forces refresh when stream switches */}
                <div className="absolute inset-0 w-full h-full flex flex-col">
                    {remoteDisplayStream && remoteDisplayStream.getVideoTracks().length > 0 ? (
                        <>
                            {remoteScreenShareUserId && (
                                <div className="absolute top-2 left-2 right-2 sm:top-3 sm:left-3 sm:right-auto z-10 flex flex-wrap items-center gap-2">
                                    <div className="flex items-center gap-2 px-2.5 py-1.5 sm:px-3 sm:py-2 rounded-lg bg-amber-500/90 text-black text-xs sm:text-sm font-semibold shadow-lg">
                                        <Monitor size={14} className="shrink-0" />
                                        <span>Shared screen</span>
                                    </div>
                                    <div className="flex items-center gap-1.5">
                                        <button
                                            type="button"
                                            onClick={(e) => { e.stopPropagation(); setScreenShareView('fullpage'); }}
                                            className="min-w-[36px] min-h-[36px] sm:min-w-[40px] sm:min-h-[40px] rounded-lg bg-black/50 hover:bg-black/70 flex items-center justify-center text-white touch-manipulation"
                                            title="Full page"
                                            aria-label="View screen share full page"
                                        >
                                            <Maximize2 size={16} />
                                        </button>
                                        <button
                                            type="button"
                                            onClick={(e) => { e.stopPropagation(); setScreenShareView('minimized'); }}
                                            className="min-w-[36px] min-h-[36px] sm:min-w-[40px] sm:min-h-[40px] rounded-lg bg-black/50 hover:bg-black/70 flex items-center justify-center text-white touch-manipulation"
                                            title="Minimize"
                                            aria-label="Minimize screen share"
                                        >
                                            <Minimize2 size={16} />
                                        </button>
                                    </div>
                                </div>
                            )}
                            <video
                                key={`remote-${remoteScreenShareUserId ? 'screen' : 'video'}`}
                                ref={remoteVideoRef}
                                autoPlay
                                playsInline
                                className={clsx(
                                  'w-full h-full min-h-0',
                                  remoteScreenShareUserId ? 'object-contain' : 'object-cover'
                                )}
                            />
                        </>
                    ) : remoteStream ? (
                        <video
                            ref={remoteVideoRef}
                            autoPlay
                            playsInline
                            className="w-full h-full object-cover"
                        />
                    ) : (
                        <div className="flex-1 flex items-center justify-center">
                            <div className="text-white text-base sm:text-xl animate-pulse">
                                {status === 'connected' ? 'Waiting for video...' : 'Connecting...'}
                            </div>
                        </div>
                    )}
                </div>

                {/* Local Video (PIP) – responsive: small on mobile, larger on tablet/desktop */}
                <div className="absolute top-2 right-2 w-20 h-16 sm:top-4 sm:right-4 sm:w-36 sm:h-28 md:w-44 md:h-32 lg:w-48 lg:h-36 bg-black rounded-lg overflow-hidden shadow-lg border-2 border-gray-700 z-10">
                     {hasLocalVideoTrack && isVideoEnabled ? (
                       <video
                          ref={setLocalVideoRef}
                          autoPlay
                          muted
                          playsInline
                          className="w-full h-full object-cover transform scale-x-[-1]"
                       />
                     ) : (
                       <div className="w-full h-full flex items-center justify-center bg-gray-900">
                         <VideoOff className="text-gray-500" size={22} />
                       </div>
                     )}
                </div>

                {/* Call Info Overlay – compact on mobile, scales for tablet/desktop */}
                <div className="absolute top-2 left-2 right-2 sm:top-4 sm:left-4 sm:right-auto md:top-4 md:left-4 bg-black/40 px-3 py-1.5 sm:px-4 sm:py-2 rounded-lg backdrop-blur-sm max-w-[85%] sm:max-w-none">
                    <h3 className="text-white font-bold text-base sm:text-lg shadow-black drop-shadow-md truncate">
                        {isCaller ? receiverName : callerName}
                    </h3>
                    <p className="text-gray-300 text-xs sm:text-sm">
                        {status === 'calling' && 'Calling...'}
                        {status === 'incoming' && 'Incoming Video Call...'}
                        {status === 'connected' && 'Connected'}
                        {status === 'ended' && 'Call Ended'}
                    </p>
                    {status === 'connected' && isEncrypted && (
                        <div 
                            className="flex items-center gap-1.5 mt-1 text-teal-300 text-xs sm:text-sm font-medium animate-in fade-in duration-500 bg-black/20 px-2 py-0.5 rounded-full backdrop-blur-md"
                            title="Call media uses WebRTC DTLS-SRTP transport encryption between peers (or via TURN). This is not end-to-end encrypted messaging; the signaling server is not an E2EE message vault."
                        >
                            <Lock size={14} />
                            <span className="hidden sm:inline">Call media encrypted (DTLS-SRTP)</span>
                        </div>
                    )}
                    {status === 'connected' && isScreenSharing && (
                        <div className="flex items-center gap-1.5 mt-1 text-[var(--volera-accent)] text-xs sm:text-sm font-medium animate-in fade-in duration-500 bg-[var(--volera-accent)]/20 px-2 py-0.5 rounded-full backdrop-blur-md flex-wrap max-w-full">
                            <Monitor size={14} className="shrink-0" />
                            <span className="min-w-0 break-words">You&apos;re sharing your screen</span>
                          {isScreenShareWithSystemAudio && <span title="System audio on" className="shrink-0"><Volume2 size={14} /></span>}
                        </div>
                    )}
                    {status === 'connected' && remoteScreenShareUserId && !isScreenSharing && (
                        <div className="flex items-center gap-1.5 mt-1 text-amber-400 text-xs sm:text-sm font-medium animate-in fade-in duration-500 bg-amber-900/40 px-2 py-0.5 rounded-full backdrop-blur-md flex-wrap max-w-full">
                            <Monitor size={14} className="shrink-0" />
                            <span className="min-w-0 break-words">{otherName} is sharing their screen</span>
                        </div>
                    )}
                </div>
            </div>
        ) : (
            /* Audio Mode UI – responsive padding, flex layout for mobile */
            <div className="flex flex-1 min-h-0 flex-col items-center w-full">
                {/* Scrollable center content */}
                <div className="flex-1 min-h-0 overflow-y-auto flex flex-col items-center justify-center py-2 sm:py-4 w-full">
                {/* Avatar: profile picture or initials */}
                <div className="w-16 h-16 sm:w-24 sm:h-24 rounded-full bg-[var(--volera-accent)]/15 flex items-center justify-center mb-3 sm:mb-6 shrink-0 overflow-hidden">
                  {otherPartyProfilePicture ? (
                    <img src={otherPartyProfilePicture} alt={otherName ?? ''} className="w-full h-full object-cover" />
                  ) : (
                    <span className="text-2xl sm:text-3xl font-bold text-[var(--volera-accent)]">
                      {getInitials(otherName)}
                    </span>
                  )}
                </div>

                {/* Status Text */}
                <h3 className="text-lg sm:text-2xl font-bold text-gray-900 dark:text-white mb-1 sm:mb-2 truncate px-2 max-w-full text-center">
                  {isCaller ? receiverName : callerName}
                </h3>
                <p className="text-gray-500 dark:text-gray-400 mb-2 sm:mb-4 font-medium text-xs sm:text-base">
                  {status === 'calling' && 'Calling...'}
                  {status === 'incoming' && 'Incoming Call...'}
                  {status === 'connected' && 'Connected'}
                  {status === 'ended' && 'Call Ended'}
                </p>
                {status === 'connected' && isEncrypted && (
                    <div 
                        className="flex items-center justify-center gap-2 text-teal-800 dark:text-teal-300 bg-teal-100/80 dark:bg-teal-900/30 px-3 py-1 sm:px-4 sm:py-1.5 rounded-full text-xs sm:text-sm font-medium mb-4 sm:mb-6 animate-in fade-in duration-500 border border-teal-200 dark:border-teal-800"
                        title="Call media uses WebRTC DTLS-SRTP transport encryption between peers (or via TURN). This is not end-to-end encrypted messaging."
                    >
                        <Lock size={14} />
                        <span>Call media encrypted (DTLS-SRTP)</span>
                    </div>
                )}
                {status === 'connected' && isScreenSharing && (
                    <div className="flex items-center justify-center gap-2 text-[var(--volera-accent)] bg-[var(--volera-accent)]/15 px-3 py-1 sm:px-4 sm:py-1.5 rounded-full text-xs sm:text-sm font-medium mb-4 animate-in fade-in duration-500 border border-[var(--volera-accent)]/30">
                        <Monitor size={14} />
                        <span>You&apos;re sharing your screen</span>
                        {isScreenShareWithSystemAudio && <span title="System audio on"><Volume2 size={14} /></span>}
                    </div>
                )}
                {status === 'connected' && remoteScreenShareUserId && !isScreenSharing && (
                    <>
                        <div className="flex flex-wrap items-center justify-center gap-2 mb-2">
                            <div className="flex items-center gap-2 text-amber-600 dark:text-amber-400 bg-amber-100/80 dark:bg-amber-900/30 px-3 py-1 sm:px-4 sm:py-1.5 rounded-full text-xs sm:text-sm font-medium animate-in fade-in duration-500 border border-amber-200 dark:border-amber-800">
                                <Monitor size={14} />
                                <span>{otherName} is sharing their screen</span>
                            </div>
                            <div className="flex items-center gap-1.5">
                                <button
                                    type="button"
                                    onClick={() => setScreenShareView('fullpage')}
                                    className="min-w-[36px] min-h-[36px] rounded-lg bg-gray-200 dark:bg-gray-700 hover:bg-gray-300 dark:hover:bg-gray-600 flex items-center justify-center text-gray-700 dark:text-gray-200 touch-manipulation"
                                    title="Full page"
                                    aria-label="View screen share full page"
                                >
                                    <Maximize2 size={16} />
                                </button>
                                <button
                                    type="button"
                                    onClick={() => setScreenShareView('minimized')}
                                    className="min-w-[36px] min-h-[36px] rounded-lg bg-gray-200 dark:bg-gray-700 hover:bg-gray-300 dark:hover:bg-gray-600 flex items-center justify-center text-gray-700 dark:text-gray-200 touch-manipulation"
                                    title="Minimize"
                                    aria-label="Minimize screen share"
                                >
                                    <Minimize2 size={16} />
                                </button>
                            </div>
                        </div>
                        {voiceScreenStream && voiceScreenStream.getVideoTracks().length > 0 && (
                            <div className="w-full max-w-full mx-auto mb-4 px-0 sm:px-2 sm:max-w-md md:max-w-lg rounded-xl overflow-hidden border-2 border-amber-400/50 shadow-lg aspect-video bg-black min-h-[200px] sm:min-h-[240px] flex-1 max-h-[40vh] sm:max-h-[50vh]">
                                <video
                                    key="voice-remote-screen"
                                    ref={remoteScreenRef}
                                    autoPlay
                                    playsInline
                                    className="w-full h-full object-contain"
                                />
                            </div>
                        )}
                    </>
                )}
                {status === 'connected' && supportsSystemAudio() && supportsScreenShare && !isScreenSharing && (
                    <label className="flex items-center justify-center gap-2 text-gray-600 dark:text-gray-400 text-xs sm:text-sm mb-4 px-4 sm:px-0 cursor-pointer touch-manipulation">
                        <input
                          type="checkbox"
                          checked={includeSystemAudio}
                          onChange={(e) => setIncludeSystemAudio(e.target.checked)}
                          className="rounded shrink-0 w-4 h-4 min-w-[16px] min-h-[16px]"
                        />
                        <span className="text-center">Include system audio when sharing screen</span>
                    </label>
                )}
                </div>
            </div>
        )}

        {/* Controls (Shared) – touch-friendly on mobile, scales for tablet/desktop */}
        <div
          className={clsx(
            "flex flex-wrap items-center justify-center gap-1.5 sm:gap-4 md:gap-6 z-20 shrink-0",
            isVideo ? "absolute left-2 right-2 sm:left-1/2 sm:right-auto sm:-translate-x-1/2 bg-black/40 p-2 sm:p-3 md:p-4 rounded-xl sm:rounded-2xl backdrop-blur-md sm:bottom-6 md:bottom-8" : "mt-auto pt-3 sm:pt-4 gap-2 sm:gap-4"
          )}
          style={isVideo ? { bottom: 'max(0.75rem, env(safe-area-inset-bottom))' } : undefined}
        >
          {status === 'incoming' && (
            <>
              <button
                type="button"
                onClick={rejectCall}
                className="min-w-[56px] min-h-[56px] w-14 h-14 sm:min-w-[56px] sm:min-h-[56px] sm:w-14 sm:h-14 rounded-full bg-red-500 hover:bg-red-600 flex items-center justify-center text-white transition-transform hover:scale-110 active:scale-95 shadow-lg touch-manipulation"
                title="Decline"
                aria-label="Decline"
              >
                <PhoneOff size={20} className="sm:w-6 sm:h-6" />
              </button>
              <button
                type="button"
                onClick={(e) => { e.preventDefault(); void handleAccept(); }}
                disabled={isAcceptingUi}
                className="min-w-[56px] min-h-[56px] w-14 h-14 sm:min-w-[56px] sm:min-h-[56px] sm:w-14 sm:h-14 rounded-full bg-green-500 hover:bg-green-600 flex items-center justify-center text-white transition-transform hover:scale-110 active:scale-95 shadow-lg touch-manipulation disabled:opacity-70 ring-2 ring-green-300/50"
                title="Accept"
                aria-label="Accept"
              >
                {isVideo ? <Video size={20} className="sm:w-6 sm:h-6" /> : <PhoneIncoming size={20} className="sm:w-6 sm:h-6" />}
              </button>
            </>
          )}

          {(status === 'calling' || status === 'connected') && (
            <>
              {status === 'connected' && (
                <>
                    <button
                      type="button"
                      onClick={() => setMinimized(true)}
                      className={clsx(
                        "min-w-[48px] min-h-[48px] w-12 h-12 sm:min-w-[48px] sm:min-h-[48px] sm:w-12 sm:h-12 rounded-full flex items-center justify-center transition-all shadow-md touch-manipulation active:scale-95",
                        isVideo ? "bg-gray-700 hover:bg-gray-600 text-white" : "bg-gray-200 dark:bg-gray-700 text-gray-700 dark:text-gray-200 hover:bg-gray-300 dark:hover:bg-gray-600"
                      )}
                      title="Minimize – check chats while on call"
                      aria-label="Minimize call"
                    >
                      <Minimize2 size={20} />
                    </button>
                    <button
                      type="button"
                      onClick={handleToggleMute}
                      className={clsx(
                        "min-w-[48px] min-h-[48px] w-12 h-12 sm:min-w-[48px] sm:min-h-[48px] sm:w-12 sm:h-12 rounded-full flex items-center justify-center transition-all shadow-md touch-manipulation active:scale-95",
                        isMuted ? "bg-red-500 hover:bg-red-600 text-white" : (isVideo ? "bg-gray-700 hover:bg-gray-600 text-white" : "bg-gray-200 dark:bg-gray-700 text-gray-700 dark:text-gray-200 hover:bg-gray-300 dark:hover:bg-gray-600")
                      )}
                      title={isMuted ? "Unmute" : "Mute"}
                      aria-label={isMuted ? "Unmute" : "Mute"}
                    >
                      {isMuted ? <MicOff size={20} /> : <Mic size={20} />}
                    </button>
                    
                    {isVideo && (
                        <button
                          onClick={handleToggleVideo}
                          className={clsx(
                            "min-w-[40px] min-h-[40px] w-10 h-10 sm:min-w-[48px] sm:min-h-[48px] sm:w-12 sm:h-12 rounded-full flex items-center justify-center text-white transition-all shadow-md touch-manipulation active:scale-95",
                            !isVideoEnabled ? "bg-red-500 hover:bg-red-600" : "bg-gray-700 hover:bg-gray-600"
                          )}
                          title={isVideoEnabled ? "Turn Video Off" : "Turn Video On"}
                          aria-label={isVideoEnabled ? "Turn video off" : "Turn video on"}
                        >
                          {!isVideoEnabled ? <VideoOff size={20} /> : <Video size={20} />}
                        </button>
                    )}
                    {isScreenSharing ? (
                      supportsScreenShare && (
                        <button
                          onClick={stopScreenShare}
                          disabled={isScreenShareLoading}
                          className={clsx(
                            "min-w-[40px] min-h-[40px] w-10 h-10 sm:min-w-[48px] sm:min-h-[48px] sm:w-12 sm:h-12 rounded-full flex items-center justify-center text-white transition-all shadow-md touch-manipulation active:scale-95",
                            isScreenShareLoading ? "opacity-50 cursor-not-allowed" : "bg-[var(--volera-accent)] hover:bg-[var(--volera-accent-hover)]"
                          )}
                          title="Stop sharing screen"
                          aria-label="Stop sharing screen"
                        >
                          {isScreenShareLoading ? (
                            <span className="w-5 h-5 border-2 border-white border-t-transparent rounded-full animate-spin" />
                          ) : (
                            <MonitorOff size={20} />
                          )}
                        </button>
                      )
                    ) : (
                      <button
                        type="button"
                        onClick={() => {
                          if (!supportsScreenShare) {
                            useToastStore.getState().addToast(
                              'Screen sharing is not supported on this device. Use a desktop browser.',
                              'error'
                            );
                            return;
                          }
                          void startScreenShare(includeSystemAudio);
                        }}
                        disabled={isScreenShareLoading || !supportsScreenShare}
                        className={clsx(
                          "min-w-[40px] min-h-[40px] w-10 h-10 sm:min-w-[48px] sm:min-h-[48px] sm:w-12 sm:h-12 rounded-full flex items-center justify-center transition-all shadow-md touch-manipulation active:scale-95",
                          isScreenShareLoading || !supportsScreenShare ? "opacity-50 cursor-not-allowed" : (isVideo ? "bg-gray-700 hover:bg-gray-600 text-white" : "bg-gray-200 dark:bg-gray-700 text-gray-700 dark:text-gray-200 hover:bg-gray-300 dark:hover:bg-gray-600")
                        )}
                        title={supportsScreenShare ? "Share screen" : "Screen sharing is not supported on this device"}
                        aria-label={supportsScreenShare ? "Share screen" : "Screen sharing is not supported on this device"}
                      >
                        {isScreenShareLoading ? (
                          <span className="w-5 h-5 border-2 border-current border-t-transparent rounded-full animate-spin" />
                        ) : (
                          <Monitor size={20} />
                        )}
                      </button>
                    )}
                </>
              )}
              
              <button
                onClick={endCall}
                className="min-w-[48px] min-h-[48px] w-12 h-12 sm:min-w-[56px] sm:min-h-[56px] sm:w-14 sm:h-14 md:min-w-[60px] md:min-h-[60px] md:w-[60px] md:h-[60px] rounded-full bg-red-500 hover:bg-red-600 flex items-center justify-center text-white transition-transform hover:scale-110 active:scale-95 shadow-lg touch-manipulation"
                title="End Call"
                aria-label="End call"
              >
                <PhoneOff size={24} className="sm:w-7 sm:h-7" />
              </button>
            </>
          )}
        </div>
      </div>
    </div>
    </>
  );
};
