import React, { useEffect, useState } from 'react';
import { Megaphone, Link2, BarChart3, Users, X, Copy, Check, Eye, FileText, TrendingUp } from 'lucide-react';
import { channelService } from '../services/api';
import { Button } from './ui/Button';
import { useToastStore } from '../store/useToastStore';
import type { ChannelDetails, ChannelAnalytics } from '../types';

interface ChannelInfoModalProps {
  channelId: string;
  isOpen: boolean;
  onClose: () => void;
}

export const ChannelInfoModal: React.FC<ChannelInfoModalProps> = ({ channelId, isOpen, onClose }) => {
  const addToast = useToastStore((s) => s.addToast);
  const [details, setDetails] = useState<ChannelDetails | null>(null);
  const [analytics, setAnalytics] = useState<ChannelAnalytics | null>(null);
  const [inviteCode, setInviteCode] = useState('');
  const [inviteUrl, setInviteUrl] = useState('');
  const [copied, setCopied] = useState(false);
  const [tab, setTab] = useState<'info' | 'analytics'>('info');
  const [loading, setLoading] = useState(true);
  const [signatures, setSignatures] = useState(false);

  useEffect(() => {
    if (!isOpen) return;
    setLoading(true);
    setCopied(false);
    setTab('info');
    channelService.getChannelDetails(channelId)
      .then((d) => {
        setDetails(d);
        setSignatures(!!d.signaturesEnabled);
        if (d.inviteCode) {
          setInviteCode(d.inviteCode);
          setInviteUrl(`${window.location.origin}/invite/${d.inviteCode}`);
        }
      })
      .finally(() => setLoading(false));
  }, [isOpen, channelId]);

  useEffect(() => {
    if (!isOpen || !details?.isAdmin || tab !== 'analytics') return;
    channelService.getAnalytics(channelId).then(setAnalytics).catch(() => setAnalytics(null));
  }, [tab, isOpen, channelId, details?.isAdmin]);

  if (!isOpen) return null;

  const generateInvite = async () => {
    const code = await channelService.generateInviteLink(channelId);
    setInviteCode(code);
    const url = `${window.location.origin}/invite/${code}`;
    setInviteUrl(url);
    addToast('Invite link ready', 'success');
  };

  const copyInvite = async () => {
    if (!inviteUrl) return;
    try {
      await navigator.clipboard.writeText(inviteUrl);
      setCopied(true);
      addToast('Invite link copied', 'success');
      setTimeout(() => setCopied(false), 2000);
    } catch {
      addToast('Could not copy link', 'error');
    }
  };

  const toggleSig = async () => {
    await channelService.toggleSignatures(channelId, !signatures);
    setSignatures(!signatures);
  };

  const analyticsCards = analytics
    ? [
        { label: 'Subscribers', value: analytics.subscriberCount, icon: Users },
        { label: 'Total posts', value: analytics.postCount, icon: FileText },
        { label: 'Total views', value: analytics.totalViews, icon: Eye },
        { label: 'Posts (7 days)', value: analytics.postsLast7Days, icon: TrendingUp },
      ]
    : [];

  return (
    <div className="fixed inset-0 z-50 flex items-center justify-center p-4 bg-black/50" onClick={onClose}>
      <div
        className="bg-white dark:bg-gray-900 rounded-xl border border-gray-200 dark:border-gray-700 w-full max-w-lg max-h-[85vh] overflow-hidden flex flex-col"
        onClick={(e) => e.stopPropagation()}
      >
        <div className="flex items-center justify-between p-4 border-b border-gray-200 dark:border-gray-700">
          <h2 className="font-semibold text-gray-900 dark:text-gray-100 flex items-center gap-2">
            <Megaphone size={18} className="text-[var(--volera-accent)]" />
            Channel info
          </h2>
          <button onClick={onClose} className="p-2 text-gray-500 hover:text-gray-800 dark:hover:text-white" aria-label="Close">
            <X size={18} />
          </button>
        </div>

        {details?.isAdmin && (
          <div className="flex gap-1 px-4 pt-3">
            {(['info', 'analytics'] as const).map((t) => (
              <button
                key={t}
                type="button"
                onClick={() => setTab(t)}
                className={`px-3 py-1.5 text-sm rounded-lg capitalize ${tab === t ? 'bg-[var(--volera-accent)]/15 text-[var(--volera-accent)]' : 'text-gray-500'}`}
              >
                {t}
              </button>
            ))}
          </div>
        )}

        <div className="p-4 overflow-y-auto flex-1 space-y-4 text-sm">
          {loading || !details ? (
            <p className="text-gray-500">Loading…</p>
          ) : tab === 'info' ? (
            <>
              <div>
                <div className="text-lg font-medium text-gray-900 dark:text-gray-100">{details.name}</div>
                {details.publicUsername && (
                  <a
                    className="text-[var(--volera-accent)] hover:underline"
                    href={`/c/${details.publicUsername}`}
                    target="_blank"
                    rel="noreferrer"
                  >
                    @{details.publicUsername}
                  </a>
                )}
                {details.description && <p className="mt-2 text-gray-600 dark:text-gray-300">{details.description}</p>}
                <p className="mt-2 text-gray-500 flex items-center gap-1">
                  <Users size={14} /> {details.subscriberCount ?? 0} subscribers
                </p>
              </div>

              {details.isAdmin && (
                <div className="space-y-3 border-t border-gray-200 dark:border-gray-700 pt-3">
                  <div className="font-medium text-gray-800 dark:text-gray-200">Invite link</div>
                  <p className="text-xs text-gray-500">
                    Anyone with this link can join the channel (works for private and public channels).
                  </p>
                  <div className="flex gap-2">
                    <Button type="button" variant="secondary" className="flex-1" onClick={generateInvite}>
                      <Link2 size={16} className="mr-2" /> {inviteCode ? 'Refresh link' : 'Generate invite link'}
                    </Button>
                    {inviteUrl && (
                      <Button type="button" onClick={copyInvite} title="Copy invite link">
                        {copied ? <Check size={16} /> : <Copy size={16} />}
                      </Button>
                    )}
                  </div>
                  {inviteUrl && (
                    <p className="text-xs break-all text-gray-600 dark:text-gray-400 bg-gray-50 dark:bg-gray-800 p-2 rounded-lg">
                      {inviteUrl}
                    </p>
                  )}
                  <label className="flex items-center gap-2">
                    <input type="checkbox" checked={signatures} onChange={toggleSig} />
                    Admin signatures on posts
                  </label>
                </div>
              )}
            </>
          ) : analytics ? (
            <div className="space-y-4">
              <div className="flex items-center gap-2 font-medium text-gray-900 dark:text-gray-100">
                <BarChart3 size={16} className="text-[var(--volera-accent)]" />
                Channel analytics
              </div>
              <div className="grid grid-cols-2 gap-3">
                {analyticsCards.map(({ label, value, icon: Icon }) => (
                  <div
                    key={label}
                    className="rounded-xl border border-gray-200 dark:border-gray-700 bg-gray-50 dark:bg-gray-800/60 p-3"
                  >
                    <div className="flex items-center gap-2 text-xs text-gray-500 dark:text-gray-400 mb-2">
                      <Icon size={14} className="text-[var(--volera-accent)]" />
                      {label}
                    </div>
                    <div className="text-2xl font-semibold tabular-nums text-gray-900 dark:text-gray-100">
                      {value.toLocaleString()}
                    </div>
                  </div>
                ))}
              </div>
              <p className="text-xs text-gray-500">
                Views are counted when subscribers open posts in this channel.
              </p>
            </div>
          ) : (
            <p className="text-gray-500">No analytics yet</p>
          )}
        </div>
      </div>
    </div>
  );
};
