import React, { useState, useEffect } from 'react';
import { Users, Check } from 'lucide-react';
import { useContactStore } from '../store/useContactStore';
import { useChatStore } from '../store/useChatStore';
import { groupService } from '../services/api';
import { clsx } from 'clsx';
import { Modal } from './ui/Modal';
import { Button } from './ui/Button';
import { Input } from './ui/Input';

interface CreateGroupModalProps {
  isOpen: boolean;
  onClose: () => void;
  onRequestAddContact?: () => void;
}

export const CreateGroupModal: React.FC<CreateGroupModalProps> = ({
  isOpen,
  onClose,
  onRequestAddContact,
}) => {
  const { contacts, fetchContacts } = useContactStore();
  const { fetchGroups } = useChatStore();
  const [groupName, setGroupName] = useState('');
  const [selectedMemberIds, setSelectedMemberIds] = useState<string[]>([]);
  const [isLoading, setIsLoading] = useState(false);
  const [error, setError] = useState('');

  useEffect(() => {
    if (isOpen) {
      fetchContacts();
    }
  }, [isOpen, fetchContacts]);

  const selectableContacts = contacts.filter((c) => !!c.contactUserId);
  const phoneOnlyContacts = contacts.filter((c) => !c.contactUserId);

  const toggleMember = (userId: string) => {
    setSelectedMemberIds((prev) =>
      prev.includes(userId) ? prev.filter((id) => id !== userId) : [...prev, userId]
    );
  };

  const handleSubmit = async (e: React.FormEvent) => {
    e.preventDefault();
    if (!groupName.trim()) {
      setError('Group name is required');
      return;
    }

    setIsLoading(true);
    setError('');

    try {
      await groupService.createGroup({
        name: groupName.trim(),
        memberIds: selectedMemberIds,
      });
      await fetchGroups();
      onClose();
      setGroupName('');
      setSelectedMemberIds([]);
    } catch (err: any) {
      setError(err.response?.data?.message || 'Failed to create group');
    } finally {
      setIsLoading(false);
    }
  };

  const handleClose = () => {
    if (isLoading) return;
    setError('');
    onClose();
  };

  return (
    <Modal
      isOpen={isOpen}
      onClose={handleClose}
      closeDisabled={isLoading}
      tallOnMobile
      title={
        <span className="flex items-center gap-2">
          <Users size={20} className="text-[var(--volera-accent)] shrink-0" />
          Create New Group
        </span>
      }
      footer={
        <Button type="submit" form="create-group-form" className="w-full" isLoading={isLoading}>
          Create Group
        </Button>
      }
      bodyClassName="flex flex-col min-h-0"
    >
      <form id="create-group-form" onSubmit={handleSubmit} className="flex flex-col flex-1 min-h-0">
        <div className="p-6 pb-2">
          {error && (
            <div className="mb-4 p-3 bg-red-50 dark:bg-red-900/20 text-red-600 dark:text-red-400 rounded-[var(--volera-radius-sm)] text-sm">
              {error}
            </div>
          )}
          <Input
            label="Group Name"
            value={groupName}
            onChange={(e) => setGroupName(e.target.value)}
            placeholder="Enter group name"
          />
          <p className="mt-4 mb-2 text-sm font-medium text-[var(--volera-text-muted)]">
            Select Members <span className="font-normal">(optional)</span>
          </p>
        </div>

        <div className="flex-1 overflow-y-auto overflow-x-hidden px-6 pb-4 min-h-[120px] max-h-[min(45dvh,20rem)] sm:max-h-[40vh]">
          <div className="space-y-2">
            {contacts.length === 0 ? (
              <div className="py-4 text-center space-y-3">
                <p className="text-sm text-[var(--volera-text-muted)]">No contacts yet.</p>
                {onRequestAddContact && (
                  <Button
                    type="button"
                    variant="secondary"
                    size="sm"
                    onClick={() => {
                      handleClose();
                      onRequestAddContact();
                    }}
                  >
                    Add a contact
                  </Button>
                )}
              </div>
            ) : (
              <>
                {selectableContacts.map((contact) => (
                  <div
                    key={contact.id}
                    onClick={() => contact.contactUserId && toggleMember(contact.contactUserId)}
                    className={clsx(
                      'flex items-center gap-3 p-3 rounded-[var(--volera-radius-sm)] cursor-pointer border transition-all min-w-0 min-h-[44px]',
                      selectedMemberIds.includes(contact.contactUserId!)
                        ? 'bg-[var(--volera-accent)]/10 border-[var(--volera-accent)]/40'
                        : 'hover:bg-[var(--volera-surface-muted)] border-transparent'
                    )}
                  >
                    <div
                      className={clsx(
                        'w-5 h-5 rounded border flex items-center justify-center transition-colors shrink-0',
                        selectedMemberIds.includes(contact.contactUserId!)
                          ? 'bg-[var(--volera-accent)] border-[var(--volera-accent)]'
                          : 'border-[var(--volera-border)] bg-[var(--volera-surface)]'
                      )}
                    >
                      {selectedMemberIds.includes(contact.contactUserId!) && (
                        <Check size={12} className="text-white" />
                      )}
                    </div>
                    <div className="flex-1 min-w-0">
                      <div className="font-medium text-[var(--volera-text)] truncate">
                        {contact.contactName}
                      </div>
                    </div>
                  </div>
                ))}
                {phoneOnlyContacts.map((contact) => (
                  <div
                    key={contact.id}
                    className="flex items-center gap-3 p-3 rounded-[var(--volera-radius-sm)] border border-transparent opacity-50 min-w-0 min-h-[44px]"
                    title="Not on Volera yet"
                  >
                    <div className="w-5 h-5 rounded border border-[var(--volera-border)] bg-[var(--volera-surface-muted)] shrink-0" />
                    <div className="flex-1 min-w-0">
                      <div className="font-medium text-[var(--volera-text)] truncate">
                        {contact.contactName}
                      </div>
                      <div className="text-xs text-[var(--volera-text-muted)]">Not on Volera yet</div>
                    </div>
                  </div>
                ))}
              </>
            )}
          </div>
        </div>
      </form>
    </Modal>
  );
};
