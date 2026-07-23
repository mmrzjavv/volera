import { create } from 'zustand';
import type { Contact } from '../types';
import { contactService } from '../services/contactService';

interface ContactState {
  contacts: Contact[];
  isLoading: boolean;
  fetchContacts: () => Promise<void>;
  addContact: (identifier: string, name: string) => Promise<void>;
  deleteContact: (id: string) => Promise<void>;
  removeContactFromStore: (id: string) => void;
  restoreContactToStore: (contact: Contact) => void;
  syncContacts: (phoneNumbers: string[]) => Promise<void>;
}

export const useContactStore = create<ContactState>((set, get) => ({
  contacts: [],
  isLoading: false,

  fetchContacts: async () => {
    set({ isLoading: true });
    try {
      const contacts = await contactService.getContacts();
      set({ contacts, isLoading: false });
    } catch (error) {
      console.error('Failed to fetch contacts', error);
      set({ isLoading: false });
    }
  },

  addContact: async (identifier, name) => {
    set({ isLoading: true });
    try {
      await contactService.addContact({ contactIdentifier: identifier, contactName: name });
      // We might want to re-fetch or just append. 
      // Since addContact returns the ID but maybe not the full enriched contact (like avatar), 
      // re-fetching is safer, but appending is faster. 
      // For now, let's re-fetch to ensure we get correct status and related user info if applicable.
      await get().fetchContacts();
    } catch (error) {
      console.error('Failed to add contact', error);
      throw error;
    } finally {
        set({ isLoading: false });
    }
  },

  deleteContact: async (id) => {
    try {
      await contactService.deleteContact(id);
      set((state) => ({
        contacts: state.contacts.filter((c) => c.id !== id)
      }));
    } catch (error) {
      console.error('Failed to delete contact', error);
    }
  },

  // Helpers for optimistic updates / undo
  removeContactFromStore: (id: string) => {
    set((state) => ({
        contacts: state.contacts.filter((c) => c.id !== id)
    }));
  },
  
  restoreContactToStore: (contact: Contact) => {
    set((state) => ({
        contacts: [...state.contacts, contact].sort((a, b) => a.contactName.localeCompare(b.contactName))
    }));
  },
  
  syncContacts: async (phoneNumbers) => {
      set({ isLoading: true });
      try {
          await contactService.syncContacts({ phoneNumbers });
          await get().fetchContacts();
      } catch (error) {
          console.error('Failed to sync contacts', error);
      } finally {
          set({ isLoading: false });
      }
  }
}));
