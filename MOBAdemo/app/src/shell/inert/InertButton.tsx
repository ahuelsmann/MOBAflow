import { useId, useState, type ReactNode } from 'react';
import texts from '../../examples/texts.json';
import './inert.css';

interface InertButtonProps {
  children: ReactNode;
  label?: string;
  className?: string;
}

export function InertButton({ children, label, className }: Readonly<InertButtonProps>) {
  const hintId = useId();
  const [hovered, setHovered] = useState(false);
  const [focused, setFocused] = useState(false);

  return (
    <span className="demo-inert-control">
      <button
        type="button"
        className={className}
        aria-label={label}
        aria-disabled="true"
        aria-describedby={hintId}
        onPointerEnter={() => setHovered(true)}
        onPointerLeave={() => setHovered(false)}
        onFocus={() => setFocused(true)}
        onBlur={() => setFocused(false)}
      >
        {children}
      </button>
      <span id={hintId} className="demo-inert-hint" role="tooltip" hidden={!hovered && !focused}>
        {texts.inertHint}
      </span>
    </span>
  );
}
