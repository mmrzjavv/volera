import { create } from 'zustand';
import { HubConnection, HubConnectionBuilder, LogLevel, HubConnectionState } from '@microsoft/signalr';
import { callService } from '../services/callService';
import { groupCallService } from '../services/groupCallService';
import { getApiBase } from '../services/api';
import type { WebRTCSignal } from '../types/call';
import { useAuthStore } from './useAuthStore';
import { useChatStore } from './useChatStore';
import { CALL_CONFIG, createBaseRtcConfig } from '../config/call';
import { useToastStore } from './useToastStore';
import { requestCallMedia } from '../utils/mediaPermissions';

const sameCallId = (a: string | null | undefined, b: string | null | undefined) =>
  !!a && !!b && a.toLowerCase() === b.toLowerCase();

interface CallState {
  callId: string | null;
  callerId: string | null;
  callerName: string | null;
  receiverId: string | null;
  receiverName: string | null;
  /** Profile picture URL of the other party (receiver when calling, caller when receiving) for UI. */
  otherPartyProfilePicture: string | null;
  status: 'idle' | 'calling' | 'incoming' | 'connected' | 'ended';
  isCaller: boolean;
  isVideo: boolean;
  isGroupCall: boolean;
  isEncrypted: boolean;
  isMinimized: boolean;
  localStream: MediaStream | null;
  remoteStream: MediaStream | null;
  /** When remote is sharing screen, this stream holds the screen track(s) for display. */
  remoteScreenStream: MediaStream | null;
  connection: HubConnection | null;
  isScreenSharing: boolean;
  isScreenShareLoading: boolean;
  remoteScreenShareUserId: string | null;
  isScreenShareWithSystemAudio: boolean;
  /** How the remote shared screen is displayed: inline (in call UI), fullpage, or minimized (floating bar). */
  screenShareView: 'inline' | 'fullpage' | 'minimized';
  isMuted: boolean;

  setMinimized: (minimized: boolean) => void;
  setScreenShareView: (view: 'inline' | 'fullpage' | 'minimized') => void;
  startScreenShare: (enableSystemAudio?: boolean) => Promise<void>;
  stopScreenShare: () => Promise<void>;
  initializeCallConnection: () => void;
  initiateCall: (receiverId: string, receiverName: string, isVideo?: boolean, receiverProfilePicture?: string | null) => Promise<void>;
  initiateGroupCall: (groupId: string, groupName: string, isVideo?: boolean, groupProfilePictureUrl?: string | null) => Promise<void>;
  acceptCall: () => Promise<void>;
  rejectCall: () => Promise<void>;
  endCall: () => Promise<void>;
  resetCall: () => void;
  toggleMute: () => void;
  toggleVideo: () => void;
  /** Set incoming call state from notification click (when app was in background and missed SignalR). */
  setIncomingFromNotification: (payload: { callId: string; callerId: string; callerName: string; receiverId: string; isVideo?: boolean; callerProfilePicture?: string | null }) => void;
}

export const useCallStore = create<CallState>((set, get) => {
  let peerConnection: RTCPeerConnection | null = null;
  let localStream: MediaStream | null = null;
  let screenShareStream: MediaStream | null = null;
  let iceCandidatesQueue: RTCIceCandidateInit[] = [];
  let isConnecting = false;
  let isAccepting = false;
  let offerInFlight = false;
  let makingOffer = false;
  let rtcConfig: RTCConfiguration = createBaseRtcConfig([]);
  let iceConfigLoaded = false;

  const loadIceConfig = async (force = false) => {
    if (iceConfigLoaded && !force) return;
    try {
      const servers = await callService.getIceServers();
      rtcConfig = createBaseRtcConfig(
        servers.map((s) => ({
          urls: s.urls,
          ...(s.username ? { username: s.username } : {}),
          ...(s.credential ? { credential: s.credential } : {}),
        }))
      );
      iceConfigLoaded = true;
      if (!servers.length) {
        console.warn('ICE servers empty — same-LAN host candidates only. Set TURN_PUBLIC_HOST for cross-network calls.');
      }
    } catch (err) {
      // Stay on host-only ICE — do not fall back to public Google/Twilio STUN
      // (unreachable on internal networks without international internet).
      console.warn('Failed to load ICE servers; using host candidates only', err);
      rtcConfig = createBaseRtcConfig([]);
      iceConfigLoaded = true;
    }
  };

  const cleanupMedia = () => {
    if (screenShareStream) {
      screenShareStream.getTracks().forEach(track => track.stop());
      screenShareStream = null;
    }
    if (localStream) {
      localStream.getTracks().forEach(track => track.stop());
      localStream = null;
    }
    if (peerConnection) {
      peerConnection.close();
      peerConnection = null;
    }
    iceCandidatesQueue = [];
    isAccepting = false;
    offerInFlight = false;
    makingOffer = false;
    set({
      localStream: null,
      remoteStream: null,
      remoteScreenStream: null,
      isEncrypted: false,
      isScreenSharing: false,
      isScreenShareLoading: false,
      remoteScreenShareUserId: null,
      isScreenShareWithSystemAudio: false,
      screenShareView: 'inline',
      isMuted: false,
    });
  };

  const flushIceQueue = async (pc: RTCPeerConnection) => {
    while (iceCandidatesQueue.length) {
      const candidate = iceCandidatesQueue.shift();
      if (candidate) {
        try {
          await pc.addIceCandidate(candidate);
        } catch (e) {
          console.error('Error adding queued ice candidate', e);
        }
      }
    }
  };

  const createAndSendOffer = async (hub: HubConnection, callId: string) => {
    if (!peerConnection || offerInFlight) return;
    offerInFlight = true;
    makingOffer = true;
    try {
      const offer = await peerConnection.createOffer();
      // Aborted / replaced while awaiting
      if (!peerConnection || peerConnection.signalingState === 'closed') return;
      await peerConnection.setLocalDescription(offer);
      const signal: WebRTCSignal = { type: 'offer', data: offer };
      await hub.invoke('SendSignal', callId, JSON.stringify(signal));
    } finally {
      makingOffer = false;
      offerInFlight = false;
    }
  };

  /** Must be the first await in Accept / Call taps on mobile or the OS denies without prompting. */
  const ensureLocalMedia = async () => {
    if (localStream) return localStream;
    const { isVideo } = get();
    const result = await requestCallMedia(isVideo);
    if (!result.ok) {
      useToastStore.getState().addToast(result.message, 'error');
      throw new Error(result.message);
    }
    localStream = result.stream;
    set({ localStream });
    return localStream;
  };

  const createPeerConnection = async () => {
    if (peerConnection) return peerConnection;

    // Media first (gesture), ICE config second — never reverse on mobile
    await ensureLocalMedia();
    await loadIceConfig();

    // WebRTC uses DTLS-SRTP for media in transit. TURN relays can still see media metadata
    // and this is NOT application-layer end-to-end encryption for chat messages.
    peerConnection = new RTCPeerConnection(rtcConfig);

    peerConnection.onconnectionstatechange = () => {
      const state = peerConnection?.connectionState;
      if (state === 'connected') {
        set({ isEncrypted: true });
      } else if (state === 'disconnected' || state === 'failed' || state === 'closed') {
        set({ isEncrypted: false });
      }
    };

    peerConnection.oniceconnectionstatechange = () => {
      const state = peerConnection?.iceConnectionState;
      if (state === 'failed' && peerConnection) {
        try {
          peerConnection.restartIce();
        } catch (e) {
          console.error('ICE restart failed', e);
          useToastStore.getState().addToast(
            'Connection failed. Check that both devices can reach the network (or TURN is configured).',
            'error'
          );
        }
      }
    };

    peerConnection.onicecandidate = (event) => {
      if (event.candidate) {
        const { connection, callId } = get();
        if (connection && callId) {
          const signal: WebRTCSignal = { type: 'ice-candidate', data: event.candidate };
          connection.invoke('SendSignal', callId, JSON.stringify(signal));
        }
      }
    };

    peerConnection.ontrack = (event) => {
      const track = event.track;
      // Unmute incoming tracks — some mobiles deliver tracks disabled until played
      track.enabled = true;

      let stream = event.streams && event.streams[0];
      if (!stream) {
        stream = new MediaStream([track]);
      } else if (!stream.getTracks().includes(track)) {
        stream.addTrack(track);
      }
      const { remoteStream: existing, remoteScreenShareUserId: screenUserId } = get();
      // When remote is screen-sharing, keep a dedicated stream for the display surface.
      if (track.kind === 'video' && screenUserId) {
        const screenStream = get().remoteScreenStream;
        if (screenStream) {
          // Replace prior screen video track so renegotiation updates the UI
          screenStream.getVideoTracks().forEach((t) => {
            if (t.id !== track.id) {
              screenStream.removeTrack(t);
              t.stop();
            }
          });
          if (!screenStream.getTracks().includes(track)) screenStream.addTrack(track);
          set({ remoteScreenStream: new MediaStream(screenStream.getTracks()) });
        } else {
          set({ remoteScreenStream: new MediaStream([track]) });
        }
      }
      if (existing && existing !== stream) {
        if (!existing.getTracks().includes(track)) existing.addTrack(track);
        set({ remoteStream: existing });
        return;
      }
      set({ remoteStream: stream });
    };

    if (localStream) {
      const { isMuted } = get();
      localStream.getTracks().forEach(track => {
        if (track.kind === 'audio') track.enabled = !isMuted;
        if (localStream && peerConnection) {
          peerConnection.addTrack(track, localStream);
        }
      });
    }

    return peerConnection;
  };

  return {
    callId: null,
    callerId: null,
    callerName: null,
    receiverId: null,
    receiverName: null,
    otherPartyProfilePicture: null,
    status: 'idle',
    isCaller: false,
    isVideo: false,
    isGroupCall: false,
    isEncrypted: false,
    isMinimized: false,
    localStream: null,
    remoteStream: null,
    remoteScreenStream: null,
    connection: null,
    isScreenSharing: false,
    isScreenShareLoading: false,
    remoteScreenShareUserId: null,
    isScreenShareWithSystemAudio: false,
    screenShareView: 'inline',
    isMuted: false,

    setMinimized: (minimized) => set({ isMinimized: minimized }),
    setScreenShareView: (view) => set({ screenShareView: view }),

    startScreenShare: async (enableSystemAudio = false) => {
      const { status, callId, connection, isScreenSharing, isScreenShareLoading } = get();
      if (status !== 'connected' || !callId || !connection || !peerConnection) return;
      if (isScreenSharing || isScreenShareLoading) return;

      set({ isScreenShareLoading: true });
      try {
        const wantSystemAudio = CALL_CONFIG.enableSystemAudioSharing && enableSystemAudio;
        const opts: DisplayMediaStreamOptions & { systemAudio?: string } = {
          video: true,
          audio: wantSystemAudio,
          ...(wantSystemAudio && { systemAudio: 'include' }),
        };
        const stream = await navigator.mediaDevices.getDisplayMedia(opts);
        screenShareStream = stream;
        const videoTrack = stream.getVideoTracks()[0];
        const senders = peerConnection.getSenders();
        const videoSender = senders.find(s => s.track?.kind === 'video');

        if (videoSender) {
          await videoSender.replaceTrack(videoTrack);
        } else {
          peerConnection.addTrack(videoTrack, stream);
        }

        const screenAudioTrack = stream.getAudioTracks()[0];
        if (screenAudioTrack) {
          peerConnection.addTrack(screenAudioTrack, stream);
        }

        const offer = await peerConnection.createOffer();
        await peerConnection.setLocalDescription(offer);
        await connection.invoke('SendSignal', callId, JSON.stringify({ type: 'offer', data: offer }));
        await connection.invoke('SendScreenShareStarted', callId);
        if (CALL_CONFIG.enableSystemAudioSharing && enableSystemAudio && screenAudioTrack) {
          await connection.invoke('SendScreenShareAudioEnabled', callId, true);
        }

        set({
          isScreenSharing: true,
          isScreenShareLoading: false,
          isScreenShareWithSystemAudio: !!screenAudioTrack,
        });

        videoTrack.onended = () => {
          get().stopScreenShare();
        };
      } catch (e: unknown) {
        const err = e as { name?: string; message?: string };
        if (err?.name === 'NotAllowedError') {
          useToastStore.getState().addToast('Screen share cancelled or permission denied', 'info');
        } else if (err?.name === 'NotSupportedError' || !navigator.mediaDevices?.getDisplayMedia) {
          useToastStore.getState().addToast(
            'Screen sharing is not supported in this browser. Use desktop Chrome/Edge/Firefox.',
            'error'
          );
        } else {
          console.error('Screen share error:', e);
          useToastStore.getState().addToast('Could not start screen share. Try again.', 'error');
        }
        set({ isScreenShareLoading: false });
      }
    },

    stopScreenShare: async () => {
      const { status, callId, connection, isScreenSharing, isScreenShareLoading, isVideo } = get();
      if (status !== 'connected' || !callId || !connection || !peerConnection) return;
      if (!isScreenSharing || isScreenShareLoading) return;

      set({ isScreenShareLoading: true });
      try {
        const senders = peerConnection.getSenders();
        const cameraVideoTrack = localStream?.getVideoTracks()[0];

        const videoSender = senders.find(s => s.track?.kind === 'video');
        if (videoSender) {
          if (cameraVideoTrack && isVideo) {
            await videoSender.replaceTrack(cameraVideoTrack);
          } else {
            peerConnection.removeTrack(videoSender);
          }
        }

        const screenAudioTrack = screenShareStream?.getAudioTracks()[0];
        if (screenAudioTrack) {
          const screenAudioSender = senders.find(s => s.track?.id === screenAudioTrack.id);
          if (screenAudioSender) peerConnection.removeTrack(screenAudioSender);
        }

        if (screenShareStream) {
          screenShareStream.getTracks().forEach(track => track.stop());
          screenShareStream = null;
        }

        const offer = await peerConnection.createOffer();
        await peerConnection.setLocalDescription(offer);
        await connection.invoke('SendSignal', callId, JSON.stringify({ type: 'offer', data: offer }));
        await connection.invoke('SendScreenShareStopped', callId);

        set({
          isScreenSharing: false,
          isScreenShareLoading: false,
          isScreenShareWithSystemAudio: false,
        });
      } catch (e) {
        console.error('Stop screen share error:', e);
        set({ isScreenShareLoading: false });
      }
    },

    setIncomingFromNotification: (payload) => set({
      status: 'incoming',
      callId: payload.callId,
      callerId: payload.callerId,
      callerName: payload.callerName,
      receiverId: payload.receiverId,
      otherPartyProfilePicture: payload.callerProfilePicture ?? null,
      isCaller: false,
      isVideo: payload.isVideo === true,
      isGroupCall: false
    }),

    initializeCallConnection: () => {
      const token = localStorage.getItem('token');
      if (!token) return;

      void loadIceConfig();

      // If connection exists but is disconnected, restart it.
      const existingConnection = get().connection;
      if (existingConnection) {
        if (existingConnection.state === 'Disconnected') {
          if (isConnecting) return;
          isConnecting = true;
          existingConnection.start()
            .then(() => console.log('CallHub Reconnected'))
            .catch(err => console.error('CallHub Reconnection Error: ', err))
            .finally(() => { isConnecting = false; });
        }
        return;
      }

      const connection = new HubConnectionBuilder()
        .withUrl(`${getApiBase() || window.location.origin}/callHub`, {
          accessTokenFactory: () => token
        })
        .withAutomaticReconnect()
        .configureLogging({
          log: (logLevel: LogLevel, message: string) => {
            // Suppress "stopped during negotiation" error which is common in React StrictMode
            if (logLevel === LogLevel.Error && message.includes('stopped during negotiation')) {
              return;
            }
            if (logLevel >= LogLevel.Information) {
              console.log(`[${new Date().toISOString()}] ${LogLevel[logLevel]}: ${message}`);
            }
          }
        })
        .build();

      connection.on('CallInitiated', (data: any) => {
        const currentUser = useAuthStore.getState().user;

        console.log('Received CallInitiated:', data);

        if (currentUser && data.receiverId && currentUser.id &&
            data.receiverId.toLowerCase() === currentUser.id.toLowerCase()) {
          console.log('Call is for me, setting incoming state');
          set({
            status: 'incoming',
            callId: data.callId,
            callerId: data.callerId,
            callerName: data.callerName,
            isCaller: false,
            isVideo: data.isVideo || data.IsVideo || false
          });
        } else {
          console.log('Call ignored - Receiver mismatch or user not logged in:', data.receiverId, currentUser?.id);
        }
      });

      connection.on('GroupCallInitiated', (data: any) => {
        const currentUser = useAuthStore.getState().user;
        console.log('Received GroupCallInitiated:', data);

        if (!currentUser?.id) {
          console.log('Group call ignored - user not logged in');
          return;
        }

        if (data.initiatorId && data.initiatorId.toLowerCase?.() === currentUser.id.toLowerCase()) {
          console.log('Group call event is for initiator; ignoring on this client.');
          return;
        }

        const { groups } = useChatStore.getState();
        const group = groups.find(g => g.id === data.groupId);
        const groupName = group?.name || 'Group Call';

        set({
          status: 'incoming',
          callId: data.groupCallId,
          callerId: data.initiatorId,
          callerName: data.initiatorName || 'Unknown',
          receiverId: data.groupId,
          receiverName: groupName,
          isCaller: false,
          isVideo: data.isVideo || false,
          isGroupCall: true
        });
      });

      connection.on('CallAccepted', async (data: any) => {
        const { isCaller, callId } = get();
        const acceptedId = data.callId ?? data.CallId;
        if (!sameCallId(callId, acceptedId)) return;

        set({ status: 'connected' });

        // Caller starts WebRTC negotiation once (CallAccepted may arrive via User + Group + connections).
        // Callee waits for the offer.
        if (isCaller && peerConnection == null && !offerInFlight) {
          offerInFlight = true;
          try {
            await createPeerConnection();
            // createAndSendOffer also guards offerInFlight — clear so it can send
            offerInFlight = false;
            if (peerConnection && callId) {
              await createAndSendOffer(connection, callId);
            }
          } catch (e) {
            offerInFlight = false;
            console.error('Failed to create offer after CallAccepted', e);
          }
        }
      });

      connection.on('CallRejected', (data: any) => {
        const { callId } = get();
        if (sameCallId(callId, data.callId ?? data.CallId)) {
          cleanupMedia();
          set({ status: 'ended' });
          setTimeout(() => get().resetCall(), 2000);
        }
      });

      connection.on('CallEnded', (data: any) => {
        const { callId } = get();
        if (sameCallId(callId, data.callId ?? data.CallId)) {
          cleanupMedia();
          set({ status: 'ended', remoteScreenShareUserId: null, remoteScreenStream: null, screenShareView: 'inline' });
          setTimeout(() => get().resetCall(), 2000);
        }
      });

      connection.on('GroupCallEnded', (data: any) => {
        const { callId } = get();
        if (sameCallId(callId, data.groupCallId ?? data.GroupCallId)) {
          cleanupMedia();
          set({ status: 'ended', remoteScreenShareUserId: null, remoteScreenStream: null, screenShareView: 'inline' });
          setTimeout(() => get().resetCall(), 2000);
        }
      });

      connection.on('ScreenShareStarted', (data: { callId?: string; userId: string }) => {
        const currentUser = useAuthStore.getState().user;
        // Hub used to broadcast to the whole group including the sharer — ignore self.
        if (currentUser?.id && data.userId?.toLowerCase() === currentUser.id.toLowerCase()) {
          return;
        }
        const { remoteStream: rs } = get();
        const videoTracks = rs?.getVideoTracks() ?? [];
        const remoteScreenStream = videoTracks.length > 0 ? new MediaStream([...videoTracks]) : null;
        set({ remoteScreenShareUserId: data.userId, remoteScreenStream });
      });

      connection.on('ScreenShareStopped', (data: { callId?: string; userId: string }) => {
        const { remoteScreenShareUserId } = get();
        if (remoteScreenShareUserId === data.userId) {
          set({ remoteScreenShareUserId: null, remoteScreenStream: null, screenShareView: 'inline' });
        }
      });

      connection.on('GroupCallParticipantJoined', async (data: any) => {
        const { isCaller, callId, status, isGroupCall } = get();

        if (!isCaller || !isGroupCall) return;
        if (!sameCallId(callId, data.groupCallId ?? data.GroupCallId)) return;

        if (status === 'calling' || status === 'connected') {
          set({ status: 'connected' });

          try {
            await createPeerConnection();
            if (peerConnection && callId && !offerInFlight) {
              await createAndSendOffer(connection, callId);
            }
          } catch (e) {
            console.error('Failed to create group-call offer', e);
          }
        }
      });

      connection.on('UserOnline', (userId: string) => {
        useChatStore.getState().updateUserStatus(userId, true);
      });

      connection.on('UserOffline', (userId: string) => {
        useChatStore.getState().updateUserStatus(userId, false);
      });

      connection.on('ReceiveSignal', async (dataStr: any) => {
        const signal: WebRTCSignal = typeof dataStr === 'string' ? JSON.parse(dataStr) : dataStr;

        if (!peerConnection && (signal.type === 'offer' || signal.type === 'answer')) {
          try {
            await createPeerConnection();
          } catch (e) {
            console.error('Failed to create peer connection for signal', e);
            return;
          }
        }

        if (!peerConnection) {
          if (signal.type === 'ice-candidate') {
            iceCandidatesQueue.push(signal.data);
            return;
          }
          console.warn('Received signal but no peer connection exists (and not an offer):', signal.type);
          return;
        }

        if (signal.type === 'offer') {
          // Perfect negotiation: callee is the polite peer and rolls back on glare.
          // Screen-share renegotiation also arrives as offers while connected.
          const polite = !get().isCaller;
          const offerCollision =
            makingOffer || peerConnection.signalingState !== 'stable';

          if (!polite && offerCollision) {
            console.warn('Ignoring glare offer (impolite peer)');
            return;
          }

          try {
            if (offerCollision) {
              await Promise.all([
                peerConnection.setLocalDescription({ type: 'rollback' }),
                peerConnection.setRemoteDescription(new RTCSessionDescription(signal.data)),
              ]);
            } else {
              await peerConnection.setRemoteDescription(new RTCSessionDescription(signal.data));
            }

            await flushIceQueue(peerConnection);
            const answer = await peerConnection.createAnswer();
            await peerConnection.setLocalDescription(answer);

            const { connection: hub, callId } = get();
            if (hub && callId) {
              const answerSignal: WebRTCSignal = { type: 'answer', data: answer };
              await hub.invoke('SendSignal', callId, JSON.stringify(answerSignal));
            }
            set({ status: 'connected' });
          } catch (e) {
            console.error('Error handling remote offer', e);
          }
        } else if (signal.type === 'answer') {
          try {
            if (peerConnection.signalingState === 'stable' && peerConnection.currentRemoteDescription) {
              return;
            }
            await peerConnection.setRemoteDescription(new RTCSessionDescription(signal.data));
            await flushIceQueue(peerConnection);
          } catch (e) {
            console.error('Error handling remote answer', e);
          }
        } else if (signal.type === 'ice-candidate') {
          try {
            if (!peerConnection.remoteDescription) {
              iceCandidatesQueue.push(signal.data);
            } else {
              await peerConnection.addIceCandidate(signal.data);
            }
          } catch (e) {
            console.error('Error adding ice candidate', e);
          }
        }
      });

      const startConnection = async () => {
        if (connection.state !== HubConnectionState.Disconnected || isConnecting) return;
        isConnecting = true;
        try {
          await connection.start();
          console.log('CallHub Connected');
        } catch (err: any) {
          if (err.message && (err.message.includes('AbortError') || err.message.includes('negotiation'))) {
            return;
          }
          console.error('CallHub Connection Error: ', err);
          setTimeout(startConnection, 5000);
        } finally {
          isConnecting = false;
        }
      };

      startConnection();

      set({ connection });
    },

    initiateCall: async (receiverId, receiverName, isVideo = false, receiverProfilePicture = null) => {
      try {
        const { connection } = get();
        // Acquire mic/camera during the Call tap (mobile gesture requirement) BEFORE any other await
        set({ isVideo });
        await ensureLocalMedia();
        void loadIceConfig();
        const response = await callService.initiate(receiverId, isVideo);
        if (!response?.callId) {
          throw new Error('No callId returned');
        }
        set({
          status: 'calling',
          callId: response.callId,
          isCaller: true,
          receiverId,
          receiverName: receiverName,
          otherPartyProfilePicture: receiverProfilePicture ?? null,
          isVideo: isVideo
        });

        if (connection) {
          await connection.invoke('JoinCallGroup', response.callId);
        }
      } catch (error) {
        console.error('Failed to initiate call', error);
        const msg = error instanceof Error ? error.message : '';
        if (!msg || !/microphone|HTTPS|blocked|camera/i.test(msg)) {
          useToastStore.getState().addToast('Failed to start call. Please try again.', 'error');
        }
        cleanupMedia();
        set({ status: 'idle' });
      }
    },

    initiateGroupCall: async (groupId, groupName, isVideo = false, groupProfilePictureUrl = null) => {
      try {
        const { connection } = get();
        set({ isVideo });
        await ensureLocalMedia();
        void loadIceConfig();
        const response = await groupCallService.initiate(groupId, isVideo);
        if (!response?.groupCallId) {
          throw new Error('No groupCallId returned');
        }
        set({
          status: 'calling',
          callId: response.groupCallId,
          isCaller: true,
          receiverId: groupId,
          receiverName: groupName,
          otherPartyProfilePicture: groupProfilePictureUrl ?? null,
          isVideo: isVideo,
          isGroupCall: true
        });

        if (connection) {
          await connection.invoke('JoinCallGroup', response.groupCallId);
        }
      } catch (error) {
        console.error('Failed to initiate group call', error);
        const msg = error instanceof Error ? error.message : '';
        if (!msg || !/microphone|HTTPS|blocked|camera/i.test(msg)) {
          useToastStore.getState().addToast('Failed to start group call. Please try again.', 'error');
        }
        cleanupMedia();
        set({ status: 'idle' });
      }
    },

    acceptCall: async () => {
      const { callId, connection, isGroupCall, callerId, status } = get();
      if (!callId || status !== 'incoming' || isAccepting) return;

      isAccepting = true;
      // Leave Incoming UI immediately so a second click cannot POST accept again
      set({ status: 'connected' });

      try {
        // FIRST await must be getUserMedia (inside createPeerConnection → ensureLocalMedia)
        await createPeerConnection();

        if (connection) {
          await connection.invoke('JoinCallGroup', callId);
        }

        if (isGroupCall) {
          await groupCallService.join(callId);

          const currentUser = useAuthStore.getState().user;
          if (connection && callerId && currentUser?.id) {
            try {
              await connection.invoke(
                'SendCallAccepted',
                callId,
                callerId,
                currentUser.id
              );
            } catch (signalErr) {
              console.error("Failed to send group call 'accepted' signal", signalErr);
            }
          }
        } else {
          await callService.accept(callId);
        }
      } catch (error) {
        console.error('Failed to accept call', error);
        const msg = error instanceof Error ? error.message : '';
        if (!msg || !/microphone|HTTPS|blocked|camera/i.test(msg)) {
          useToastStore.getState().addToast('Failed to accept call. Please try again.', 'error');
        }
        cleanupMedia();
        set({ status: 'incoming' });
      } finally {
        isAccepting = false;
      }
    },

    rejectCall: async () => {
      try {
        const { callId, isGroupCall } = get();
        if (callId) {
          if (isGroupCall) {
            // For now, simply end our local state for group calls.
          } else {
            await callService.reject(callId);
          }
        }
        get().resetCall();
      } catch (error) {
        console.error('Failed to reject call', error);
      }
    },

    endCall: async () => {
      try {
        const { callId, isGroupCall } = get();
        if (callId) {
          if (isGroupCall) {
            await groupCallService.end(callId);
          } else {
            await callService.end(callId);
          }
        }
        cleanupMedia();
        get().resetCall();
      } catch (error) {
        console.error('Failed to end call', error);
      }
    },

    resetCall: () => {
      cleanupMedia();
      set({
        callId: null,
        callerId: null,
        callerName: null,
        receiverId: null,
        receiverName: null,
        otherPartyProfilePicture: null,
        status: 'idle',
        isCaller: false,
        isVideo: false,
        isGroupCall: false,
        isEncrypted: false,
        isMinimized: false,
        localStream: null,
        remoteStream: null,
        remoteScreenStream: null,
        isScreenSharing: false,
        isScreenShareLoading: false,
        remoteScreenShareUserId: null,
        isScreenShareWithSystemAudio: false,
        screenShareView: 'inline',
        isMuted: false,
      });
    },

    toggleMute: () => {
      const { localStream, isMuted } = get();
      const nextMuted = !isMuted;
      localStream?.getAudioTracks().forEach(track => {
        track.enabled = !nextMuted;
      });
      peerConnection?.getSenders().forEach(sender => {
        if (sender.track?.kind === 'audio') {
          sender.track.enabled = !nextMuted;
        }
      });
      set({ isMuted: nextMuted });
    },

    toggleVideo: () => {
      const { localStream } = get();
      if (localStream) {
        localStream.getVideoTracks().forEach(track => {
          track.enabled = !track.enabled;
        });
      }
    }
  };
});
