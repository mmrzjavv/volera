import { useEffect, useState } from 'react';
import { useParams, useNavigate } from 'react-router-dom';
import { groupService, channelService } from '../services/api';
import { useAuthStore } from '../store/useAuthStore';
import { Button } from '../components/ui/Button';

type Preview = { id: string; name: string; inviteCode?: string; isChannel?: boolean } | null;

export function Invite() {
  const { inviteCode } = useParams<{ inviteCode: string }>();
  const navigate = useNavigate();
  const { isAuthenticated } = useAuthStore();
  const [preview, setPreview] = useState<Preview>(null);
  const [loading, setLoading] = useState(true);
  const [joining, setJoining] = useState(false);
  const [error, setError] = useState<string | null>(null);

  useEffect(() => {
    if (!inviteCode) {
      setError('Invalid invite link');
      setLoading(false);
      return;
    }
    channelService
      .getInvitePreview(inviteCode)
      .then((data) => {
        if (data?.id) {
          setPreview({ id: data.id, name: data.name, isChannel: true });
          return;
        }
        throw new Error('not channel');
      })
      .catch(() =>
        groupService
          .getGroupByInviteCode(inviteCode)
          .then((data) => {
            setPreview(data ? { ...data, isChannel: false } : null);
            if (!data) setError('Invalid or expired invite link');
          })
          .catch(() => setError('Invalid or expired invite link'))
      )
      .finally(() => setLoading(false));
  }, [inviteCode]);

  const handleJoin = async () => {
    if (!inviteCode || !preview) return;
    setJoining(true);
    setError(null);
    try {
      if (preview.isChannel) {
        await channelService.joinByInvite(inviteCode);
      } else {
        await groupService.joinByInvite(inviteCode);
      }
      navigate('/', { state: { openGroupId: preview.id }, replace: true });
    } catch {
      setError(preview.isChannel ? 'Could not join channel.' : 'Could not join group. You may already be a member.');
    } finally {
      setJoining(false);
    }
  };

  const noun = preview?.isChannel ? 'channel' : 'group';

  if (!isAuthenticated) {
    return (
      <div className="min-h-screen flex items-center justify-center bg-[var(--volera-bg)] px-4">
        <div className="text-center">
          <p className="text-[var(--volera-text-muted)] mb-4">Please log in to join this {noun || 'chat'}.</p>
          <Button onClick={() => navigate('/login', { state: { from: `/invite/${inviteCode}` }, replace: true })}>
            Log in
          </Button>
        </div>
      </div>
    );
  }

  if (loading) {
    return (
      <div className="min-h-screen flex items-center justify-center bg-[var(--volera-bg)]">
        <p className="text-[var(--volera-text-muted)]">Loading…</p>
      </div>
    );
  }

  if (error || !preview) {
    return (
      <div className="min-h-screen flex items-center justify-center bg-[var(--volera-bg)] px-4">
        <div className="text-center max-w-sm">
          <p className="text-red-600 dark:text-red-400 mb-4">{error ?? 'Invalid invite link'}</p>
          <Button variant="secondary" onClick={() => navigate('/', { replace: true })}>
            Go to chats
          </Button>
        </div>
      </div>
    );
  }

  return (
    <div className="min-h-screen flex items-center justify-center bg-[var(--volera-bg)] px-4">
      <div className="w-full max-w-sm rounded-[var(--volera-radius-md)] bg-[var(--volera-surface)] shadow-xl border border-[var(--volera-border)] overflow-hidden">
        <div className="p-6 text-center min-w-0">
          <div className="w-16 h-16 rounded-full bg-[var(--volera-accent)]/15 flex items-center justify-center text-[var(--volera-accent)] font-bold text-2xl mx-auto mb-4 shrink-0">
            {preview.name[0].toUpperCase()}
          </div>
          <h1 className="text-xl font-semibold text-[var(--volera-text)] mb-1 min-w-0 max-w-full truncate" title={preview.name}>
            {preview.name}
          </h1>
          <p className="text-sm text-[var(--volera-text-muted)] mb-6">
            You’ve been invited to join this {noun}
          </p>
          {error && <p className="text-sm text-red-600 dark:text-red-400 mb-4">{error}</p>}
          <Button onClick={handleJoin} isLoading={joining} className="w-full">
            Join {noun}
          </Button>
        </div>
        <div className="px-6 pb-6">
          <button
            type="button"
            onClick={() => navigate('/', { replace: true })}
            className="w-full py-2 min-h-[44px] text-sm text-[var(--volera-text-muted)] hover:text-[var(--volera-text)]"
          >
            Cancel
          </button>
        </div>
      </div>
    </div>
  );
}
