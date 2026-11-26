import { hostname, userInfo } from 'node:os';
import { expect, type BrowserContext, type Locator, type Page } from '@playwright/test';

export interface TrackedTransfer {
  url: string;
  mimeType: string;
  encodedDataLength: number;
}

export interface TrackedResponseMeta {
  url: string;
  mimeType: string;
  contentEncoding?: string;
  status: number;
}

function escapeRegExp(value: string): string {
  return value.replaceAll(/[.*+?^${}()|[\]\\]/g, String.raw`\$&`);
}

export function buildHostDetailPatterns(): RegExp[] {
  const patterns: RegExp[] = [
    /[A-Za-z]:[\\/](?:Users|Repo|Windows|Program Files)/i,
    /(?:^|[^\w./])\/(?:home|Users|root)\/[\w.-]+/,
    /\b(?:10\.\d{1,3}|192\.168|172\.(?:1[6-9]|2\d|3[01])|169\.254)\.\d{1,3}\.\d{1,3}\b/,
    /(?:localhost|127\.0\.0\.1|0\.0\.0\.0|\[::1\]|\.local|\.internal|\.lan):\d{2,5}|:(?:5000|5001|5173|8080)\b/i,
  ];

  const identityNames = new Set<string>();
  const machine = hostname();
  if (machine.length >= 3) identityNames.add(machine);
  try {
    const user = userInfo().username;
    if (user.length >= 3) identityNames.add(user);
  } catch {
    // userInfo() may fail in restricted environments.
  }

  for (const name of identityNames) {
    const escaped = escapeRegExp(name);
    patterns.push(
      new RegExp(String.raw`(?:Users|home)[\\/]${escaped}(?![\w.-])`, 'i'),
      new RegExp(String.raw`\\\\${escaped}[\\/]`, 'i'),
      new RegExp(String.raw`://${escaped}(?![\w.-])`, 'i'),
      new RegExp(String.raw`(?<![\w.-])${escaped}\.(?:local|lan|internal)\b`, 'i'),
    );
  }

  return patterns;
}

const HOST_DETAIL_PATTERNS = buildHostDetailPatterns();

export function assertNoHostLeak(label: string, text: string): void {
  for (const pattern of HOST_DETAIL_PATTERNS) {
    const match = pattern.exec(text);
    expect(match, `${label} exposed host detail "${match?.[0] ?? ''}" matching ${pattern}`).toBeNull();
  }
}

export async function clickAtCenter(page: Page, locator: Locator): Promise<void> {
  await locator.scrollIntoViewIfNeeded();
  const box = await locator.boundingBox();
  expect(box).not.toBeNull();
  await page.mouse.click(box!.x + box!.width / 2, box!.y + box!.height / 2);
}

export async function attachTransferRecorder(context: BrowserContext, page: Page) {
  const cdp = await context.newCDPSession(page);
  await cdp.send('Network.enable');

  const responsesByRequest = new Map<string, TrackedResponseMeta>();
  let currentTransfers: TrackedTransfer[] = [];

  cdp.on('Network.responseReceived', (event) => {
    const headerEntry = Object.entries(event.response.headers).find(
      ([key]) => key.toLowerCase() === 'content-encoding',
    );
    responsesByRequest.set(event.requestId, {
      url: event.response.url,
      mimeType: event.response.mimeType,
      contentEncoding: headerEntry?.[1],
      status: event.response.status,
    });
  });

  cdp.on('Network.loadingFinished', (event) => {
    const meta = responsesByRequest.get(event.requestId);
    if (!meta) return;
    currentTransfers.push({
      url: meta.url,
      mimeType: meta.mimeType,
      encodedDataLength: event.encodedDataLength,
    });
  });

  return {
    getTransfers: () => currentTransfers,
    resetTransfers: () => {
      currentTransfers = [];
    },
    getResponses: () => [...responsesByRequest.values()],
  };
}
