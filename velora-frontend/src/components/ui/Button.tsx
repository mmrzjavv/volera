import React from 'react';
import { twMerge } from 'tailwind-merge';
import { Loader2 } from 'lucide-react';

interface ButtonProps extends React.ButtonHTMLAttributes<HTMLButtonElement> {
  variant?: 'primary' | 'secondary' | 'danger';
  size?: 'sm' | 'md' | 'lg';
  isLoading?: boolean;
}

export const Button = React.forwardRef<HTMLButtonElement, ButtonProps>(
  ({ className, variant = 'primary', size = 'md', isLoading, children, disabled, ...props }, ref) => {
    const variants = {
      primary:
        'bg-[var(--volera-accent)] text-white hover:bg-[var(--volera-accent-hover)]',
      secondary:
        'bg-[var(--volera-surface-muted)] text-[var(--volera-text)] hover:opacity-90 border border-[var(--volera-border)]',
      danger:
        'bg-[var(--volera-danger)] text-white hover:opacity-90',
    };

    const sizes = {
      sm: 'px-3 py-1.5 text-sm min-h-[36px]',
      md: 'px-4 py-2 min-h-[44px]',
      lg: 'px-6 py-3 text-lg min-h-[48px]',
    };

    return (
      <button
        ref={ref}
        className={twMerge(
          'rounded-[var(--volera-radius-sm)] font-medium transition-[var(--volera-motion)] disabled:opacity-50 disabled:cursor-not-allowed flex items-center justify-center gap-2',
          variants[variant],
          sizes[size],
          className
        )}
        disabled={isLoading || disabled}
        {...props}
      >
        {isLoading && <Loader2 className="w-4 h-4 animate-spin" />}
        {children}
      </button>
    );
  }
);

Button.displayName = 'Button';
