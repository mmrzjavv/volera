'use client';

import { useEffect, useRef, useCallback, useState } from 'react';
import * as signalR from '@microsoft/signalr';
import type { BranchMessagePayload } from '@/api/support';
import type { MessageReactionDto } from '@/api/support';

const getHubBase = (): string => {
  if (typeof window === 'undefined') return '';
  const base = (process.env.NEXT_PUBLIC_API_URL ?? '').replace(/\/$/, '');
  if (!base) {
    console.error('NEXT_PUBLIC_API_URL is required for SupportHub');
  }
  return base;
};

export interface MessageReactionsUpdatedPayload {
  messageId: string;
  reactions: MessageReactionDto[];
}

export function useSupportHub(
  token: string | undefined,
  onBranchMessage: (payload: BranchMessagePayload) => void,
  onReactionsUpdated?: (payload: MessageReactionsUpdatedPayload) => void
) {
  const connectionRef = useRef<signalR.HubConnection | null>(null);
  const [connected, setConnected] = useState(false);
  const onMessageRef = useRef(onBranchMessage);
  const onReactionsRef = useRef(onReactionsUpdated);
  onMessageRef.current = onBranchMessage;
  onReactionsRef.current = onReactionsUpdated;

  useEffect(() => {
    if (!token) {
      if (connectionRef.current) {
        connectionRef.current.stop().catch(() => {});
        connectionRef.current = null;
        setConnected(false);
      }
      return;
    }

    const base = getHubBase();
    const url = `${base}/supportHub?access_token=${encodeURIComponent(token)}`;
    const connection = new signalR.HubConnectionBuilder()
      .withUrl(url)
      .withAutomaticReconnect()
      .build();

    connection.on('BranchMessage', (payload: BranchMessagePayload) => {
      onMessageRef.current(payload);
    });

    connection.on('MessageReactionsUpdated', (payload: MessageReactionsUpdatedPayload) => {
      onReactionsRef.current?.(payload);
    });

    connection
      .start()
      .then(() => {
        connectionRef.current = connection;
        setConnected(true);
      })
      .catch(() => setConnected(false));

    connection.onclose(() => setConnected(false));
    connection.onreconnected(() => setConnected(true));

    return () => {
      connection.stop().catch(() => {});
      connectionRef.current = null;
      setConnected(false);
    };
  }, [token]);

  return connected;
}
