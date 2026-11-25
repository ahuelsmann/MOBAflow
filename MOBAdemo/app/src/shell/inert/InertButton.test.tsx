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
