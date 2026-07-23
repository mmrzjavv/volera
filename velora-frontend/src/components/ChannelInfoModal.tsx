import React, { useEffect, useRef, useState } from 'react';
import {
  Megaphone,
  Link2,
  BarChart3,
  Users,
  Copy,
  Check,
  Eye,
  FileText,
  TrendingUp,
  ImagePlus,
  Pencil,
  Loader2,
} from 'lucide-react';
import { channelService, fileService } from '../services/api';
import { Button } from './ui/Button';
import { Modal } from './ui/Modal';
import { useToastStore } from '../store/useToastStore';
import { getInitials } from '../utils/getInitials';
import type { ChannelDetails, ChannelAnalytics } from '../types';

interface ChannelInfoModalProps {
  channelId: string;
  isOpen: boolean;
  onClose: () => void;
  onProfileUpdated?: (updates: {
    name: string;
    description?: string | null;
    profilePictureUrl?: string | null;
  }) => void;
}

export const ChannelInfoModal: React.FC<ChannelInfoModalProps> = ({
  channelId,
  isOpen,
  onClose,
  onProfileUpdated,
}) => {
  const addToast = useToastStore((s) => s.addToast);
  const [details, setDetails] = useState<ChannelDetails | null>(null);
  const [analytics, setAnalytics] = useState<ChannelAnalytics | null>(null);
  const [inviteCode, setInviteCode] = useState('');
  const [inviteUrl, setInviteUrl] = useState('');
  const [copied, setCopied] = useState(false);
  const [tab, setTab] = useState<'info' | 'analytics'>('info');
  const [loading, setLoading] = useState(true);
  const [signatures, setSignatures] = useState(false);
  const [savingProfile, setSavingProfile] = useState(false);
  const [isEditingName, setIsEditingName] = useState(false);
  const [isEditingDescription, setIsEditingDescription] = useState(false);
  const [editName, setEditName] = useState('');
  const [editDescription, setEditDescription] = useState('');
  const fileInputRef = useRef<HTMLInputElement>(null);

  useEffect(() => {
    if (!isOpen) return;
    setLoading(true);
    setCopied(false);
    setTab('info');
    setIsEditingName(false);
    setIsEditingDescription(false);
    channelService
      .getChannelDetails(channelId)
      .then((d) => {
        setDetails(d);
        setSignatures(!!d.signaturesEnabled);
        setEditName(d.name);
        setEditDescription(d.description ?? '');
        if (d.inviteCode) {
          setInviteCode(d.inviteCode);
          setInviteUrl(`${window.location.origin}/invite/${d.inviteCode}`);
        } else {
          setInviteCode('');
          setInviteUrl('');
        }
      })
      .finally(() => setLoading(false));
  }, [isOpen, channelId]);

  useEffect(() => {
    if (!isOpen || !details?.isAdmin || tab !== 'analytics') return;
    channelService.getAnalytics(channelId).then(setAnalytics).catch(() => setAnalytics(null));
  }, [tab, isOpen, channelId, details?.isAdmin]);

  const canChangeInfo = !!details?.canChangeInfo;
  const busy = savingProfile;

  const saveProfile = async (patch: {
    name?: string;
    description?: string | null;
    profilePictureUrl?: string | null;
  }) => {
    if (!details) return;
    const name = (patch.name ?? details.name).trim();
    if (!name) {
      addToast('Channel name is required', 'error');
      return;
    }
    const description =
      patch.description !== undefined ? patch.description : (details.description ?? null);
    const profilePictureUrl =
      patch.profilePictureUrl !== undefined
        ? patch.profilePictureUrl
        : (details.profilePictureUrl ?? null);

    setSavingProfile(true);
    try {
      await channelService.updateProfile(channelId, {
        name,
        description: description ?? undefined,
        profilePictureUrl: profilePictureUrl ?? undefined,
      });
      const next: ChannelDetails = {
        ...details,
        name,
        description,
        profilePictureUrl,
      };
      setDetails(next);
      setEditName(name);
      setEditDescription(description ?? '');
      onProfileUpdated?.({ name, description, profilePictureUrl });
      addToast('Channel profile updated', 'success');
    } catch {
      addToast('Failed to update channel profile', 'error');
      throw new Error('profile update failed');
    } finally {
      setSavingProfile(false);
    }
  };

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
    <Modal
      isOpen={isOpen}
      onClose={() => !busy && onClose()}
      closeDisabled={busy}
      tallOnMobile
      maxWidth="max-w-lg"
      title={
        <span className="flex items-center gap-2">
          <Megaphone size={18} className="text-[var(--volera-accent)] shrink-0" />
          Channel info
        </span>
      }
      headerClassName={details?.isAdmin ? 'border-b-0' : undefined}
    >
      {details?.isAdmin && (
        <div className="flex gap-1 px-4 pt-2 pb-1 border-b border-[var(--volera-border)] sticky top-0 bg-[var(--volera-surface)] z-[1]">
          {(['info', 'analytics'] as const).map((t) => (
            <button
              key={t}
              type="button"
              onClick={() => setTab(t)}
              className={`px-3 py-2 min-h-[40px] text-sm rounded-lg capitalize touch-manipulation ${
                tab === t
                  ? 'bg-[var(--volera-accent-soft)] text-[var(--volera-accent)]'
                  : 'text-[var(--volera-text-muted)]'
              }`}
            >
              {t}
            </button>
          ))}
        </div>
      )}

      <div className="p-4 space-y-4 text-sm">
        {loading || !details ? (
          <p className="text-[var(--volera-text-muted)]">Loading…</p>
        ) : tab === 'info' ? (
          <>
            <div className="flex items-start gap-3">
              <div className="relative shrink-0">
                <div className="w-16 h-16 rounded-full bg-[var(--volera-accent-soft)] flex items-center justify-center text-[var(--volera-accent)] font-bold text-xl overflow-hidden">
                  {details.profilePictureUrl ? (
                    <img src={details.profilePictureUrl} alt="" className="w-full h-full object-cover" />
                  ) : (
                    getInitials(details.name) || <Megaphone size={22} />
                  )}
                </div>
                {canChangeInfo && (
                  <>
                    <button
                      type="button"
                      onClick={() => fileInputRef.current?.click()}
                      disabled={busy}
                      className="absolute bottom-0 right-0 w-7 h-7 min-w-[28px] min-h-[28px] rounded-full bg-[var(--volera-accent)] text-white flex items-center justify-center shadow hover:bg-[var(--volera-accent-hover)] disabled:opacity-50"
                      title="Change channel photo"
                      aria-label="Change channel photo"
                    >
                      {savingProfile ? <Loader2 size={12} className="animate-spin" /> : <ImagePlus size={14} />}
                    </button>
                    <input
                      ref={fileInputRef}
                      type="file"
                      accept="image/*"
                      className="hidden"
                      onChange={async (e) => {
                        const file = e.target.files?.[0];
                        e.target.value = '';
                        if (!file) return;
                        setSavingProfile(true);
                        try {
                          const { url } = await fileService.upload(file);
                          await saveProfile({ profilePictureUrl: url });
                        } catch {
                          addToast('Failed to upload photo', 'error');
                          setSavingProfile(false);
                        }
                      }}
                    />
                  </>
                )}
              </div>

              <div className="min-w-0 flex-1 space-y-1">
                {canChangeInfo && isEditingName ? (
                  <div className="flex flex-wrap items-center gap-2">
                    <input
                      type="text"
                      value={editName}
                      onChange={(e) => setEditName(e.target.value)}
                      maxLength={100}
                      autoFocus
                      disabled={busy}
                      className="flex-1 min-w-[8rem] px-2 py-2 text-sm border border-[var(--volera-border)] rounded-[var(--volera-radius-sm)] bg-[var(--volera-surface-muted)] text-[var(--volera-text)] focus:outline-none focus:ring-2 focus:ring-[var(--volera-accent)]"
                    />
                    <button
                      type="button"
                      disabled={!editName.trim() || busy}
                      onClick={async () => {
                        try {
                          await saveProfile({ name: editName.trim() });
                          setIsEditingName(false);
                        } catch {
                          /* toast already shown */
                        }
                      }}
                      className="text-xs font-medium text-[var(--volera-accent)] disabled:opacity-50 min-h-[36px] px-2"
                    >
                      Save
                    </button>
                    <button
                      type="button"
                      disabled={busy}
                      onClick={() => {
                        setIsEditingName(false);
                        setEditName(details.name);
                      }}
                      className="text-xs text-[var(--volera-text-muted)] min-h-[36px] px-2"
                    >
                      Cancel
                    </button>
                  </div>
                ) : (
                  <div className="flex items-center gap-1.5 min-w-0">
                    <div className="text-lg font-medium truncate">{details.name}</div>
                    {canChangeInfo && (
                      <button
                        type="button"
                        disabled={busy}
                        onClick={() => {
                          setEditName(details.name);
                          setIsEditingName(true);
                        }}
                        className="p-2 rounded text-[var(--volera-text-muted)] hover:text-[var(--volera-text)] hover:bg-[var(--volera-surface-muted)] shrink-0 min-h-[40px] min-w-[40px] flex items-center justify-center"
                        title="Edit channel name"
                        aria-label="Edit channel name"
                      >
                        <Pencil size={14} />
                      </button>
                    )}
                  </div>
                )}

                {details.publicUsername && (
                  <a
                    className="text-[var(--volera-accent)] hover:underline break-all"
                    href={`/c/${details.publicUsername}`}
                    target="_blank"
                    rel="noreferrer"
                  >
                    @{details.publicUsername}
                  </a>
                )}
                <p className="text-[var(--volera-text-muted)] flex items-center gap-1">
                  <Users size={14} /> {details.subscriberCount ?? 0} subscribers
                </p>
              </div>
            </div>

            <div className="space-y-2">
              <div className="flex items-center justify-between gap-2">
                <span className="text-xs font-semibold uppercase tracking-wide text-[var(--volera-text-muted)]">
                  Description
                </span>
                {canChangeInfo && !isEditingDescription && (
                  <button
                    type="button"
                    disabled={busy}
                    onClick={() => {
                      setEditDescription(details.description ?? '');
                      setIsEditingDescription(true);
                    }}
                    className="text-xs text-[var(--volera-accent)] hover:underline disabled:opacity-50 min-h-[36px] px-1"
                  >
                    Edit
                  </button>
                )}
              </div>
              {canChangeInfo && isEditingDescription ? (
                <div className="space-y-2">
                  <textarea
                    value={editDescription}
                    onChange={(e) => setEditDescription(e.target.value)}
                    rows={3}
                    maxLength={500}
                    disabled={busy}
                    placeholder="What is this channel about?"
                    className="w-full px-3 py-2 text-sm border border-[var(--volera-border)] rounded-[var(--volera-radius-sm)] bg-[var(--volera-surface-muted)] text-[var(--volera-text)] resize-none focus:outline-none focus:ring-2 focus:ring-[var(--volera-accent)]"
                  />
                  <div className="flex gap-2 justify-end">
                    <button
                      type="button"
                      disabled={busy}
                      onClick={() => {
                        setIsEditingDescription(false);
                        setEditDescription(details.description ?? '');
                      }}
                      className="px-3 py-2 text-xs text-[var(--volera-text-muted)] min-h-[40px]"
                    >
                      Cancel
                    </button>
                    <Button
                      type="button"
                      size="sm"
                      disabled={busy}
                      isLoading={busy}
                      onClick={async () => {
                        try {
                          await saveProfile({ description: editDescription.trim() || null });
                          setIsEditingDescription(false);
                        } catch {
                          /* toast already shown */
                        }
                      }}
                    >
                      Save
                    </Button>
                  </div>
                </div>
              ) : details.description ? (
                <p className="text-[var(--volera-text-muted)] whitespace-pre-wrap break-words">{details.description}</p>
              ) : (
                <p className="text-[var(--volera-text-muted)] italic">No description</p>
              )}
            </div>

            {details.isAdmin && (
              <div className="space-y-3 border-t border-[var(--volera-border)] pt-3">
                <div className="font-medium">Invite link</div>
                <p className="text-xs text-[var(--volera-text-muted)]">
                  Anyone with this link can join the channel (works for private and public channels).
                </p>
                <div className="flex flex-col sm:flex-row gap-2">
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
                  <p className="text-xs break-all text-[var(--volera-text-muted)] bg-[var(--volera-surface-muted)] p-2 rounded-lg">
                    {inviteUrl}
                  </p>
                )}
                <label className="flex items-center gap-2 min-h-[44px]">
                  <input type="checkbox" checked={signatures} onChange={toggleSig} />
                  Admin signatures on posts
                </label>
              </div>
            )}
          </>
        ) : analytics ? (
          <div className="space-y-4">
            <div className="flex items-center gap-2 font-medium">
              <BarChart3 size={16} className="text-[var(--volera-accent)]" />
              Channel analytics
            </div>
            <div className="grid grid-cols-2 gap-3">
              {analyticsCards.map(({ label, value, icon: Icon }) => (
                <div
                  key={label}
                  className="rounded-xl border border-[var(--volera-border)] bg-[var(--volera-surface-muted)] p-3"
                >
                  <div className="flex items-center gap-2 text-xs text-[var(--volera-text-muted)] mb-2">
                    <Icon size={14} className="text-[var(--volera-accent)]" />
                    {label}
                  </div>
                  <div className="text-2xl font-semibold tabular-nums">{value.toLocaleString()}</div>
                </div>
              ))}
            </div>
            <p className="text-xs text-[var(--volera-text-muted)]">
              Views are counted when subscribers open posts in this channel.
            </p>
          </div>
        ) : (
          <p className="text-[var(--volera-text-muted)]">No analytics yet</p>
        )}
      </div>
    </Modal>
  );
};
