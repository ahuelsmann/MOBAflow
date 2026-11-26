import { useEffect, useId, useLayoutEffect, useRef, useState, type ReactNode } from 'react';
import texts from '../../examples/texts.json';
import './inert.css';

interface InertButtonProps {
  children: ReactNode;
  label?: string;
  className?: string;
  hint?: string;
  pressed?: boolean;
  expanded?: boolean;
}

interface InertNumberBoxProps {
  label: string;
  className?: string;
  value: number;
}

export function InertButton(props: Readonly<InertButtonProps>) {
  return <InertControl {...props} />;
}

export function InertNumberBox({ label, className, value }: Readonly<InertNumberBoxProps>) {
  return <InertControl label={label} className={className} numberValue={value} />;
}

function InertControl({
  children, label, className, hint = texts.inertHint, pressed, expanded, numberValue,
}: Readonly<Omit<InertButtonProps, 'children'> & { children?: ReactNode; numberValue?: number }>) {
  const hintId = useId();
  const triggerRef = useRef<HTMLElement>(null);
  const hintRef = useRef<HTMLSpanElement>(null);
  const [hovered, setHovered] = useState(false);
  const [focused, setFocused] = useState(false);
  const [tapped, setTapped] = useState(false);
  const [dismissed, setDismissed] = useState(false);
  const visible = !dismissed && (hovered || focused || tapped);

  useEffect(() => {
    if (!visible) return;
    function onEscape(event: KeyboardEvent) {
      if (event.key !== 'Escape') return;
      setTapped(false);
      setDismissed(true);
    }
    document.addEventListener('keydown', onEscape);
    return () => document.removeEventListener('keydown', onEscape);
  }, [visible]);

  useLayoutEffect(() => {
    if (!visible) return;
    const button = triggerRef.current;
    const tooltip = hintRef.current;
    if (!button || !tooltip) return;

    function positionHint(trigger: HTMLElement, box: HTMLSpanElement) {
      const gap = 8;
      const bounds = trigger.getBoundingClientRect();
      const width = document.documentElement.clientWidth || window.innerWidth;
      const height = document.documentElement.clientHeight || window.innerHeight;
      const left = Math.max(gap, Math.min(bounds.left, width - box.offsetWidth - gap));
      const below = bounds.bottom + box.offsetHeight <= height - gap;
      const top = Math.max(gap, below ? bounds.bottom : bounds.top - box.offsetHeight);
      box.style.left = left + 'px';
      box.style.top = top + 'px';
    }

    const reposition = () => positionHint(button, tooltip);
    reposition();
    window.addEventListener('resize', reposition);
    window.addEventListener('scroll', reposition, true);
    return () => {
      window.removeEventListener('resize', reposition);
      window.removeEventListener('scroll', reposition, true);
    };
  }, [hint, visible]);

  const triggerProps = {
    ref: (node: HTMLElement | null) => { triggerRef.current = node; },
    className,
    'aria-label': label,
    'aria-disabled': true,
    'aria-describedby': hintId,
    onFocus: () => { setFocused(true); setDismissed(false); },
    onBlur: () => { setFocused(false); setTapped(false); },
    onClick: () => { setDismissed(tapped); setTapped(!tapped); },
  };

  return (
    <span
      className="demo-inert-control"
      onPointerEnter={() => { setHovered(true); setDismissed(false); }}
      onPointerLeave={() => setHovered(false)}
    >
      {numberValue === undefined ? (
        <button {...triggerProps} type="button" aria-pressed={pressed} aria-expanded={expanded}>
          {children}
        </button>
      ) : (
        <input {...triggerProps} type="number" readOnly value={numberValue} />
      )}
      <span ref={hintRef} id={hintId} className="demo-inert-hint" role="tooltip" hidden={!visible}>
        {hint}
      </span>
    </span>
  );
}
