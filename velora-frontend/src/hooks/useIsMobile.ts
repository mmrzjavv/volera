import { useEffect, useState } from 'react';

/** Matches chat layout split (`md` / 768px). */
export function useIsMobile(breakpointPx = 768): boolean {
  const query = `(max-width: ${breakpointPx - 1}px)`;
  const [isMobile, setIsMobile] = useState(
    () => typeof window !== 'undefined' && window.matchMedia(query).matches
  );

  useEffect(() => {
    const mql = window.matchMedia(query);
    const handler = () => setIsMobile(mql.matches);
    handler();
    mql.addEventListener('change', handler);
    return () => mql.removeEventListener('change', handler);
  }, [query]);

  return isMobile;
}
