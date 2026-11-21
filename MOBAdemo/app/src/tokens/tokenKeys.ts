/** Returns the sorted custom property names declared in a style sheet (comments and var() uses excluded). */
export function parseTokenKeys(css: string): string[] {
  const withoutComments = css.replace(/\/\*[\s\S]*?\*\//g, '');
  const keys = [...withoutComments.matchAll(/(?:^|[{;\s])(--[a-z0-9-]+)\s*:/g)].map(
    (match) => match[1],
  );
  return keys.sort((a, b) => a.localeCompare(b));
}
