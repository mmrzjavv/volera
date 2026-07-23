/**
 * Returns 1–2 letter initials from a display name (e.g. "John Doe" → "JD", "Mary Jane" → "MJ").
 * Used for avatar fallbacks when no profile picture is available.
 */
export function getInitials(displayName: string | null | undefined): string {
  if (!displayName || typeof displayName !== 'string') return '?';
  const parts = displayName.trim().split(/\s+/).filter(Boolean);
  if (parts.length === 0) return '?';
  if (parts.length === 1) return parts[0].slice(0, 2).toUpperCase();
  return (parts[0][0] + parts[parts.length - 1][0]).toUpperCase();
}
