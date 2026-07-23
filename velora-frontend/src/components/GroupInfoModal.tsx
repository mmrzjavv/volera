import React, { useState, useRef, useEffect } from 'react';
import type { Group, User, Contact } from '../types';
import { Crown, UserMinus, Link as LinkIcon, PlusCircle, Pencil, Trash2, ImagePlus } from 'lucide-react';
import { useToastStore } from '../store/useToastStore';
import { getInitials } from '../utils/getInitials';
import { Modal } from './ui/Modal';

interface GroupInfoModalProps {
  group: Group | null;
  members: User[];
  /** Contacts of the current admin */
  contacts?: Contact[];
  currentUserId: string | undefined;
  isOpen: boolean;
  onClose: () => void;
  onLeave?: () => void;
  onRemoveMember?: (userId: string) => void;
  /** Transfer group admin to this user (current admin only) */
  onMakeAdmin?: (userId: string) => void;
  onAddMember?: (userId: string) => void;
  /** Update group name / description / profile picture (admin only) */
  onUpdateProfile?: (data: { name?: string; description?: string | null; profilePictureUrl?: string | null }) => Promise<void>;
  /** Delete the entire group (admin only) */
  onDeleteGroup?: () => Promise<void>;
  inviteCode?: string;
  /** When invite code is not yet loaded, call this to generate and return the full invite URL */
  onGetInviteLink?: () => Promise<string | null>;
  /** Current async action: 'leave' | 'add' | 'remove' | 'makeAdmin' | 'updateProfile' | 'delete' – disables relevant buttons */
  actionInProgress?: string | null;
  /** True while members/details are loading */
  isLoadingDetails?: boolean;
}

export const GroupInfoModal: React.FC<GroupInfoModalProps> = ({
  group,
  members,
  contacts,
  currentUserId,
  isOpen,
  onClose,
  onLeave,
  onRemoveMember,
  onMakeAdmin,
  onAddMember,
  onUpdateProfile,
  onDeleteGroup,
  inviteCode,
  onGetInviteLink,
  actionInProgress = null,
  isLoadingDetails = false,
}) => {
  const { addToast } = useToastStore();

  const [selectedUserIdToAdd, setSelectedUserIdToAdd] = useState<string>('');
  const [page, setPage] = useState(1);
  const [inviteLinkLoading, setInviteLinkLoading] = useState(false);
  const [isEditingName, setIsEditingName] = useState(false);
  const [editNameValue, setEditNameValue] = useState(group?.name ?? '');
  const [showDeleteConfirm, setShowDeleteConfirm] = useState(false);
  const fileInputRef = useRef<HTMLInputElement>(null);
  const pageSize = 20;

  useEffect(() => {
    if (group) {
      setEditNameValue(group.name);
      setIsEditingName(false);
      setShowDeleteConfirm(false);
      setSelectedUserIdToAdd('');
      setPage(1);
    }
  }, [group?.id, group?.name]);

  if (!group) return null;

  const isCurrentUserAdmin = group.adminId === currentUserId;

  const registeredContacts = (contacts || []).filter((c) => !!c.contactUserId);
  const phoneOnlyContacts = (contacts || []).filter((c) => !c.contactUserId);

  const contactUsers: User[] = registeredContacts.map((c) => {
    const u = c.contactUser;
    if (u) return u;
    return {
      id: c.contactUserId!,
      username: c.contactName,
      firstName: c.contactName.split(' ')[0] || c.contactName,
      lastName: c.contactName.split(' ').slice(1).join(' '),
      phoneNumber: c.contactPhoneNumber,
      profilePicture: c.contactUser?.profilePicture,
      isOnline: c.contactUser?.isOnline ?? false,
    } as User;
  });

  const availableUsersToAdd = contactUsers.filter(
    (u) => !members.some((m) => m.id === u.id)
  );

  const total = availableUsersToAdd.length;
  const pagedUsers = availableUsersToAdd.slice(0, page * pageSize);
  const hasMore = total > pagedUsers.length;

  const busy = !!actionInProgress;

  return (
    <Modal
      isOpen={isOpen}
      onClose={onClose}
      closeDisabled={busy}
      title="Group Info"
      maxWidth="max-w-md"
      bodyClassName="px-6 py-4"
      footer={
        showDeleteConfirm ? (
          <div className="space-y-2">
            <p className="text-sm text-[var(--volera-text)]">
              Delete this group for everyone? This cannot be undone.
            </p>
            <div className="flex gap-2">
              <button
                type="button"
                onClick={async () => {
                  if (!onDeleteGroup) return;
                  try {
                    await onDeleteGroup();
                    setShowDeleteConfirm(false);
                    onClose();
                  } catch {
                    addToast('Failed to delete group', 'error');
                  }
                }}
                disabled={actionInProgress === 'delete'}
                className="flex-1 inline-flex items-center justify-center gap-2 px-3 py-2 text-sm font-medium text-white bg-red-600 rounded-[var(--volera-radius-sm)] hover:bg-red-700 disabled:opacity-50 disabled:pointer-events-none"
              >
                <Trash2 size={16} />
                {actionInProgress === 'delete' ? 'Deleting…' : 'Delete group'}
              </button>
              <button
                type="button"
                onClick={() => setShowDeleteConfirm(false)}
                disabled={busy}
                className="px-3 py-2 text-sm font-medium text-[var(--volera-text)] bg-[var(--volera-surface-muted)] rounded-[var(--volera-radius-sm)] hover:bg-[var(--volera-border)]/40 disabled:opacity-50"
              >
                Cancel
              </button>
            </div>
          </div>
        ) : (
          <div className="flex gap-2 flex-wrap">
            {isCurrentUserAdmin && onDeleteGroup && (
              <button
                type="button"
                onClick={() => setShowDeleteConfirm(true)}
                disabled={busy}
                className="inline-flex items-center justify-center gap-2 px-3 py-2 text-sm font-medium text-red-600 bg-red-50 dark:bg-red-900/20 rounded-[var(--volera-radius-sm)] hover:bg-red-100 dark:hover:bg-red-900/30 transition-colors disabled:opacity-50 disabled:pointer-events-none"
              >
                <Trash2 size={16} />
                Delete group
              </button>
            )}
            {onLeave && (
              <button
                type="button"
                onClick={onLeave}
                disabled={busy}
                className="flex-1 min-w-0 inline-flex items-center justify-center gap-2 px-3 py-2 text-sm font-medium text-red-600 bg-red-50 dark:bg-red-900/20 rounded-[var(--volera-radius-sm)] hover:bg-red-100 dark:hover:bg-red-900/30 transition-colors disabled:opacity-50 disabled:pointer-events-none"
              >
                <UserMinus size={16} />
                {actionInProgress === 'leave' ? 'Leaving…' : 'Leave Group'}
              </button>
            )}
          </div>
        )
      }
    >
      <div className="pb-4 border-b border-[var(--volera-border)]">
        <div className="flex items-center gap-3">
          <div className="relative">
            <div className="w-12 h-12 rounded-full bg-[var(--volera-accent)]/15 flex items-center justify-center text-[var(--volera-accent)] font-bold text-lg overflow-hidden">
              {group.profilePictureUrl ? (
                <img src={group.profilePictureUrl} alt="" className="w-full h-full object-cover" />
              ) : (
                group.name[0].toUpperCase()
              )}
            </div>
            {isCurrentUserAdmin && onUpdateProfile && !busy && (
              <>
                <button
                  type="button"
                  onClick={() => fileInputRef.current?.click()}
                  className="absolute bottom-0 right-0 w-6 h-6 rounded-full bg-[var(--volera-accent)] text-white flex items-center justify-center shadow hover:bg-[var(--volera-accent-hover)]"
                  title="Change group photo"
                >
                  <ImagePlus size={12} />
                </button>
                <input
                  ref={fileInputRef}
                  type="file"
                  accept="image/*"
                  className="hidden"
                  onChange={async (e) => {
                    const file = e.target.files?.[0];
                    if (!file || !onUpdateProfile) return;
                    e.target.value = '';
                    setInviteLinkLoading(true);
                    try {
                      const { fileService } = await import('../services/api');
                      const { url } = await fileService.upload(file);
                      await onUpdateProfile({ profilePictureUrl: url });
                      addToast('Group photo updated', 'success');
                    } catch {
                      addToast('Failed to update photo', 'error');
                    } finally {
                      setInviteLinkLoading(false);
                    }
                  }}
                />
              </>
            )}
          </div>
          <div className="flex-1 min-w-0">
            {isCurrentUserAdmin && onUpdateProfile && isEditingName ? (
              <div className="flex items-center gap-2">
                <input
                  type="text"
                  value={editNameValue}
                  onChange={(e) => setEditNameValue(e.target.value)}
                  className="flex-1 px-2 py-1 text-sm border border-[var(--volera-border)] rounded-[var(--volera-radius-sm)] bg-[var(--volera-surface-muted)] text-[var(--volera-text)] focus:outline-none focus:ring-1 focus:ring-[var(--volera-accent)]"
                  maxLength={100}
                  autoFocus
                />
                <button
                  type="button"
                  disabled={!editNameValue.trim() || actionInProgress === 'updateProfile'}
                  onClick={async () => {
                    if (!editNameValue.trim() || !onUpdateProfile) return;
                    try {
                      await onUpdateProfile({ name: editNameValue.trim() });
                      setIsEditingName(false);
                      addToast('Group name updated', 'success');
                    } catch {
                      addToast('Failed to update name', 'error');
                    }
                  }}
                  className="text-xs font-medium text-[var(--volera-accent)] hover:text-[var(--volera-accent-hover)] disabled:opacity-50"
                >
                  Save
                </button>
                <button
                  type="button"
                  onClick={() => {
                    setIsEditingName(false);
                    setEditNameValue(group.name);
                  }}
                  className="text-xs text-[var(--volera-text-muted)] hover:text-[var(--volera-text)]"
                >
                  Cancel
                </button>
              </div>
            ) : (
              <div className="flex items-center gap-1.5 min-w-0">
                <h4 className="text-base font-semibold text-[var(--volera-text)] truncate">{group.name}</h4>
                {isCurrentUserAdmin && onUpdateProfile && !busy && (
                  <button
                    type="button"
                    onClick={() => {
                      setIsEditingName(true);
                      setEditNameValue(group.name);
                    }}
                    className="p-0.5 rounded text-[var(--volera-text-muted)] hover:text-[var(--volera-text)] hover:bg-[var(--volera-border)]/40 flex-shrink-0"
                    title="Edit group name"
                  >
                    <Pencil size={14} />
                  </button>
                )}
              </div>
            )}
            <p className="text-xs text-[var(--volera-text-muted)]">
              {members.length} member{members.length === 1 ? '' : 's'}
            </p>
            {isCurrentUserAdmin && (inviteCode || onGetInviteLink) && (
              <button
                type="button"
                disabled={inviteLinkLoading}
                onClick={async () => {
                  const link = inviteCode
                    ? `${window.location.origin}/invite/${inviteCode}`
                    : await (async () => {
                        setInviteLinkLoading(true);
                        try {
                          return (await onGetInviteLink?.()) ?? null;
                        } finally {
                          setInviteLinkLoading(false);
                        }
                      })();
                  if (link) {
                    try {
                      await navigator.clipboard.writeText(link);
                      addToast('Invite link copied to clipboard', 'success');
                    } catch {
                      addToast('Could not copy link', 'error');
                    }
                  }
                }}
                className="mt-1 inline-flex items-center gap-1 text-[11px] text-[var(--volera-accent)] hover:text-[var(--volera-accent-hover)] disabled:opacity-50"
              >
                <LinkIcon size={12} />
                {inviteLinkLoading ? 'Getting link…' : inviteCode ? 'Copy invite link' : 'Get invite link'}
              </button>
            )}
          </div>
        </div>
      </div>

      <div className="pt-3">
        {isCurrentUserAdmin && onAddMember && (
          <div className="mb-4 space-y-2">
            <h5 className="text-xs font-semibold text-[var(--volera-text-muted)] mb-1.5 uppercase tracking-wide">
              Add member
            </h5>
            {pagedUsers.length > 0 ? (
              <>
                <div className="p-3 rounded-xl bg-[var(--volera-accent)]/10 border border-[var(--volera-accent)]/20 flex items-center gap-2">
                  <select
                    className="flex-1 bg-[var(--volera-surface)] border border-[var(--volera-accent)]/30 rounded-lg px-2 py-1 text-sm text-[var(--volera-text)] focus:outline-none focus:ring-1 focus:ring-[var(--volera-accent)]"
                    value={selectedUserIdToAdd}
                    onChange={(e) => setSelectedUserIdToAdd(e.target.value)}
                  >
                    <option value="">Select contact to add</option>
                    {pagedUsers.map((u) => (
                      <option key={u.id} value={u.id}>
                        {[u.firstName, u.lastName].filter(Boolean).join(' ') || u.username}
                      </option>
                    ))}
                  </select>
                  <button
                    type="button"
                    disabled={!selectedUserIdToAdd || busy}
                    onClick={async () => {
                      if (!selectedUserIdToAdd) return;
                      await onAddMember?.(selectedUserIdToAdd);
                      setSelectedUserIdToAdd('');
                    }}
                    className="inline-flex items-center gap-1 px-3 py-1.5 text-xs font-medium rounded-lg bg-[var(--volera-accent)] text-white disabled:opacity-50 disabled:cursor-not-allowed hover:bg-[var(--volera-accent-hover)] transition-colors"
                  >
                    <PlusCircle size={14} />
                    {actionInProgress === 'add' ? 'Adding…' : 'Add'}
                  </button>
                </div>
                {hasMore && (
                  <button
                    type="button"
                    onClick={() => setPage((p) => p + 1)}
                    className="w-full text-xs text-[var(--volera-accent)] hover:text-[var(--volera-accent-hover)] text-center"
                  >
                    Load more contacts ({total - pagedUsers.length} more)
                  </button>
                )}
              </>
            ) : phoneOnlyContacts.length > 0 ? (
              <div className="space-y-2">
                <p className="text-sm text-[var(--volera-text-muted)]">
                  No Volera contacts to add. These contacts are not on Volera yet:
                </p>
                {phoneOnlyContacts.map((contact) => (
                  <div
                    key={contact.id}
                    className="flex items-center gap-3 p-3 rounded-[var(--volera-radius-sm)] border border-transparent opacity-50 min-w-0 min-h-[44px]"
                    title="Not on Volera yet"
                  >
                    <div className="w-5 h-5 rounded border border-[var(--volera-border)] bg-[var(--volera-surface-muted)] shrink-0" />
                    <div className="flex-1 min-w-0">
                      <div className="font-medium text-[var(--volera-text)] truncate text-sm">
                        {contact.contactName}
                      </div>
                      <div className="text-xs text-[var(--volera-text-muted)]">Not on Volera yet</div>
                    </div>
                  </div>
                ))}
              </div>
            ) : (
              <p className="text-sm text-[var(--volera-text-muted)] py-2">
                No contacts to add. Add people to your contacts first, or share the invite link.
              </p>
            )}
          </div>
        )}
        <h5 className="text-xs font-semibold text-[var(--volera-text-muted)] mb-2 uppercase tracking-wide">
          Members
        </h5>
        {isLoadingDetails ? (
          <div className="py-8 flex justify-center">
            <div className="w-6 h-6 border-2 border-[var(--volera-accent)] border-t-transparent rounded-full animate-spin" />
          </div>
        ) : (
          <div className="space-y-2">
            {members.map((m) => {
              const isAdmin = m.id === group.adminId;
              const isSelf = m.id === currentUserId;
              return (
                <div
                  key={m.id}
                  className="flex items-center justify-between py-2 border-b border-[var(--volera-border)] last:border-0"
                >
                  <div className="flex items-center gap-3 min-w-0">
                    <div className="w-8 h-8 rounded-full bg-[var(--volera-surface-muted)] overflow-hidden flex items-center justify-center text-xs font-semibold text-[var(--volera-text)] uppercase">
                      {m.profilePicture ? (
                        <img
                          src={m.profilePicture}
                          alt={m.firstName}
                          className="w-full h-full object-cover"
                        />
                      ) : (
                        getInitials([m.firstName, m.lastName].filter(Boolean).join(' ') || m.username)
                      )}
                    </div>
                    <div className="min-w-0">
                      <div className="flex items-center gap-1">
                        <span className="text-sm font-medium text-[var(--volera-text)] truncate">
                          {[m.firstName, m.lastName].filter(Boolean).join(' ') || m.username}
                        </span>
                        {isAdmin && (
                          <Crown size={14} className="text-amber-400 flex-shrink-0" />
                        )}
                        {isSelf && (
                          <span className="text-[10px] text-[var(--volera-text-muted)] flex-shrink-0">
                            (You)
                          </span>
                        )}
                      </div>
                      {m.username && (
                        <p className="text-xs text-[var(--volera-text-muted)] truncate">@{m.username}</p>
                      )}
                    </div>
                  </div>
                  <div className="flex items-center gap-1">
                    {isCurrentUserAdmin && !isSelf && onMakeAdmin && !isAdmin && (
                      <button
                        type="button"
                        onClick={() => onMakeAdmin(m.id)}
                        disabled={busy}
                        className="p-1.5 rounded-full text-amber-500 hover:bg-amber-50 dark:hover:bg-amber-900/20 transition-colors disabled:opacity-50 disabled:pointer-events-none"
                        title="Make group admin"
                      >
                        <Crown size={16} />
                      </button>
                    )}
                    {isCurrentUserAdmin && !isSelf && onRemoveMember && (
                      <button
                        type="button"
                        onClick={() => onRemoveMember(m.id)}
                        disabled={busy}
                        className="p-1.5 rounded-full text-red-500 hover:bg-red-50 dark:hover:bg-red-900/20 transition-colors disabled:opacity-50 disabled:pointer-events-none"
                        title="Remove from group"
                      >
                        <UserMinus size={16} />
                      </button>
                    )}
                  </div>
                </div>
              );
            })}
          </div>
        )}
      </div>
    </Modal>
  );
};
