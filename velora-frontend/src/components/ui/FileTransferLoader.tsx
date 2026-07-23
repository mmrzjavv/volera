import React from 'react';
import { useFileTransferStore } from '../../store/useFileTransferStore';
import { Upload, File as FileIcon } from 'lucide-react';

export const FileTransferLoader: React.FC = () => {
    const transfers = useFileTransferStore(state => state.transfers);
    const activeUploads = Object.values(transfers).filter(t => t.type === 'upload' && t.status !== 'completed');

    if (activeUploads.length === 0) return null;

    return (
        <div className="fixed bottom-20 left-1/2 transform -translate-x-1/2 bg-white dark:bg-gray-800 shadow-lg rounded-xl border border-gray-200 dark:border-gray-700 p-4 w-80 z-50">
            <h4 className="text-sm font-semibold mb-3 flex items-center gap-2 text-gray-700 dark:text-gray-200">
                <Upload size={16} />
                Uploading {activeUploads.length} file(s)
            </h4>
            <div className="space-y-3 max-h-60 overflow-y-auto">
                {activeUploads.map(transfer => (
                    <div key={transfer.id} className="bg-gray-50 dark:bg-gray-750 rounded-lg p-2">
                        <div className="flex justify-between items-start mb-1">
                            <div className="flex items-center gap-2 overflow-hidden">
                                <FileIcon size={14} className="text-gray-400 flex-shrink-0" />
                                <span className="text-xs font-medium text-gray-600 dark:text-gray-300 truncate" title={transfer.id}>
                                    {transfer.id}
                                </span>
                            </div>
                            {transfer.status === 'error' ? (
                                <span className="text-xs text-red-500">Failed</span>
                            ) : (
                                <span className="text-xs text-blue-600 font-bold">{transfer.progress}%</span>
                            )}
                        </div>
                        
                        <div className="w-full bg-gray-200 rounded-full h-1.5 dark:bg-gray-700 overflow-hidden">
                            <div 
                                className={`h-1.5 rounded-full transition-all duration-300 ${
                                    transfer.status === 'error' ? 'bg-red-500' : 'bg-blue-500'
                                }`}
                                style={{ width: `${transfer.progress}%` }}
                            />
                        </div>
                        
                        {transfer.error && (
                             <p className="text-[10px] text-red-500 mt-1">{transfer.error}</p>
                        )}
                    </div>
                ))}
            </div>
        </div>
    );
};
