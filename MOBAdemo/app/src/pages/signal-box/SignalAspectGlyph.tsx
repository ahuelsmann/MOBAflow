import type { SignalAspect } from '../../examples/types';

interface SignalAspectGlyphProps {
  aspectId: SignalAspect['id'];
  className?: string;
}

/**
 * Renders the Ks signal housing and lit LED positions for an aspect code so
 * aspect recognition never relies on color alone (FR-045, FR-046).
 */
export function SignalAspectGlyph({ aspectId, className }: Readonly<SignalAspectGlyphProps>) {
  const hp0Lit = aspectId === 'Hp0';
  const ks1Lit = aspectId === 'Ks1';
  const ks2Lit = aspectId === 'Ks2';
  const sh1Lit = aspectId === 'Sh1';

  return (
    <svg
      aria-hidden="true"
      className={className ? `signal-aspect-glyph ${className}` : 'signal-aspect-glyph'}
      viewBox="0 0 40 52"
    >
      <rect className="signal-glyph-mast" x="18" y="34" width="4" height="16" rx="1.5" />
      <rect className="signal-glyph-housing" x="6" y="4" width="28" height="34" rx="4" />

      {/* Top-right: Hp 0 red stop lamp */}
      <circle
        className={hp0Lit ? 'signal-lamp signal-lamp-red' : 'signal-lamp signal-lamp-off'}
        cx="24"
        cy="13"
        r="4"
      />

      {/* Middle-left: Ks 1 green proceed lamp */}
      <circle
        className={ks1Lit ? 'signal-lamp signal-lamp-green' : 'signal-lamp signal-lamp-off'}
        cx="14"
        cy="21"
        r="4"
      />

      {/* Middle-right: Ks 2 yellow expect-stop lamp (flashing indicator) */}
      <circle
        className={ks2Lit ? 'signal-lamp signal-lamp-yellow signal-lamp-flashing' : 'signal-lamp signal-lamp-off'}
        cx="26"
        cy="21"
        r="4"
      />

      {/* Sh 1 diagonal white lamps (bottom-left and center-right) */}
      <circle
        className={sh1Lit ? 'signal-lamp signal-lamp-white' : 'signal-lamp signal-lamp-off'}
        cx="13"
        cy="31"
        r="3"
      />
      <circle
        className={sh1Lit ? 'signal-lamp signal-lamp-white' : 'signal-lamp signal-lamp-off'}
        cx="22"
        cy="26"
        r="3"
      />
    </svg>
  );
}
