import { useEffect, useState } from 'react';
import { useNavigate, useParams } from 'react-router-dom';
import { channelService } from '../services/api';
import { useAuthStore } from '../store/useAuthStore';
import { useChatStore } from '../store/useChatStore';
import { Button } from '../components/ui/Button';
import type { ChannelDetails } from '../types';

export function PublicChannelPage() {
  const { username } = useParams<{ username: string }>();
  const navigate = useNavigate();
  const { isAuthenticated } = useAuthStore();
  const { selectGroup } = useChatStore();
  const [channel, setChannel] = useState<ChannelDetails | null>(null);
  const [error, setError] = useState<string | null>(null);
  const [loading, setLoading] = useState(true);
  const [busy, setBusy] = useState(false);

  useEffect(() => {
    if (!username) return;
    channelService
      .getByUsername(username)
      .then(setChannel)
      .catch(() => setError('Channel not found'))
      .finally(() => setLoading(false));
  }, [username]);

  const subscribe = async () => {
    if (!channel || !isAuthenticated) {
      navigate('/login', { state: { from: `/c/${username}` } });
      return;
    }
    setBusy(true);
    try {
      await channelService.subscribe(channel.id);
      selectGroup({
        id: channel.id,
        name: channel.name,
        adminId: channel.adminId,
        createdAt: channel.createdAt,
        profilePictureUrl: channel.profilePictureUrl,
        isChannel: true,
        canPost: channel.canPost,
      });
      navigate('/', { replace: true });
    } catch {
      setError('Could not subscribe');
    } finally {
      setBusy(false);
    }
  };

  if (loading) {
    return (
      <div className="min-h-screen flex items-center justify-center bg-[var(--volera-bg)]">
        <p className="text-[var(--volera-text-muted)]">Loading…</p>
      </div>
    );
  }

  if (error || !channel) {
    return (
      <div className="min-h-screen flex items-center justify-center bg-[var(--volera-bg)] px-4">
        <div className="text-center">
          <p className="text-red-500 mb-4">{error ?? 'Not found'}</p>
          <Button variant="secondary" onClick={() => navigate('/')}>Back</Button>
        </div>
      </div>
    );
  }

  return (
    <div className="min-h-screen flex items-center justify-center bg-[var(--volera-bg)] px-4">
      <div className="w-full max-w-sm rounded-[var(--volera-radius-md)] bg-[var(--volera-surface)] border border-[var(--volera-border)] p-6 text-center">
        <div className="w-20 h-20 rounded-full bg-[var(--volera-accent)]/15 mx-auto mb-4 flex items-center justify-center text-2xl font-bold text-[var(--volera-accent)] overflow-hidden">
          {channel.profilePictureUrl ? (
            <img src={channel.profilePictureUrl} alt="" className="w-full h-full object-cover" />
          ) : (
            channel.name[0]?.toUpperCase()
          )}
        </div>
        <h1 className="text-xl font-semibold text-[var(--volera-text)]">{channel.name}</h1>
        {channel.publicUsername && <p className="text-sm text-[var(--volera-text-muted)]">@{channel.publicUsername}</p>}
        {channel.description && <p className="mt-3 text-sm text-[var(--volera-text-muted)]">{channel.description}</p>}
        <p className="mt-2 text-xs text-[var(--volera-text-muted)]">{channel.subscriberCount ?? 0} subscribers</p>
        <Button className="w-full mt-6" isLoading={busy} onClick={subscribe}>
          Subscribe
        </Button>
      </div>
    </div>
  );
}
