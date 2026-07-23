import React, { useState, useEffect } from 'react';
import { systemMessageService } from '../services/api';
import type { SystemMessage } from '../types';
import { Trash2, Pencil } from 'lucide-react';

const formatExpiresAtForInput = (expiresAt?: string): string => {
    if (!expiresAt) return '';
    const d = new Date(expiresAt);
    if (isNaN(d.getTime())) return '';
    return d.toISOString().slice(0, 16);
};

const AdminMessages: React.FC = () => {
    const [title, setTitle] = useState('');
    const [content, setContent] = useState('');
    const [expiresAt, setExpiresAt] = useState('');
    const [messages, setMessages] = useState<SystemMessage[]>([]);
    const [loading, setLoading] = useState(false);
    const [editingId, setEditingId] = useState<string | null>(null);

    useEffect(() => {
        loadMessages();
    }, []);

    const loadMessages = async () => {
        try {
            const data = await systemMessageService.getActive();
            setMessages(data as SystemMessage[]);
        } catch (error) {
            console.error(error);
        }
    };

    const startEdit = (msg: SystemMessage) => {
        setEditingId(msg.id);
        setTitle(msg.title);
        setContent(msg.content);
        setExpiresAt(formatExpiresAtForInput(msg.expiresAt));
    };

    const cancelEdit = () => {
        setEditingId(null);
        setTitle('');
        setContent('');
        setExpiresAt('');
    };

    const handleSubmit = async (e: React.FormEvent) => {
        e.preventDefault();
        setLoading(true);
        try {
            if (editingId) {
                await systemMessageService.update(editingId, {
                    title,
                    content,
                    expiresAt: expiresAt ? new Date(expiresAt).toISOString() : undefined
                });
                cancelEdit();
                loadMessages();
                alert('Message updated!');
            } else {
                await systemMessageService.create({
                    title,
                    content,
                    expiresAt: expiresAt ? new Date(expiresAt).toISOString() : undefined
                });
                setTitle('');
                setContent('');
                setExpiresAt('');
                loadMessages();
                alert('Message created!');
            }
        } catch (error) {
            alert(editingId ? 'Failed to update message' : 'Failed to create message');
            console.error(error);
        } finally {
            setLoading(false);
        }
    };

    const handleDelete = async (id: string) => {
        if (!confirm('Are you sure?')) return;
        try {
            await systemMessageService.delete(id);
            if (editingId === id) cancelEdit();
            loadMessages();
        } catch (error) {
            console.error(error);
        }
    };

    return (
        <div className="w-full max-w-4xl min-w-0">
            <h1 className="text-xl sm:text-2xl font-bold mb-4 sm:mb-6">System Messages</h1>
            
            <form onSubmit={handleSubmit} className="bg-slate-900 border border-slate-800 rounded-xl p-4 sm:p-6 mb-6">
                <div className="mb-4">
                    <label className="block text-slate-400 text-sm font-medium mb-2">Title</label>
                    <input 
                        className="w-full py-2.5 px-3 bg-slate-800 border border-slate-700 rounded-lg text-slate-100 placeholder-slate-500 focus:border-teal-500 focus:ring-1 focus:ring-teal-500 touch-manipulation"
                        value={title}
                        onChange={e => setTitle(e.target.value)}
                        required
                    />
                </div>
                <div className="mb-4">
                    <label className="block text-slate-400 text-sm font-medium mb-2">Content</label>
                    <textarea 
                        className="w-full py-2.5 px-3 bg-slate-800 border border-slate-700 rounded-lg text-slate-100 placeholder-slate-500 focus:border-teal-500 focus:ring-1 focus:ring-teal-500 min-h-[120px] touch-manipulation"
                        value={content}
                        onChange={e => setContent(e.target.value)}
                        required
                    />
                </div>
                <div className="mb-4">
                    <label className="block text-slate-400 text-sm font-medium mb-2">Expires At (Optional)</label>
                    <input 
                        type="datetime-local"
                        className="w-full py-2.5 px-3 bg-slate-800 border border-slate-700 rounded-lg text-slate-100 focus:border-teal-500 focus:ring-1 focus:ring-teal-500 touch-manipulation"
                        value={expiresAt}
                        onChange={e => setExpiresAt(e.target.value)}
                    />
                </div>
                <div className="flex flex-wrap gap-2">
                    <button
                        type="submit"
                        className="min-h-[44px] px-4 py-2.5 bg-teal-600 hover:bg-teal-700 text-white font-medium rounded-lg focus:outline-none focus:ring-2 focus:ring-teal-500 disabled:opacity-50 touch-manipulation"
                        disabled={loading}
                    >
                        {loading ? (editingId ? 'Updating...' : 'Sending...') : (editingId ? 'Update Message' : 'Send Message')}
                    </button>
                    {editingId && (
                        <button
                            type="button"
                            onClick={cancelEdit}
                            className="min-h-[44px] px-4 py-2.5 bg-slate-700 hover:bg-slate-600 text-slate-200 font-medium rounded-lg focus:outline-none focus:ring-2 focus:ring-slate-500 touch-manipulation"
                        >
                            Cancel
                        </button>
                    )}
                </div>
            </form>

            <h2 className="text-lg sm:text-xl font-bold mb-4">Active Messages</h2>
            <div className="bg-slate-900 border border-slate-800 rounded-xl overflow-hidden">
                <ul className="divide-y divide-slate-800">
                    {messages.map(msg => (
                        <li key={msg.id} className="px-4 sm:px-6 py-4 flex flex-col sm:flex-row sm:justify-between sm:items-center gap-3">
                            <div className="min-w-0 flex-1">
                                <p className="font-semibold text-slate-100">{msg.title}</p>
                                <p className="text-sm text-slate-400 mt-1">{msg.content}</p>
                                <p className="text-xs text-slate-500 mt-1">Expires: {msg.expiresAt ? new Date(msg.expiresAt).toLocaleString() : 'Never'}</p>
                            </div>
                            <div className="flex gap-1 self-start sm:self-center">
                                <button
                                    type="button"
                                    onClick={() => startEdit(msg)}
                                    className="p-2.5 rounded-lg text-slate-400 hover:bg-slate-800 hover:text-teal-400 touch-manipulation min-h-[44px] min-w-[44px] flex items-center justify-center"
                                    aria-label="Edit message"
                                >
                                    <Pencil size={20} />
                                </button>
                                <button
                                    type="button"
                                    onClick={() => handleDelete(msg.id)}
                                    className="p-2.5 rounded-lg text-red-400 hover:bg-slate-800 hover:text-red-300 touch-manipulation min-h-[44px] min-w-[44px] flex items-center justify-center"
                                    aria-label="Delete message"
                                >
                                    <Trash2 size={20} />
                                </button>
                            </div>
                        </li>
                    ))}
                    {messages.length === 0 && <li className="px-4 sm:px-6 py-6 text-slate-500 text-center">No active messages.</li>}
                </ul>
            </div>
        </div>
    );
};

export default AdminMessages;
