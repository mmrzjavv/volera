import React, { useId } from 'react';
import { twMerge } from 'tailwind-merge';

interface InputProps extends React.InputHTMLAttributes<HTMLInputElement> {
  label?: string;
}

export function Input({ label, className, id: idProp, ...props }: InputProps) {
  const generatedId = useId();
  const id = idProp ?? generatedId;
  return (
    <div className="flex flex-col gap-1">
      {label && (
        <label htmlFor={id} className="text-sm font-medium text-[var(--volera-text-muted)]">
          {label}
        </label>
      )}
      <input
        id={id}
        className={twMerge(
          'px-3 py-2 min-h-[44px] border border-[var(--volera-border)] rounded-[var(--volera-radius-sm)] bg-[var(--volera-surface-muted)] text-[var(--volera-text)] placeholder:text-[var(--volera-text-muted)]/70 focus:outline-none focus:ring-2 focus:ring-[var(--volera-accent)] focus:border-transparent transition-[var(--volera-motion)]',
          className
        )}
        {...props}
      />
    </div>
  );
}
