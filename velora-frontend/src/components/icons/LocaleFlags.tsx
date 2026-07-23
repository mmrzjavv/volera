import { clsx } from 'clsx';

type FlagProps = {
  className?: string;
  title?: string;
};

/** Historical Iran flag (Lion and Sun) — not the Islamic Republic tricolor emblem. */
export function IranLionSunFlag({ className, title = 'Iran (Lion and Sun)' }: FlagProps) {
  return (
    <svg
      viewBox="0 0 24 16"
      className={clsx('inline-block shrink-0 rounded-[2px] border border-black/10', className)}
      role="img"
      aria-hidden={title ? undefined : true}
      aria-label={title}
    >
      <rect width="24" height="5.33" fill="#239F40" />
      <rect y="5.33" width="24" height="5.34" fill="#FFFFFF" />
      <rect y="10.67" width="24" height="5.33" fill="#DA0000" />
      <g transform="translate(12 8)">
        <circle r="2.4" fill="none" stroke="#DA0000" strokeWidth="0.35" />
        <circle r="1.9" fill="none" stroke="#DA0000" strokeWidth="0.2" />
        <path
          d="M-1.1 0.3c0.3-0.7 1.1-1.1 1.9-0.9 0.5 0.2 0.8 0.6 0.8 1.1 0 0.7-0.6 1.3-1.3 1.3-0.4 0-0.7-0.2-1-0.4-0.2 0.3-0.6 0.5-1 0.4-0.7-0.2-1.1-0.9-0.9-1.6 0.2-0.5 0.6-0.9 1.1-1 0.1 0.5 0.3 0.9 0.6 1.1z"
          fill="#E8B923"
          stroke="#C9A020"
          strokeWidth="0.15"
        />
        <path d="M0.7-0.2l0.6 0.3-0.4 0.5 0.5-0.1 0.2 0.6-0.6-0.2-0.3 0.5-0.1-0.6-0.5 0.3 0.4-0.4-0.5-0.1z" fill="#DA0000" />
      </g>
    </svg>
  );
}

export function UsFlag({ className, title = 'United States' }: FlagProps) {
  return (
    <svg
      viewBox="0 0 24 16"
      className={clsx('inline-block shrink-0 rounded-[2px] border border-black/10', className)}
      role="img"
      aria-hidden={title ? undefined : true}
      aria-label={title}
    >
      <rect width="24" height="16" fill="#B22234" />
      {[0, 2, 4, 6, 8, 10, 12].map((y) => (
        <rect key={y} y={y * 1.23} width="24" height="1.23" fill="#FFFFFF" />
      ))}
      <rect width="9.6" height="8.6" fill="#3C3B6E" />
      {[0, 1, 2, 3, 4].flatMap((row) =>
        [0, 1, 2, 3, 4, 5].map((col) => (
          <circle
            key={`${row}-${col}`}
            cx={1 + col * 1.5 + (row % 2 ? 0.75 : 0)}
            cy={1 + row * 1.5}
            r="0.35"
            fill="#FFFFFF"
          />
        ))
      )}
    </svg>
  );
}
