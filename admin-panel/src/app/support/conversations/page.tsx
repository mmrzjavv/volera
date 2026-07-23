'use client';

import { useState, useEffect, useCallback, useRef, useMemo } from 'react';
import Link from 'next/link';
import { useSupportAuthStore } from '@/store/useSupportAuthStore';
import { supportApi, type SupportMessage, type BranchMessagePayload } from '@/api/support';
import { useSupportHub, type MessageReactionsUpdatedPayload } from '@/hooks/useSupportHub';
import { Select } from '@/components/ui/Select';
import { Button } from '@/components/ui/Button';
import { MessageSquare, Send, Paperclip, X, Reply, User, Smile, ArrowLeft, Loader2 } from 'lucide-react';

const PAGE_SIZE = 50;

function isClientMessage(m: SupportMessage): boolean {
  return !m.supportSenderId;
}

/** User-friendly date/time: "Today 2:19 PM", "Yesterday 5:53 PM", or "Feb 18, 2:19 PM" */
function formatMessageDateTime(isoOrDate: string | Date): string {
  const d = typeof isoOrDate === 'string' ? new Date(isoOrDate) : isoOrDate;
  const now = new Date();
  const today = new Date(now.getFullYear(), now.getMonth(), now.getDate());
  const msgDay = new Date(d.getFullYear(), d.getMonth(), d.getDate());
  const timeStr = d.toLocaleTimeString([], { hour: '2-digit', minute: '2-digit' });
  if (msgDay.getTime() === today.getTime()) return `Today ${timeStr}`;
  const yesterday = new Date(today);
  yesterday.setDate(yesterday.getDate() - 1);
  if (msgDay.getTime() === yesterday.getTime()) return `Yesterday ${timeStr}`;
  return d.toLocaleDateString([], { month: 'short', day: 'numeric' }) + ', ' + timeStr;
}

function senderDisplayName(m: SupportMessage): string {
  if (m.supportSender) {
    const n = [m.supportSender.firstName, m.supportSender.lastName].filter(Boolean).join(' ').trim();
    return n || m.supportSender.username || 'Support';
  }
  if (m.sender) {
    const n = [m.sender.firstName, m.sender.lastName].filter(Boolean).join(' ').trim();
    return n || m.sender.username || m.sender.email || m.sender.phoneNumber || 'Client';
  }
  return 'Unknown';
}

/** Client (sender) display details for list and header: name, email, phone */
function getClientDisplayInfo(m: SupportMessage): { name: string; email: string | null; phone: string | null } {
  const name = senderDisplayName(m);
  if (!m.sender) return { name, email: null, phone: null };
  const email = m.sender.email && m.sender.email.trim() ? m.sender.email.trim() : null;
  const phone = m.sender.phoneNumber && m.sender.phoneNumber.trim() ? m.sender.phoneNumber.trim() : null;
  return { name, email, phone };
}

function payloadToMessage(p: BranchMessagePayload, supportSender?: { id: string; firstName?: string; lastName?: string; username?: string }): SupportMessage {
  return {
    id: p.messageId,
    senderId: p.senderId,
    supportSenderId: p.supportSenderId ?? null,
    supportSender: supportSender ?? null,
    targetReceiverUserId: p.targetReceiverUserId ?? undefined,
    content: p.content,
    sentAt: p.sentAt,
    attachmentUrl: p.attachmentUrl ?? null,
    attachmentType: p.attachmentType ?? null,
    replyToMessageId: p.replyToMessageId ?? null,
    replyToMessage: p.replyToMessage?.contentSnippet
      ? { id: '', content: '', contentSnippet: p.replyToMessage.contentSnippet }
      : null,
  };
}

function messageBelongsToConversation(m: SupportMessage, clientId: string): boolean {
  if (isClientMessage(m)) return m.senderId === clientId;
  return m.targetReceiverUserId === clientId || m.targetReceiverUserId == null;
}

function buildConversations(messages: SupportMessage[]): { clientId: string; label: string; lastMessage: string; lastAt: string; email: string | null; phone: string | null }[] {
  const byClient = new Map<string, { label: string; lastMessage: string; lastAt: string; email: string | null; phone: string | null }>();
  for (const m of messages) {
    if (!isClientMessage(m) || !m.senderId) continue;
    const id = m.senderId;
    const existing = byClient.get(id);
    const at = new Date(m.sentAt).getTime();
    const snippet = (m.content || '').slice(0, 40) + ((m.content?.length ?? 0) > 40 ? '…' : '') || 'Attachment';
    const { name, email, phone } = getClientDisplayInfo(m);
    if (!existing || at > new Date(existing.lastAt).getTime()) {
      byClient.set(id, { label: name, lastMessage: snippet, lastAt: m.sentAt, email, phone });
    }
  }
  return Array.from(byClient.entries())
    .map(([clientId, v]) => ({ clientId, ...v }))
    .sort((a, b) => new Date(b.lastAt).getTime() - new Date(a.lastAt).getTime());
}

export default function SupportConversationsPage() {
  const auth = useSupportAuthStore((s) => s.auth);
  const [branches, setBranches] = useState<{ id: string; name: string }[]>([]);
  const [selectedBranchId, setSelectedBranchId] = useState('');
  const [allMessages, setAllMessages] = useState<SupportMessage[]>([]);
  const [selectedClientId, setSelectedClientId] = useState<string | null>(null);
  const [loadingBranches, setLoadingBranches] = useState(true);
  const [loadingMessages, setLoadingMessages] = useState(false);
  const [loadingMore, setLoadingMore] = useState(false);
  const [sending, setSending] = useState(false);
  const [replyContent, setReplyContent] = useState('');
  const [replyToMessageId, setReplyToMessageId] = useState<string>('');
  const [replyToPreview, setReplyToPreview] = useState<string>('');
  const [attachmentUrl, setAttachmentUrl] = useState<string>('');
  const [attachmentType, setAttachmentType] = useState<string>('');
  const [uploading, setUploading] = useState(false);
  const [error, setError] = useState('');
  const fileInputRef = useRef<HTMLInputElement>(null);
  const messagesEndRef = useRef<HTMLDivElement>(null);
  const sendInProgressRef = useRef(false);

  const loadBranches = useCallback(() => {
    if (!auth?.token || !auth.supportUser?.id) return;
    setLoadingBranches(true);
    setError('');
    supportApi
      .getBranches(auth.token, auth.supportUser.id)
      .then((res) => {
        if (res.success && res.data) {
          setBranches(res.data.map((b) => ({ id: b.id, name: b.name || b.id })));
          if (res.data.length > 0 && !selectedBranchId) setSelectedBranchId(res.data[0].id);
        }
      })
      .catch((e) => setError(e instanceof Error ? e.message : 'Failed to load branches'))
      .finally(() => setLoadingBranches(false));
  }, [auth?.token, auth?.supportUser?.id]);

  const loadMessages = useCallback(
    (before?: string, append = false) => {
      if (!auth?.token || !selectedBranchId) {
        if (!append) setAllMessages([]);
        return;
      }
      if (append) setLoadingMore(true);
      else setLoadingMessages(true);
      setError('');
      supportApi
        .getBranchMessages(auth.token, selectedBranchId, { limit: PAGE_SIZE, before })
        .then((res) => {
          if (res.success && res.data) {
            setAllMessages((prev) => (append ? [...res.data!, ...prev] : res.data!));
          }
        })
        .catch((e) => {
          setError(e instanceof Error ? e.message : 'Failed to load messages');
          if (!append) setAllMessages([]);
        })
        .finally(() => {
          setLoadingMessages(false);
          setLoadingMore(false);
        });
    },
    [auth?.token, selectedBranchId]
  );

  const handleBranchMessage = useCallback(
    (payload: BranchMessagePayload) => {
      if (payload.branchId !== selectedBranchId) return;
      const supportSender =
        payload.supportSenderId && auth?.supportUser?.id === payload.supportSenderId
          ? {
              id: auth.supportUser.id,
              firstName: auth.supportUser.firstName,
              lastName: auth.supportUser.lastName,
              username: auth.supportUser.username,
            }
          : undefined;
      const newMsg = payloadToMessage(payload, supportSender);
      setAllMessages((prev) => {
        if (prev.some((m) => m.id === newMsg.id)) return prev;
        return [...prev, newMsg];
      });
    },
    [selectedBranchId, auth?.supportUser]
  );

  const handleReactionsUpdated = useCallback((payload: MessageReactionsUpdatedPayload) => {
    setAllMessages((prev) =>
      prev.map((m) => (m.id === payload.messageId ? { ...m, messageReactions: payload.reactions } : m))
    );
  }, []);

  const connected = useSupportHub(auth?.token, handleBranchMessage, handleReactionsUpdated);

  useEffect(() => {
    loadBranches();
  }, [loadBranches]);

  useEffect(() => {
    loadMessages();
    setSelectedClientId(null);
  }, [loadMessages]);

  const conversations = useMemo(() => buildConversations(allMessages), [allMessages]);

  const conversationMessages = useMemo(() => {
    if (!selectedClientId) return [];
    return allMessages.filter((m) => messageBelongsToConversation(m, selectedClientId));
  }, [allMessages, selectedClientId]);

  useEffect(() => {
    messagesEndRef.current?.scrollIntoView({ behavior: 'smooth' });
  }, [conversationMessages]);

  const selectedConversation = useMemo(
    () => conversations.find((c) => c.clientId === selectedClientId) ?? null,
    [conversations, selectedClientId]
  );

  const handleSendReply = async () => {
    if (sendInProgressRef.current || sending) return;
    if (!auth?.token || !selectedBranchId) return;
    const content = replyContent.trim();
    if (!content && !attachmentUrl) return;
    sendInProgressRef.current = true;
    setSending(true);
    setError('');
    try {
      await supportApi.sendReply(auth.token, selectedBranchId, {
        content: content || '',
        targetClientUserId: selectedClientId || undefined,
        replyToMessageId: replyToMessageId || undefined,
        attachmentUrl: attachmentUrl || undefined,
        attachmentType: attachmentType || undefined,
      });
      setReplyContent('');
      setReplyToMessageId('');
      setReplyToPreview('');
      setAttachmentUrl('');
      setAttachmentType('');
      loadMessages();
    } catch (e) {
      setError(e instanceof Error ? e.message : 'Failed to send reply');
    } finally {
      sendInProgressRef.current = false;
      setSending(false);
    }
  };

  const loadOlder = () => {
    if (allMessages.length === 0) return;
    const oldest = allMessages[0];
    const before = new Date(oldest.sentAt).toISOString();
    loadMessages(before, true);
  };

  const hasMore = allMessages.length >= PAGE_SIZE;

  const branchOptions = branches.map((b) => ({ value: b.id, label: b.name }));

  const onFileSelect = async (e: React.ChangeEvent<HTMLInputElement>) => {
    const file = e.target.files?.[0];
    if (!file || !auth?.token) return;
    e.target.value = '';
    setUploading(true);
    setError('');
    try {
      const res = await supportApi.uploadFile(auth.token, file);
      if (res.data?.url) {
        setAttachmentUrl(res.data.url);
        setAttachmentType(file.type || 'application/octet-stream');
      }
    } catch (err) {
      setError(err instanceof Error ? err.message : 'Upload failed');
    } finally {
      setUploading(false);
    }
  };

  const onAttachClick = () => {
    if (uploading) return;
    fileInputRef.current?.click();
  };

  const startReplyTo = (m: SupportMessage) => {
    setReplyToMessageId(m.id);
    setReplyToPreview(m.content.slice(0, 80) + (m.content.length > 80 ? '…' : ''));
  };

  const QUICK_EMOJIS = ['👍', '❤️', '😂', '😮', '😢'];
  const [reactingMessageId, setReactingMessageId] = useState<string | null>(null);

  const handleReact = async (messageId: string, emoji: string) => {
    if (!auth?.token || !selectedBranchId) return;
    const msg = allMessages.find((m) => m.id === messageId);
    const reactions = msg?.messageReactions ?? [];
    const myReaction = reactions.find((r) => r.supportUserId === auth?.supportUser?.id);
    try {
      if (myReaction && myReaction.emoji === emoji) {
        await supportApi.removeReaction(auth.token, selectedBranchId, messageId);
      } else {
        await supportApi.addReaction(auth.token, selectedBranchId, messageId, emoji);
      }
    } catch {
      // ignore
    }
    setReactingMessageId(null);
  };

  if (!auth) return null;

  return (
    <div className="flex h-full flex-col bg-neutral-50/80 md:flex-row">
      {/* Left: conversation list */}
      <aside className="flex w-full shrink-0 flex-col border-b border-neutral-200 bg-white md:w-72 md:border-b-0 md:border-r">
        <div className="flex items-center gap-2 border-b border-neutral-200 px-3 py-3">
          <Link
            href="/support"
            className="flex shrink-0 items-center justify-center rounded-lg p-2 text-neutral-500 hover:bg-neutral-100 hover:text-neutral-700"
            title="Back to Inbox"
          >
            <ArrowLeft className="h-5 w-5" />
          </Link>
          <div className="min-w-0 flex-1">
            <Select
              label=""
              options={branchOptions}
              value={selectedBranchId}
              onChange={(e) => setSelectedBranchId(e.target.value)}
            />
          </div>
          <span
            className={`flex shrink-0 items-center gap-1.5 text-xs font-medium ${
              connected ? 'text-emerald-600' : 'text-amber-600'
            }`}
          >
            <span className={`h-2 w-2 rounded-full ${connected ? 'bg-emerald-500' : 'bg-amber-500'}`} />
            {connected ? 'Live' : '…'}
          </span>
        </div>
        <div className="flex-1 overflow-y-auto">
          {!selectedBranchId && (
            <p className="p-4 text-center text-sm text-neutral-500">Select a branch</p>
          )}
          {selectedBranchId && loadingMessages && allMessages.length === 0 && (
            <p className="p-4 text-center text-sm text-neutral-500">Loading…</p>
          )}
          {selectedBranchId && !loadingMessages && conversations.length === 0 && (
            <p className="p-4 text-center text-sm text-neutral-500">No conversations yet</p>
          )}
          {conversations.length > 0 && (
            <ul className="divide-y divide-neutral-100">
              {conversations.map((c) => (
                <li key={c.clientId}>
                  <button
                    type="button"
                    onClick={() => setSelectedClientId(c.clientId)}
                    className={`flex w-full items-center gap-3 px-3 py-3 text-left transition ${
                      selectedClientId === c.clientId ? 'bg-primary-50' : 'hover:bg-neutral-50'
                    }`}
                  >
                    <div className="flex h-10 w-10 shrink-0 items-center justify-center rounded-full bg-neutral-200 text-neutral-600">
                      <User className="h-5 w-5" />
                    </div>
                    <div className="min-w-0 flex-1">
                      <p className="truncate text-sm font-medium text-neutral-900">{c.label}</p>
                      {(c.email || c.phone) && (
                        <p className="truncate text-xs text-neutral-500">
                          {[c.email, c.phone].filter(Boolean).join(' · ')}
                        </p>
                      )}
                      {!c.email && !c.phone && (
                        <p className="truncate text-xs text-neutral-500">{c.lastMessage}</p>
                      )}
                    </div>
                    <span className="shrink-0 text-[11px] text-neutral-400">
                      {formatMessageDateTime(c.lastAt)}
                    </span>
                  </button>
                </li>
              ))}
            </ul>
          )}
        </div>
      </aside>

      {/* Right: selected conversation messages + input */}
      <div className="flex flex-1 flex-col min-w-0">
        {error && (
          <div className="shrink-0 bg-red-50 px-4 py-2 text-sm text-red-700">{error}</div>
        )}

        {selectedClientId ? (
          <>
            <header className="shrink-0 border-b border-neutral-200 bg-white px-4 py-3">
              <div className="flex items-center justify-between gap-2">
                <div className="min-w-0">
                  <p className="font-medium text-neutral-900">{selectedConversation?.label ?? 'Unknown'}</p>
                  {(selectedConversation?.email || selectedConversation?.phone) && (
                    <p className="mt-0.5 text-xs text-neutral-500">
                      {[selectedConversation?.email, selectedConversation?.phone].filter(Boolean).join(' · ')}
                    </p>
                  )}
                </div>
                {hasMore && (
                  <Button variant="secondary" size="sm" onClick={loadOlder} disabled={loadingMore}>
                    {loadingMore ? 'Loading…' : 'Load older'}
                  </Button>
                )}
              </div>
            </header>

            <div className="flex-1 overflow-y-auto px-4 py-4">
              {conversationMessages.length === 0 && !loadingMessages && (
                <div className="flex h-full items-center justify-center text-neutral-500">
                  No messages in this conversation yet.
                </div>
              )}
              {conversationMessages.length > 0 && (
                <ul className="space-y-3">
                  {conversationMessages.map((m) => {
                    const isClient = isClientMessage(m);
                    const name = senderDisplayName(m);
                    return (
                      <li
                        key={m.id}
                        className={`flex flex-col gap-1 rounded-2xl px-4 py-2.5 max-w-[85%] ${
                          isClient
                            ? 'self-start bg-white border border-neutral-200 text-neutral-800'
                            : 'self-end bg-primary-600 text-white border border-primary-600'
                        }`}
                      >
                        <div className="flex items-center justify-between gap-2">
                          <span className="text-xs font-medium opacity-90">{name}</span>
                          <span className={`text-[11px] opacity-75 ${isClient ? 'text-neutral-500' : ''}`}>
                            {formatMessageDateTime(m.sentAt)}
                          </span>
                        </div>
                        {m.replyToMessageId && (
                          <div
                            className={`rounded-lg border px-2 py-1 text-xs ${
                              isClient ? 'border-neutral-200 bg-neutral-50 text-neutral-600' : 'border-white/30 bg-white/15'
                            }`}
                          >
                            Reply to: {m.replyToMessage?.contentSnippet ?? m.replyToMessage?.content?.slice(0, 60) ?? '…'}
                          </div>
                        )}
                        <p className="break-words text-sm">{m.content || '\u00A0'}</p>
                        {m.attachmentUrl && (
                          <a
                            href={m.attachmentUrl}
                            target="_blank"
                            rel="noopener noreferrer"
                            className={`text-xs underline ${isClient ? 'text-primary-600' : 'text-white/90'}`}
                          >
                            Attachment
                          </a>
                        )}
                        {(m.messageReactions?.length ?? 0) > 0 && (
                          <div className="mt-1 flex flex-wrap gap-1">
                            {m.messageReactions!.map((r, idx) => (
                              <span
                                key={`${r.supportUserId ?? r.userId}-${idx}`}
                                className={`rounded-full px-2 py-0.5 text-xs border ${
                                  isClient
                                    ? 'bg-neutral-100 border-neutral-200 text-neutral-700'
                                    : 'bg-white/20 border-white/30 text-white'
                                }`}
                              >
                                {r.emoji} {r.supportUserName ?? r.userName ?? ''}
                              </span>
                            ))}
                          </div>
                        )}
                        <div className="mt-1 flex items-center gap-1">
                          {isClient && (
                            <button
                              type="button"
                              onClick={() => startReplyTo(m)}
                              className="flex items-center gap-1 text-xs text-neutral-500 hover:text-primary-600"
                            >
                              <Reply className="h-3 w-3" /> Reply
                            </button>
                          )}
                          <button
                            type="button"
                            onClick={() => setReactingMessageId(reactingMessageId === m.id ? null : m.id)}
                            className="flex items-center gap-1 text-xs text-neutral-500 hover:text-primary-600"
                            title="React"
                          >
                            <Smile className="h-3 w-3" /> React
                          </button>
                        </div>
                        {reactingMessageId === m.id && (
                          <div className="mt-1 flex gap-1">
                            {QUICK_EMOJIS.map((emoji) => (
                              <button
                                key={emoji}
                                type="button"
                                onClick={() => handleReact(m.id, emoji)}
                                className="rounded-lg p-1.5 text-lg hover:bg-neutral-200 dark:hover:bg-neutral-600 transition"
                              >
                                {emoji}
                              </button>
                            ))}
                          </div>
                        )}
                      </li>
                    );
                  })}
                  <div ref={messagesEndRef} />
                </ul>
              )}
            </div>

            <div className="shrink-0 border-t border-neutral-200 bg-white px-4 py-3">
              {(replyToPreview || attachmentUrl || uploading) && (
                <div className="mb-2 flex items-center justify-between gap-2 rounded-xl bg-neutral-100 px-3 py-2 text-sm">
                  <div className="min-w-0 flex-1 flex items-center gap-2">
                    {uploading && (
                      <Loader2 className="h-4 w-4 shrink-0 animate-spin text-primary-600" aria-hidden />
                    )}
                    {replyToPreview && (
                      <p className="truncate text-neutral-600">
                        <span className="font-medium">Replying:</span> {replyToPreview}
                      </p>
                    )}
                    {attachmentUrl && !uploading && <p className="text-neutral-500">Attachment attached</p>}
                    {uploading && <p className="text-neutral-600">Uploading…</p>}
                  </div>
                  <button
                    type="button"
                    onClick={() => {
                      setReplyToMessageId('');
                      setReplyToPreview('');
                      setAttachmentUrl('');
                      setAttachmentType('');
                    }}
                    className="shrink-0 rounded p-1 text-neutral-400 hover:bg-neutral-200 hover:text-neutral-600"
                    aria-label="Clear"
                  >
                    <X className="h-4 w-4" />
                  </button>
                </div>
              )}
              <div className="flex items-center gap-2">
                <input
                  type="file"
                  ref={fileInputRef}
                  onChange={onFileSelect}
                  className="hidden"
                  accept="image/*,.pdf,.doc,.docx,.txt"
                  disabled={uploading}
                />
                <button
                  type="button"
                  onClick={onAttachClick}
                  disabled={uploading}
                  className="flex h-10 w-10 shrink-0 items-center justify-center rounded-full text-neutral-500 transition hover:bg-neutral-100 hover:text-neutral-700 disabled:opacity-50 disabled:pointer-events-none"
                  aria-label={uploading ? 'Uploading…' : 'Attach file'}
                >
                  {uploading ? (
                    <Loader2 className="h-5 w-5 animate-spin" aria-hidden />
                  ) : (
                    <Paperclip className="h-5 w-5" />
                  )}
                </button>
                <input
                  type="text"
                  placeholder={sending ? 'Sending…' : 'Type a message…'}
                  value={replyContent}
                  onChange={(e) => setReplyContent(e.target.value)}
                  onKeyDown={(e) => {
                    if (e.key === 'Enter' && !e.shiftKey) {
                      e.preventDefault();
                      if (!sending) handleSendReply();
                    }
                  }}
                  disabled={sending}
                  className="min-w-0 flex-1 rounded-2xl border border-neutral-300 bg-neutral-50 px-4 py-2.5 text-sm outline-none transition placeholder:text-neutral-400 focus:border-primary-500 focus:bg-white focus:ring-1 focus:ring-primary-500 disabled:opacity-60 disabled:cursor-not-allowed"
                />
                <button
                  type="button"
                  onClick={handleSendReply}
                  disabled={sending || (!replyContent.trim() && !attachmentUrl)}
                  className="flex h-10 w-10 shrink-0 items-center justify-center rounded-full bg-primary-600 text-white shadow transition hover:bg-primary-700 disabled:opacity-40 disabled:cursor-not-allowed disabled:pointer-events-none"
                  aria-label={sending ? 'Sending…' : 'Send'}
                >
                  {sending ? (
                    <Loader2 className="h-5 w-5 animate-spin" aria-hidden />
                  ) : (
                    <Send className="h-5 w-5" />
                  )}
                </button>
              </div>
            </div>
          </>
        ) : (
          <div className="flex flex-1 items-center justify-center bg-neutral-50/50">
            <div className="text-center text-neutral-500">
              <MessageSquare className="mx-auto mb-2 h-12 w-12 text-neutral-300" />
              <p className="text-sm font-medium">Select a conversation</p>
              <p className="mt-1 text-xs">Choose a chat from the list to view and reply to messages.</p>
              <Link href="/support" className="mt-3 inline-block text-sm text-primary-600 hover:underline">
                Back to Inbox overview
              </Link>
            </div>
          </div>
        )}
      </div>
    </div>
  );
}
