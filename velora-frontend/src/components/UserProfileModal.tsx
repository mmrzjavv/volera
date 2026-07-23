import React from 'react';
import type { User } from '../types';
import { getInitials } from '../utils/getInitials';
import { Modal } from './ui/Modal';

interface UserProfileModalProps {
  user: User | null;
  isOpen: boolean;
  onClose: () => void;
}

export const UserProfileModal: React.FC<UserProfileModalProps> = ({ user, isOpen, onClose }) => {
  return (
    <Modal isOpen={isOpen && !!user} onClose={onClose} title="Profile" maxWidth="max-w-sm">
      {user && (
        <div className="px-6 py-5 flex flex-col items-center text-center min-w-0">
          <div className="w-24 h-24 rounded-full bg-[var(--volera-surface-muted)] overflow-hidden mb-3 shadow-sm shrink-0">
            {user.profilePicture ? (
              <img
                src={user.profilePicture}
                alt={`${user.firstName} ${user.lastName}`}
                className="w-full h-full object-cover"
              />
            ) : (
              <div className="w-full h-full flex items-center justify-center text-[var(--volera-text-muted)] text-2xl font-bold uppercase">
                {getInitials(
                  [user.firstName, user.lastName].filter(Boolean).join(' ') || user.username
                )}
              </div>
            )}
          </div>

          <div className="w-full min-w-0 max-w-full">
            <h4 className="text-lg font-semibold text-[var(--volera-text)] truncate">
              {[user.firstName, user.lastName].filter(Boolean).join(' ') || user.username}
            </h4>
            {user.username && (
              <p className="text-sm text-[var(--volera-text-muted)] truncate">@{user.username}</p>
            )}
          </div>

          {user.bio && (
            <p className="mt-3 text-sm text-[var(--volera-text-muted)] whitespace-pre-wrap break-words max-w-xs min-w-0 w-full">
              {user.bio}
            </p>
          )}
        </div>
      )}
    </Modal>
  );
};
