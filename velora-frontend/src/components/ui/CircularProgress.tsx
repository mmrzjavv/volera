import React from 'react';

interface CircularProgressProps {
  progress: number;
  size?: number;
  strokeWidth?: number;
  color?: string;
  trackColor?: string;
  onClick?: (e: React.MouseEvent) => void;
  className?: string;
  icon?: React.ReactNode;
}

export const CircularProgress: React.FC<CircularProgressProps> = ({
  progress,
  size = 40,
  strokeWidth = 4,
  color = '#3b82f6',
  trackColor = '#e5e7eb',
  onClick,
  className,
  icon
}) => {
  const center = size / 2;
  const radius = center - strokeWidth / 2;
  const circumference = 2 * Math.PI * radius;
  const offset = circumference - (progress / 100) * circumference;

  return (
    <div 
        className={`relative inline-flex items-center justify-center ${className || ''}`}
        style={{ width: size, height: size }}
        onClick={onClick}
    >
      <svg
        className="transform -rotate-90"
        width={size}
        height={size}
      >
        <circle
          className="transition-colors duration-300"
          stroke={trackColor}
          strokeWidth={strokeWidth}
          fill="transparent"
          r={radius}
          cx={center}
          cy={center}
        />
        <circle
          className="transition-all duration-300 ease-in-out"
          stroke={color}
          strokeWidth={strokeWidth}
          fill="transparent"
          r={radius}
          cx={center}
          cy={center}
          style={{
            strokeDasharray: circumference,
            strokeDashoffset: offset,
          }}
        />
      </svg>
      {icon && (
          <div className="absolute inset-0 flex items-center justify-center text-white">
              {icon}
          </div>
      )}
    </div>
  );
};
