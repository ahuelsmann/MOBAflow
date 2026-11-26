import { fireEvent, render, screen, within } from '@testing-library/react';
import { expect, it } from 'vitest';
import { App } from '../../App';

const load = async () => {};
const initialInfo = 'Visual web demo: prepared example views, no hardware connected. Switch between the pages and the theme and try the speed slider; the other controls work in the MOBAflow desktop app.';

it('shows the active theme toggle beside the inert command bar', async () => {
  render(<App load={load} />);
  await screen.findByRole('main');
  const header = screen.getByRole('banner');
  const theme = within(header).getByRole('button', { name: 'Light theme' });
  expect(theme).not.toHaveAttribute('aria-disabled');
  const commands = within(header).getByRole('group', { name: 'Commands' });
  for (const name of ['New', 'Open', 'Simulate', 'Reset']) {
    expect(within(commands).getByRole('button', { name })).toHaveAttribute('aria-disabled', 'true');
  }
  for (const name of ['New', 'Open']) {
    expect(within(commands).getByRole('button', { name })).toHaveAccessibleDescription('Fixed example project in the web demo');
  }
  const open = within(commands).getByRole('button', { name: 'Open' });
  fireEvent.focus(open);
  expect(screen.getByRole('tooltip')).toBeVisible();
  expect(screen.getByRole('tooltip')).toHaveTextContent('Fixed example project in the web demo');
  fireEvent.blur(open);

  const inPort = within(commands).getByRole('spinbutton', { name: 'Feedback Point (InPort)' });
  expect(inPort).toHaveAttribute('readonly');
  expect(inPort).toHaveAttribute('aria-disabled', 'true');
  expect(inPort).toHaveValue(0);
});

it('shows the first-visit message expanded and keeps all bars unchanged on activation', async () => {
  render(<App load={load} />);
  const main = await screen.findByRole('main');
  const messages = screen.getByRole('region', { name: 'Messages' });
  const status = screen.getByRole('contentinfo', { name: 'Demo status' });
  expect(within(messages).getByText(initialInfo)).toBeVisible();
  const expander = within(messages).getByRole('button', { name: 'Messages' });
  expect(expander).toHaveAttribute('aria-expanded', 'true');
  for (const name of ['Errors 0', 'Warnings 0', 'Information 1', 'Refresh']) {
    expect(within(messages).getByRole('button', { name })).toHaveAttribute('aria-disabled', 'true');
  }
  const segments = within(status).getAllByRole('listitem');
  expect(segments).toHaveLength(4);
  [
    'Solution: MOBAflow demo layout', 'Z21: Not connected (web demo)',
    'REST API: Not available in the web demo', 'Speech: Not available in the web demo',
  ].forEach((text, index) => expect(segments[index]).toHaveTextContent(text));
  const power = within(status).getByRole('button', { name: 'Track Power' });
  expect(power).toHaveAttribute('aria-pressed', 'false');
  expect(within(status).getByRole('button', { name: 'Speech provider' })).toHaveAttribute('aria-disabled', 'true');

  const heading = within(main).getByRole('heading', { level: 1 }).textContent;
  const inert = screen.getAllByRole('button').filter(button => button.getAttribute('aria-disabled') === 'true');
  inert.forEach(button => fireEvent.click(button));
  expect(within(main).getByRole('heading', { level: 1 })).toHaveTextContent(heading!);
  expect(expander).toHaveAttribute('aria-expanded', 'true');
  expect(power).toHaveAttribute('aria-pressed', 'false');
  expect(within(messages).getByText(initialInfo)).toBeVisible();
});

it('adds a warning on load failure without exposing the underlying host details', async () => {
  const privateDetails = 'https://internal-host:8080/private/example-data.json';
  render(<App load={() => Promise.reject(new Error(privateDetails))} />);
  expect(await screen.findByRole('alert')).toHaveTextContent('The demo data could not be loaded.');
  const messages = screen.getByRole('region', { name: 'Messages' });
  expect(within(messages).getByRole('button', { name: 'Warnings 1' })).toBeVisible();
  expect(within(messages).getByText(initialInfo)).toBeVisible();
  expect(within(messages).getByText('Warning')).toBeVisible();
  expect(document.body).not.toHaveTextContent(privateDetails);
});
