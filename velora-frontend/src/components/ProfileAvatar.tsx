import { useEffect, useState } from 'react';
import { ensureReachableMediaUrl } from '../utils/ensureReachableMediaUrl';
import { getInitials } from '../utils/getInitials';
import { clsx } from 'clsx';

interface ProfileAvatarProps {
  src?: string | null;
  name: string;
  className?: string;
  textClassName?: string;
}

/**
 * Avatar that re-signs unreachable storage URLs (localhost MinIO / object keys)
 * and falls back to initials when the image fails to load.
 */
export function ProfileAvatar({ src, name, className, textClassName }: ProfileAvatarProps) {
  const [resolvedSrc, setResolvedSrc] = useState<string | null>(null);
  const [failed, setFailed] = useState(false);

  useEffect(() => {
    setFailed(false);
    if (!src) {
      setResolvedSrc(null);
      return;
    }

    let cancelled = false;
    ensureReachableMediaUrl(src)
      .then((url) => {
        if (!cancelled) setResolvedSrc(url || null);
      })
      .catch(() => {
        if (!cancelled) setResolvedSrc(src);
      });

    return () => {
      cancelled = true;
    };
  }, [src]);

  if (!src || failed || !resolvedSrc) {
    return (
      <span className={clsx('flex items-center justify-center w-full h-full font-bold', textClassName)}>
        {getInitials(name)}
      </span>
    );
  }

  return (
    <img
      src={resolvedSrc}
      alt={name}
      className={clsx('w-full h-full object-cover', className)}
      onError={() => setFailed(true)}
    />
  );
}
