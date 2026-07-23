import React, { useState } from 'react';
import { UserPlus, Search } from 'lucide-react';
import { useContactStore } from '../store/useContactStore';
import { contactService } from '../services/contactService';
import type { User } from '../types';
import { Modal } from './ui/Modal';
import { Button } from './ui/Button';
import { Input } from './ui/Input';

interface AddContactModalProps {
  isOpen: boolean;
  onClose: () => void;
}

export const AddContactModal: React.FC<AddContactModalProps> = ({ isOpen, onClose }) => {
  const { addContact } = useContactStore();
  const [identifier, setIdentifier] = useState('');
  const [name, setName] = useState('');
  const [error, setError] = useState('');
  const [isLoading, setIsLoading] = useState(false);
  const [searchResults, setSearchResults] = useState<User[]>([]);
  const [isSearching, setIsSearching] = useState(false);

  const handleSearch = async () => {
    if (!identifier.trim()) return;
    setIsSearching(true);
    setError('');
    try {
      const results = await contactService.searchUsers(identifier.trim(), 'username');
      setSearchResults(Array.isArray(results) ? results : [results]);
    } catch {
      setError('User not found');
      setSearchResults([]);
    } finally {
      setIsSearching(false);
    }
  };

  const handleSubmit = async (e: React.FormEvent) => {
    e.preventDefault();
    if (!identifier || !name) {
      setError('Please fill in all fields');
      return;
    }

    setIsLoading(true);
    setError('');

    try {
      await addContact(identifier.trim(), name.trim());
      onClose();
      setIdentifier('');
      setName('');
      setSearchResults([]);
    } catch (err: any) {
      setError(err.response?.data?.message || 'Failed to add contact');
    } finally {
      setIsLoading(false);
    }
  };

  const selectUser = (user: User) => {
    setIdentifier(user.username || user.id);
    setName(`${user.firstName} ${user.lastName}`.trim());
  };

  const handleClose = () => {
    if (isLoading) return;
    setError('');
    setSearchResults([]);
    onClose();
  };

  return (
    <Modal
      isOpen={isOpen}
      onClose={handleClose}
      closeDisabled={isLoading}
      title={
        <span className="flex items-center gap-2">
          <UserPlus size={20} className="text-[var(--volera-accent)] shrink-0" />
          Add New Contact
        </span>
      }
    >
      <div className="p-6">
        <p className="text-xs text-[var(--volera-text-muted)] mb-3">
          Search by username or enter manually below.
        </p>
        <div className="mb-4 flex gap-2">
          <input
            type="text"
            value={identifier}
            onChange={(e) => setIdentifier(e.target.value)}
            placeholder="Search by Username"
            className="flex-1 min-h-[44px] px-4 py-2 border border-[var(--volera-border)] rounded-[var(--volera-radius-sm)] bg-[var(--volera-surface-muted)] text-[var(--volera-text)] placeholder:text-[var(--volera-text-muted)]/70 focus:outline-none focus:ring-2 focus:ring-[var(--volera-accent)]"
          />
          <button
            type="button"
            onClick={handleSearch}
            disabled={isSearching || !identifier}
            className="p-2 min-h-[44px] min-w-[44px] flex items-center justify-center bg-[var(--volera-accent)]/15 text-[var(--volera-accent)] rounded-[var(--volera-radius-sm)] hover:bg-[var(--volera-accent)]/25 disabled:opacity-50"
            aria-label="Search"
          >
            <Search size={20} />
          </button>
        </div>

        {searchResults.length > 0 && (
          <div className="mb-4 max-h-40 overflow-y-auto overflow-x-hidden border border-[var(--volera-border)] rounded-[var(--volera-radius-sm)]">
            {searchResults.map((user) => (
              <div
                key={user.id}
                onClick={() => selectUser(user)}
                className="p-2 hover:bg-[var(--volera-surface-muted)] cursor-pointer flex items-center gap-2 border-b border-[var(--volera-border)] last:border-0 min-w-0"
              >
                <div className="w-8 h-8 shrink-0 bg-[var(--volera-accent)]/15 rounded-full flex items-center justify-center text-[var(--volera-accent)] text-xs font-bold">
                  {user.firstName?.[0]}
                  {user.lastName?.[0]}
                </div>
                <div className="min-w-0 flex-1">
                  <div className="text-sm font-medium truncate text-[var(--volera-text)]">
                    {user.firstName} {user.lastName}
                  </div>
                  <div className="text-xs text-[var(--volera-text-muted)] truncate">
                    @{user.username}
                  </div>
                </div>
              </div>
            ))}
          </div>
        )}

        <form onSubmit={handleSubmit} className="space-y-4">
          <Input
            label="Contact Name (Nickname)"
            value={name}
            onChange={(e) => setName(e.target.value)}
            placeholder="e.g. Mom, John Doe"
            required
          />
          <Input
            label="Username (Selected)"
            value={identifier}
            onChange={(e) => setIdentifier(e.target.value)}
            placeholder="e.g. johndoe"
          />

          {error && (
            <div className="p-3 max-h-24 overflow-y-auto overflow-x-hidden bg-red-50 dark:bg-red-900/20 text-red-600 dark:text-red-400 text-sm rounded-[var(--volera-radius-sm)] break-words">
              {error}
            </div>
          )}

          <div className="flex gap-3 pt-2">
            <Button type="button" variant="secondary" onClick={handleClose} className="flex-1" disabled={isLoading}>
              Cancel
            </Button>
            <Button type="submit" className="flex-1" isLoading={isLoading}>
              Add Contact
            </Button>
          </div>
        </form>
      </div>
    </Modal>
  );
};
