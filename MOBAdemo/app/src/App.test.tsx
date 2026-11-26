import { render, screen } from '@testing-library/react';
import { describe, expect, it } from 'vitest';
import { App } from './App';

function deferred() {
  let resolve!: () => void;
  let reject!: (reason: unknown) => void;
  const promise = new Promise<void>((res, rej) => {
    resolve = res;
    reject = rej;
  });
  return { promise, resolve, reject };
}

describe('App start-up states', () => {
  it('shows the loading state until the data is ready (FR-052)', async () => {
    const pending = deferred();
    render(<App load={() => pending.promise} />);

    expect(screen.getByRole('status')).toHaveTextContent('Loading MOBAdemo');
    expect(screen.queryByRole('main')).not.toBeInTheDocument();

    pending.resolve();

    expect(await screen.findByRole('main')).toBeInTheDocument();
    expect(screen.queryByRole('status')).not.toBeInTheDocument();
  });

  it('shows a visible error when a data file fails to load', async () => {
    render(<App load={() => Promise.reject(new Error('locomotives.json: 404'))} />);

    const alert = await screen.findByRole('alert');
    expect(alert).toHaveTextContent('could not be loaded');
    expect(alert).not.toHaveTextContent('locomotives.json: 404');
    expect(screen.queryByRole('main')).not.toBeInTheDocument();
    expect(screen.queryByRole('status')).not.toBeInTheDocument();
  });
});
