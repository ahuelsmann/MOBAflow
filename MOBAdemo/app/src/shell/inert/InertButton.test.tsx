import { fireEvent, render, screen } from '@testing-library/react';
import { expect, it, vi } from 'vitest';
import { InertButton } from './InertButton';

const hint = 'Available in the MOBAflow desktop app';

it('leaves the form unchanged and exposes the unavailable control and its hint', () => {
  const submit = vi.fn((event: React.FormEvent<HTMLFormElement>) => event.preventDefault());
  render(<form onSubmit={submit}><InertButton>Reset</InertButton></form>);
  const button = screen.getByRole('button', { name: 'Reset' });
  expect(button).toHaveAttribute('aria-disabled', 'true');
  expect(button).toHaveAccessibleDescription(hint);
  fireEvent.click(button);
  expect(submit).not.toHaveBeenCalled();
});

it('shows the hint through both hover and focus', () => {
  render(<InertButton>Simulate</InertButton>);
  const button = screen.getByRole('button', { name: 'Simulate' });
  expect(screen.queryByRole('tooltip')).not.toBeInTheDocument();
  fireEvent.pointerEnter(button);
  expect(screen.getByRole('tooltip')).toHaveTextContent(hint);
  fireEvent.focus(button);
  fireEvent.pointerLeave(button);
  expect(screen.getByRole('tooltip')).toBeVisible();
  fireEvent.blur(button);
  expect(screen.queryByRole('tooltip')).not.toBeInTheDocument();
});

it('toggles a custom hint on tap without changing the pressed state', () => {
  render(<InertButton hint="Read-only preview" pressed>Power</InertButton>);
  const button = screen.getByRole('button', { name: 'Power' });
  expect(button).toHaveAccessibleDescription('Read-only preview');
  fireEvent.click(button);
  expect(screen.getByRole('tooltip')).toHaveTextContent('Read-only preview');
  expect(button).toHaveAttribute('aria-pressed', 'true');
  fireEvent.click(button);
  expect(screen.queryByRole('tooltip')).not.toBeInTheDocument();
  expect(button).toHaveAttribute('aria-pressed', 'true');
});

it('closes a tapped hint when another element is pressed', () => {
  render(
    <>
      <InertButton>First</InertButton>
      <InertButton>Second</InertButton>
    </>,
  );
  const first = screen.getByRole('button', { name: 'First' });
  const second = screen.getByRole('button', { name: 'Second' });
  fireEvent.click(first);
  expect(screen.getByRole('tooltip')).toBeVisible();
  fireEvent.pointerDown(second);
  expect(screen.queryByRole('tooltip')).not.toBeInTheDocument();
});

it('dismisses a hover hint on Escape without moving focus', () => {
  render(<InertButton>Simulate</InertButton>);
  const button = screen.getByRole('button', { name: 'Simulate' });
  fireEvent.pointerEnter(button);
  expect(screen.getByRole('tooltip')).toBeVisible();

  fireEvent.keyDown(document, { key: 'Escape' });
  expect(screen.queryByRole('tooltip')).not.toBeInTheDocument();
  expect(button).toHaveAccessibleDescription(hint);
  fireEvent.pointerLeave(button);
  fireEvent.pointerEnter(button);
  expect(screen.getByRole('tooltip')).toBeVisible();
});

it('clamps the hint to the viewport and moves it above a bottom-edge trigger', () => {
  const viewport = document.documentElement;
  const width = vi.spyOn(viewport, 'clientWidth', 'get').mockReturnValue(320);
  const height = vi.spyOn(viewport, 'clientHeight', 'get').mockReturnValue(480);
  const boxWidth = vi.spyOn(HTMLElement.prototype, 'offsetWidth', 'get').mockReturnValue(180);
  const boxHeight = vi.spyOn(HTMLElement.prototype, 'offsetHeight', 'get').mockReturnValue(48);
  try {
    render(<InertButton>Simulate</InertButton>);
    const button = screen.getByRole('button', { name: 'Simulate' });
    vi.spyOn(button, 'getBoundingClientRect').mockReturnValue(new DOMRect(280, 432, 40, 40));
    fireEvent.focus(button);
    const tooltip = screen.getByRole('tooltip');
    expect(tooltip.style.left).toBe('132px');
    expect(tooltip.style.top).toBe('384px');
  } finally {
    width.mockRestore();
    height.mockRestore();
    boxWidth.mockRestore();
    boxHeight.mockRestore();
  }
});
