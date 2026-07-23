'use client';

import { useEffect, useRef, useState } from 'react';
import * as signalR from '@microsoft/signalr';

export interface ContentIndexedPayload {
  jobId: string;
  branchId: string;
  status: string;
  error?: string | null;
}

const getHubBase = (): string => {
  if (typeof window === 'undefined') return '';
  return (process.env.NEXT_PUBLIC_API_URL ?? '').replace(/\/$/, '');
};

export function useAiWidgetHub(
  companyToken: string | undefined,
  onContentIndexed: (payload: ContentIndexedPayload) => void
) {
  const connectionRef = useRef<signalR.HubConnection | null>(null);
  const [connected, setConnected] = useState(false);
  const onContentRef = useRef(onContentIndexed);
  onContentRef.current = onContentIndexed;

  useEffect(() => {
    if (!companyToken) {
      if (connectionRef.current) {
        connectionRef.current.stop().catch(() => {});
        connectionRef.current = null;
        setConnected(false);
      }
      return;
    }

    const base = getHubBase();
    const url = `${base}/aiWidgetHub?access_token=${encodeURIComponent(companyToken)}`;
    const connection = new signalR.HubConnectionBuilder()
      .withUrl(url)
      .withAutomaticReconnect()
      .build();

    connection.on('ContentIndexed', (jobId: string, branchId: string, status: string, error?: string | null) => {
      onContentRef.current({ jobId, branchId, status, error });
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
  }, [companyToken]);

  return connected;
}
