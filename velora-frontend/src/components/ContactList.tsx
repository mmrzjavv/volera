import React, { useEffect, useState } from 'react';
import { useContactStore } from '../store/useContactStore';
import { useChatStore } from '../store/useChatStore';
import { useConfirmationStore } from '../store/useConfirmationStore';
import { UserPlus, Trash2 } from 'lucide-react';
import { getInitials } from '../utils/getInitials';
import { clsx } from 'clsx';
import { AddContactModal } from './AddContactModal';
import type { Contact } from '../types';

export const ContactList = () => {
  const { contacts, fetchContacts, isLoading, deleteContact } = useContactStore();
  const { selectUser, selectedUser, unreadCounts, users } = useChatStore();
  const { openDialog } = useConfirmationStore();
  const [isAddModalOpen, setIsAddModalOpen] = useState(false);

  useEffect(() => {
    fetchContacts();
  }, [fetchContacts]);

  const handleContactClick = (contact: Contact) => {
      // Map Contact to User for ChatStore
      // We assume contact has enough info, or we might need to fetch full user profile if missing
      // For now, let's construct a User object from Contact
      // Prefer live user data if available
      const liveUser = users?.find(u => u.id === contact.contactUserId);
      
      const user = liveUser || {
          id: contact.contactUserId,
          username: contact.contactName, // Fallback
          firstName: contact.contactName.split(' ')[0],
          lastName: contact.contactName.split(' ').slice(1).join(' '),
          email: '', // Not available in Contact list usually, might need adjustment
          phoneNumber: contact.contactPhoneNumber,
          profilePicture: contact.contactUser?.profilePicture,
          isOnline: contact.contactUser?.isOnline ?? false,
          lastSeen: contact.contactUser?.lastSeen
      };
      selectUser(user as any);
  };

  const handleUnknownSenderClick = (userId: string) => {
      const user = users?.find(u => u.id === userId);
      if (user) {
          selectUser(user);
      }
  };

  const handleDelete = async (e: React.MouseEvent, id: string) => {
      e.stopPropagation();
      openDialog({
          title: 'Delete Contact',
          message: 'Are you sure you want to delete this contact? This will remove them from your contacts list.',
          confirmText: 'Delete',
          variant: 'danger',
          onConfirm: async () => {
              await deleteContact(id);
          }
      });
  }

  // Filter users who have unread messages but are NOT in contacts
  const unknownSenders = Object.keys(unreadCounts).filter(senderId => 
      users?.some(u => u.id === senderId) && // Must be a known user (fetched from backend)
      !contacts?.some(c => c.contactUserId === senderId) // Must NOT be in contacts
  );

  return (
    <div className="flex-1 flex flex-col overflow-hidden">
      <div className="p-4 border-b border-gray-300 dark:border-gray-700 flex justify-between items-center bg-gray-100 dark:bg-gray-900">
        <div>
            <h2 className="font-bold text-gray-700 dark:text-gray-200">Contacts</h2>
        </div>
        <button 
            onClick={() => setIsAddModalOpen(true)}
            className="p-2 text-[var(--volera-accent)] hover:bg-[var(--volera-accent)]/10 rounded-full transition-colors"
            title="Add Contact"
        >
            <UserPlus size={20} />
        </button>
      </div>

      <div className="flex-1 overflow-y-auto overflow-x-hidden">
        {/* Unknown Senders Section */}
        {unknownSenders.length > 0 && (
            <div className="mb-2 border-b border-gray-100 dark:border-gray-700">
                <div className="px-4 py-2 text-xs font-semibold text-gray-500 dark:text-gray-400 bg-gray-50 dark:bg-gray-700/50">New Chats</div>
                {unknownSenders.map(senderId => {
                    const user = users?.find(u => u.id === senderId);
                    if (!user) return null;
                    const unreadCount = unreadCounts[senderId] || 0;
                    
                    return (
                        <div
                          key={senderId}
                          onClick={() => handleUnknownSenderClick(senderId)}
                          className={clsx(
                            "group p-4 border-b border-gray-100 dark:border-gray-700 cursor-pointer hover:bg-gray-50 dark:hover:bg-gray-700/50 transition-colors flex items-center gap-3 relative",
                            selectedUser?.id === senderId && "bg-[var(--volera-accent)]/10 border-l-4 border-l-[var(--volera-accent)]"
                          )}
                        >
                             <div className="relative">
                                <div className="w-10 h-10 rounded-full bg-[var(--volera-accent)]/15 flex items-center justify-center text-[var(--volera-accent)] overflow-hidden font-bold text-sm">
                                    {user.profilePicture ? (
                                        <img src={user.profilePicture} alt={user.firstName} className="w-full h-full object-cover" />
                                    ) : (
                                        getInitials(`${user.firstName ?? ''} ${user.lastName ?? ''}`.trim())
                                    )}
                                </div>
                                {user.isOnline && (
                                    <span className="absolute bottom-0 right-0 w-3 h-3 bg-green-500 border-2 border-white rounded-full"></span>
                                )}
                              </div>
                              <div className="flex-1 min-w-0">
                                <div className="font-medium text-gray-900 truncate">{user.firstName} {user.lastName}</div>
                                <div className="text-xs text-gray-500 truncate">{user.phoneNumber}</div>
                              </div>
                              {unreadCount > 0 && (
                                  <div className="w-5 h-5 rounded-full bg-[var(--volera-accent)] text-white text-[10px] flex items-center justify-center font-bold">
                                      {unreadCount}
                                  </div>
                              )}
                        </div>
                    );
                })}
            </div>
        )}

        {isLoading ? (
            <div className="p-4 text-center text-gray-500">Loading contacts...</div>
        ) : contacts.length === 0 && unknownSenders.length === 0 ? (
            <div className="p-8 text-center text-gray-400 flex flex-col items-center">
                <p>No contacts yet.</p>
                <button onClick={() => setIsAddModalOpen(true)} className="text-[var(--volera-accent)] hover:underline mt-2">Add one now</button>
            </div>
        ) : (
            contacts.map((contact) => {
                const liveUser = users?.find(u => u.id === contact.contactUserId);
                const isOnline = liveUser ? liveUser.isOnline : contact.contactUser?.isOnline;
                const unreadCount = contact.contactUserId ? (unreadCounts[contact.contactUserId] || 0) : 0;

                return (
                <div
                  key={contact.id}
                  onClick={() => handleContactClick(contact)}
                  className={clsx(
                    "group p-4 border-b border-gray-100 dark:border-gray-700 cursor-pointer hover:bg-gray-50 dark:hover:bg-gray-700/50 transition-colors flex items-center gap-3 relative",
                    selectedUser?.id === contact.contactUserId && "bg-[var(--volera-accent)]/10 border-l-4 border-l-[var(--volera-accent)]"
                  )}
                >
                  <div className="relative">
                    <div className="w-10 h-10 rounded-full bg-gray-200 dark:bg-gray-600 flex items-center justify-center text-gray-600 dark:text-gray-400 overflow-hidden font-bold text-sm">
                        {contact.contactUser?.profilePicture ? (
                            <img src={contact.contactUser.profilePicture} alt={contact.contactName} className="w-full h-full object-cover" />
                        ) : (
                            getInitials(contact.contactName)
                        )}
                    </div>
                    {isOnline && (
                        <span className="absolute bottom-0 right-0 w-3 h-3 bg-green-500 border-2 border-white dark:border-gray-800 rounded-full"></span>
                    )}
                  </div>
                  <div className="flex-1 min-w-0">
                    <div className="font-medium text-gray-900 dark:text-white truncate">{contact.contactName}</div>
                    <div className="text-xs text-gray-500 dark:text-gray-400 truncate">{contact.contactPhoneNumber}</div>
                  </div>
                  
                  {unreadCount > 0 && (
                      <div className="w-5 h-5 rounded-full bg-[var(--volera-accent)] text-white text-[10px] flex items-center justify-center font-bold mr-2">
                          {unreadCount}
                      </div>
                  )}

                  <button 
                    onClick={(e) => handleDelete(e, contact.id)}
                    className="p-2 min-w-[44px] min-h-[44px] flex items-center justify-center text-gray-400 dark:text-gray-500 hover:text-red-500 dark:hover:text-red-400 opacity-100 md:opacity-0 md:group-hover:opacity-100 transition-opacity touch-manipulation"
                    title="Delete Contact"
                  >
                      <Trash2 size={16} />
                  </button>
                </div>
            )})
        )}
      </div>

      <AddContactModal isOpen={isAddModalOpen} onClose={() => setIsAddModalOpen(false)} />
    </div>
  );
};
