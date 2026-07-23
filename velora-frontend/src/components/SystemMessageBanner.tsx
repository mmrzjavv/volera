import React, { useEffect, useState } from 'react';
import { systemMessageService } from '../services/api';
import type { SystemMessage } from '../types';
import { X } from 'lucide-react';

const SystemMessageBanner: React.FC = () => {
    const [messages, setMessages] = useState<SystemMessage[]>([]);

    useEffect(() => {
        const fetchMessages = async () => {
            try {
                const data = await systemMessageService.getActive();
                const messages = data as SystemMessage[];
                setMessages(messages.filter(m => !m.isRead));
            } catch (error) {
                console.error("Failed to fetch system messages", error);
            }
        };

        fetchMessages();
    }, []);

    const handleDismiss = async (id: string) => {
        try {
            await systemMessageService.markAsRead(id);
            setMessages(prev => prev.filter(m => m.id !== id));
        } catch (error) {
            console.error("Failed to mark message as read", error);
        }
    };

    if (messages.length === 0) return null;

    const message = messages[0];

    return (
        <div className="bg-blue-600 text-white px-4 py-3 shadow-md relative flex items-start justify-between">
            <div className="flex-1">
                <p className="font-bold">{message.title}</p>
                <p className="text-sm mt-1">{message.content}</p>
            </div>
            <button 
                onClick={() => handleDismiss(message.id)}
                className="text-white hover:text-gray-200 ml-4 p-1"
            >
                <X className="w-5 h-5" />
            </button>
        </div>
    );
};

export default SystemMessageBanner;
