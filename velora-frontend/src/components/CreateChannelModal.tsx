import React, { useState } from 'react';
import { Megaphone } from 'lucide-react';
import { useChatStore } from '../store/useChatStore';
import { useToastStore } from '../store/useToastStore';
import { channelService } from '../services/api';
import { Modal } from './ui/Modal';
import { Button } from './ui/Button';
import { Input } from './ui/Input';

interface CreateChannelModalProps {
  isOpen: boolean;
  onClose: () => void;
}

export const CreateChannelModal: React.FC<CreateChannelModalProps> = ({ isOpen, onClose }) => {
  const { fetchChannels, selectGroup } = useChatStore();
  const addToast = useToastStore((s) => s.addToast);
  const [name, setName] = useState('');
  const [description, setDescription] = useState('');
  const [isPublic, setIsPublic] = useState(false);
  const [publicUsername, setPublicUsername] = useState('');
  const [isLoading, setIsLoading] = useState(false);
  const [error, setError] = useState('');

  const handleSubmit = async (e: React.FormEvent) => {
    e.preventDefault();
    if (!name.trim()) {
      setError('Channel name is required');
      return;
    }
    if (isPublic && !/^[a-zA-Z][a-zA-Z0-9_]{3,31}$/.test(publicUsername.trim())) {
      setError('Public username must start with a letter and be 4–32 chars (letters, numbers, _)');
      return;
    }

    setIsLoading(true);
    setError('');
    try {
      const created = await channelService.createChannel({
        name: name.trim(),
        description: description.trim() || undefined,
        isPublic,
        publicUsername: isPublic ? publicUsername.trim() : undefined,
      });
      const channelId = created.channelId;
      await fetchChannels();
      const details = await channelService.getChannelDetails(channelId);
      await selectGroup({
        id: channelId,
        name: details.name || name.trim(),
        adminId: details.adminId || '',
        createdAt: details.createdAt || new Date().toISOString(),
        profilePictureUrl: details.profilePictureUrl,
        isChannel: true,
        kind: 'Channel',
        canPost: true,
        isPublic: details.isPublic,
        publicUsername: details.publicUsername,
      });
      if (created.inviteCode) {
        const url = `${window.location.origin}/invite/${created.inviteCode}`;
        try {
          await navigator.clipboard.writeText(url);
          addToast('Channel created — invite link copied', 'success');
        } catch {
          addToast('Channel created', 'success');
        }
      } else {
        addToast('Channel created', 'success');
      }
      setName('');
      setDescription('');
      setIsPublic(false);
      setPublicUsername('');
      onClose();
    } catch (err: unknown) {
      const msg = (err as { response?: { data?: { message?: string | string[] } } })?.response?.data?.message;
      setError(Array.isArray(msg) ? msg.join(', ') : msg || 'Failed to create channel');
    } finally {
      setIsLoading(false);
    }
  };

  return (
    <Modal
      isOpen={isOpen}
      onClose={() => !isLoading && onClose()}
      closeDisabled={isLoading}
      title={
        <span className="flex items-center gap-2">
          <Megaphone size={20} className="text-[var(--volera-accent)] shrink-0" />
          Create Channel
        </span>
      }
      footer={
        <Button type="submit" form="create-channel-form" className="w-full" isLoading={isLoading}>
          Create Channel
        </Button>
      }
    >
      <form id="create-channel-form" onSubmit={handleSubmit} className="p-6 space-y-4">
        {error && (
          <div className="p-3 bg-red-50 dark:bg-red-900/20 text-red-600 dark:text-red-400 rounded-[var(--volera-radius-sm)] text-sm">
            {error}
          </div>
        )}
        <Input label="Name" value={name} onChange={(e) => setName(e.target.value)} placeholder="Channel name" required />
        <Input label="Description" value={description} onChange={(e) => setDescription(e.target.value)} placeholder="Optional" />
        <label className="flex items-center gap-2 text-sm text-[var(--volera-text)] min-h-[44px]">
          <input type="checkbox" checked={isPublic} onChange={(e) => setIsPublic(e.target.checked)} />
          Public channel
        </label>
        {isPublic && (
          <Input
            label="Public username"
            value={publicUsername}
            onChange={(e) => setPublicUsername(e.target.value)}
            placeholder="my_channel"
            required
          />
        )}
      </form>
    </Modal>
  );
};
