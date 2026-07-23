import React, { useRef, useState, useEffect } from 'react';
import { Play, Pause } from 'lucide-react';
import { clsx } from 'clsx';

interface VoiceMessagePlayerProps {
    src: string;
    isMyMessage: boolean;
}

export const VoiceMessagePlayer: React.FC<VoiceMessagePlayerProps> = ({ src, isMyMessage }) => {
    const audioRef = useRef<HTMLAudioElement>(null);
    const [isPlaying, setIsPlaying] = useState(false);
    const [progress, setProgress] = useState(0);
    const [duration, setDuration] = useState(0);
    const [currentTime, setCurrentTime] = useState(0);

    useEffect(() => {
        if (audioRef.current) {
            audioRef.current.load();
        }
    }, [src]);

    const togglePlay = () => {
        if (!audioRef.current) return;
        if (isPlaying) {
            audioRef.current.pause();
        } else {
            audioRef.current.play().catch(err => {
                console.error("Playback failed:", err);
                setIsPlaying(false);
            });
        }
        setIsPlaying(!isPlaying);
    };

    const handleTimeUpdate = () => {
        if (!audioRef.current) return;
        const current = audioRef.current.currentTime;
        const dur = audioRef.current.duration;
        setCurrentTime(current);
        if (dur && isFinite(dur)) {
            setProgress((current / dur) * 100);
        }
    };

    const handleLoadedMetadata = () => {
        if (audioRef.current) {
            const dur = audioRef.current.duration;
            if (isFinite(dur)) {
                setDuration(dur);
            } else {
                // If duration is infinite (streaming/buggy blob), try to get it later or assume unknown
                setDuration(0);
                // Force a seek to end to get duration? No, that's risky.
            }
        }
    };

    const handleEnded = () => {
        setIsPlaying(false);
        setProgress(0);
        setCurrentTime(0);
    };

    const handleSeek = (e: React.ChangeEvent<HTMLInputElement>) => {
        const newProgress = Number(e.target.value);
        if (audioRef.current && duration) {
            const newTime = (newProgress / 100) * duration;
            audioRef.current.currentTime = newTime;
            setProgress(newProgress);
            setCurrentTime(newTime);
        }
    };

    const formatTime = (time: number) => {
        if (isNaN(time)) return "0:00";
        const minutes = Math.floor(time / 60);
        const seconds = Math.floor(time % 60);
        return `${minutes}:${seconds.toString().padStart(2, '0')}`;
    };

    return (
        <div className="flex items-center gap-2 md:gap-3 p-1 rounded-xl w-full min-w-[160px] select-none">
            <audio
                ref={audioRef}
                src={src}
                onTimeUpdate={handleTimeUpdate}
                onLoadedMetadata={handleLoadedMetadata}
                onEnded={handleEnded}
                onError={(e) => {
                    console.error("Audio playback error:", e.currentTarget.error, src);
                    setIsPlaying(false);
                }}
                preload="auto"
                playsInline
                crossOrigin="anonymous"
                className="hidden"
            />
            
            <button
                onClick={(e) => {
                    e.stopPropagation();
                    togglePlay();
                }}
                className={clsx(
                    "flex-shrink-0 w-8 h-8 md:w-10 md:h-10 rounded-full flex items-center justify-center transition-colors shadow-sm",
                    isMyMessage 
                        ? "bg-white text-blue-600 hover:bg-blue-50" 
                        : "bg-blue-600 text-white hover:bg-blue-700"
                )}
            >
                {isPlaying ? <Pause size={16} className="md:w-5 md:h-5" fill="currentColor" /> : <Play size={16} className="ml-0.5 md:w-5 md:h-5" fill="currentColor" />}
            </button>

            <div className="flex-1 flex flex-col justify-center gap-1 min-w-0">
                <input
                    type="range"
                    min="0"
                    max="100"
                    value={progress}
                    onChange={handleSeek}
                    onClick={(e) => e.stopPropagation()}
                    className={clsx(
                        "w-full h-1.5 rounded-full appearance-none cursor-pointer focus:outline-none accent-current touch-none",
                        isMyMessage 
                            ? "bg-blue-400 accent-white" 
                            : "bg-gray-200 accent-blue-600"
                    )}
                    style={{
                         // Custom styling for the slider track could go here if needed
                    }}
                />
                <div className={clsx(
                    "flex justify-between text-[10px] font-medium px-0.5",
                    isMyMessage ? "text-blue-100" : "text-gray-500"
                )}>
                    <span>{formatTime(currentTime)}</span>
                    <span>{formatTime(duration)}</span>
                </div>
            </div>
        </div>
    );
};
