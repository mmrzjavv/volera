'use client';

import { useState, useEffect, useCallback } from 'react';
import Link from 'next/link';
import { useSupportAuthStore } from '@/store/useSupportAuthStore';
import { supportApi, type SupportMessage } from '@/api/support';
import { useSupportHub } from '@/hooks/useSupportHub';
import { Card, CardHeader, CardTitle, CardContent } from '@/components/ui/Card';
import { Button } from '@/components/ui/Button';
import {
  MessageSquare,
  Building2,
  Users,
  ArrowRight,
  Inbox,
  Activity,
  Wifi,
  WifiOff,
} from 'lucide-react';

function isClientMessage(m: SupportMessage): boolean {
  return !m.supportSenderId;
}

/** Unique client sender IDs from messages */
function countConversations(messages: SupportMessage[]): number {
  const ids = new Set<string>();
  for (const m of messages) {
    if (isClientMessage(m) && m.senderId) ids.add(m.senderId);
  }
  return ids.size;
}

export default function SupportInboxOverviewPage() {
  const auth = useSupportAuthStore((s) => s.auth);
  const [branches, setBranches] = useState<{ id: string; name: string }[]>([]);
  const [conversationCount, setConversationCount] = useState<number | null>(null);
  const [loading, setLoading] = useState(true);
  const [error, setError] = useState('');

  const connected = useSupportHub(auth?.token ?? undefined, () => {}, undefined);

  const loadOverview = useCallback(() => {
    if (!auth?.token || !auth?.supportUser?.id) return;
    setLoading(true);
    setError('');
    supportApi
      .getBranches(auth.token, auth.supportUser.id)
      .then((res) => {
        if (!res.success || !res.data?.length) {
          setBranches([]);
          setConversationCount(0);
          return;
        }
        const list = res.data.map((b) => ({ id: b.id, name: b.name || b.id }));
        setBranches(list);
        // Load messages from first branch to get conversation count
        const firstBranchId = list[0].id;
        return supportApi.getBranchMessages(auth!.token, firstBranchId, { limit: 500 });
      })
      .then((messagesRes) => {
        if (messagesRes?.success && messagesRes.data) {
          setConversationCount(countConversations(messagesRes.data));
        }
      })
      .catch((e) => setError(e instanceof Error ? e.message : 'Failed to load overview'))
      .finally(() => setLoading(false));
  }, [auth?.token, auth?.supportUser?.id]);

  useEffect(() => {
    loadOverview();
  }, [loadOverview]);

  if (!auth) return null;

  const displayName =
    auth.supportUser.firstName || auth.supportUser.lastName
      ? `${auth.supportUser.firstName} ${auth.supportUser.lastName}`.trim()
      : auth.supportUser.username;

  return (
    <div className="p-4 lg:p-6 max-w-5xl mx-auto">
      <div className="mb-8">
        <h1 className="text-2xl font-bold text-slate-900">Inbox</h1>
        <p className="mt-1 text-slate-600">
          Welcome back, {displayName}. Here’s your support overview.
        </p>
      </div>

      {error && (
        <div className="mb-4 rounded-lg bg-red-50 px-4 py-3 text-sm text-red-700">{error}</div>
      )}

      {/* Status & connection */}
      <div className="mb-6 flex flex-wrap items-center gap-3">
        <span
          className={`inline-flex items-center gap-2 rounded-full px-3 py-1.5 text-sm font-medium ${
            connected ? 'bg-emerald-50 text-emerald-700' : 'bg-amber-50 text-amber-700'
          }`}
        >
          {connected ? (
            <>
              <Wifi className="h-4 w-4" />
              Live
            </>
          ) : (
            <>
              <WifiOff className="h-4 w-4" />
              Reconnecting…
            </>
          )}
        </span>
        <span className="inline-flex items-center gap-2 rounded-full bg-slate-100 px-3 py-1.5 text-sm text-slate-700">
          <Activity className="h-4 w-4" />
          Real-time updates when you open conversations
        </span>
      </div>

      {/* Stats */}
      <div className="grid gap-4 sm:grid-cols-2 lg:grid-cols-3 mb-8">
        <Card>
          <CardContent className="flex items-center gap-4 p-6">
            <div className="rounded-xl bg-primary-100 p-3">
              <Building2 className="h-6 w-6 text-primary-600" />
            </div>
            <div>
              <p className="text-2xl font-bold text-slate-900">
                {loading ? '—' : branches.length}
              </p>
              <p className="text-sm text-slate-500">Branches</p>
            </div>
          </CardContent>
        </Card>
        <Card>
          <CardContent className="flex items-center gap-4 p-6">
            <div className="rounded-xl bg-slate-100 p-3">
              <Users className="h-6 w-6 text-slate-600" />
            </div>
            <div>
              <p className="text-2xl font-bold text-slate-900">
                {loading ? '—' : conversationCount ?? '—'}
              </p>
              <p className="text-sm text-slate-500">Conversations (first branch)</p>
            </div>
          </CardContent>
        </Card>
        <Card className="sm:col-span-2 lg:col-span-1">
          <CardContent className="flex items-center gap-4 p-6">
            <div className="rounded-xl bg-emerald-100 p-3">
              <Inbox className="h-6 w-6 text-emerald-600" />
            </div>
            <div className="min-w-0">
              <p className="text-sm font-medium text-slate-900">Open conversations</p>
              <p className="text-xs text-slate-500">View and reply to guest chats</p>
            </div>
          </CardContent>
        </Card>
      </div>

      {/* Main CTA */}
      <Card className="border-primary-200 bg-primary-50/50">
        <CardHeader>
          <CardTitle className="flex items-center gap-2 text-slate-900">
            <MessageSquare className="h-5 w-5 text-primary-600" />
            Conversations
          </CardTitle>
        </CardHeader>
        <CardContent>
          <p className="text-sm text-slate-600 mb-4">
            View all conversations by branch, reply to messages, and react in real time.
          </p>
          <Link href="/support/conversations">
            <Button variant="primary" size="lg" className="gap-2">
              Open conversations
              <ArrowRight className="h-4 w-4" />
            </Button>
          </Link>
        </CardContent>
      </Card>

      {/* Quick info */}
      <Card className="mt-6">
        <CardHeader>
          <CardTitle className="text-slate-900">Quick info</CardTitle>
        </CardHeader>
        <CardContent className="text-sm text-slate-600 space-y-1">
          <p>Role: <span className="font-medium text-slate-800">{auth.supportUser.role}</span></p>
          <p>Username: {auth.supportUser.username}</p>
          {auth.supportUser.email && <p>Email: {auth.supportUser.email}</p>}
        </CardContent>
      </Card>
    </div>
  );
}
