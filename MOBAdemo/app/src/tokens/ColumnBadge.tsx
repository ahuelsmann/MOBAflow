const glyphs = {
  collection: 'M2 4h2m2 0h8M2 8h2m2 0h8M2 12h2m2 0h8',
  helper: 'M2 8h4m4 0h4M8 2v4m0 4v4M6 6h4v4H6z',
  properties: 'M2 4h4m3 0h5M2 8h8m3 0h1M2 12h2m3 0h7M6 3v2m4 2v2m-6 2v2',
};

export function ColumnBadge({ role }: Readonly<{ role: keyof typeof glyphs }>) {
  return (
    <svg aria-hidden="true" className="moba-col-badge" viewBox="0 0 16 16">
      <path d={glyphs[role]} fill="none" stroke="currentColor" strokeLinecap="round" strokeLinejoin="round" />
    </svg>
  );
}
