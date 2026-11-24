// Writes a Brotli (quality 11) and a gzip (level 9) sibling next to every text asset of a build output,
// so the MOBAdemo host can serve the precompressed variant without compressing at run time (spec FR-058).
// It uses only the zlib module that ships with Node. Usage: node scripts/compress-assets.mjs [folder]
import { readdirSync, readFileSync, writeFileSync } from 'node:fs';
import { extname, join, resolve } from 'node:path';
import { brotliCompressSync, constants, gzipSync } from 'node:zlib';

const textExtensions = new Set(['.js', '.css', '.html', '.svg']);

function* listFiles(folder) {
  for (const entry of readdirSync(folder, { withFileTypes: true })) {
    const path = join(folder, entry.name);
    if (entry.isDirectory()) {
      yield* listFiles(path);
    } else if (entry.isFile()) {
      yield path;
    }
  }
}

const folder = resolve(process.argv[2] ?? 'dist');
let count = 0;
for (const file of listFiles(folder)) {
  if (!textExtensions.has(extname(file).toLowerCase())) {
    continue;
  }
  const content = readFileSync(file);
  writeFileSync(
    `${file}.br`,
    brotliCompressSync(content, {
      params: {
        [constants.BROTLI_PARAM_QUALITY]: constants.BROTLI_MAX_QUALITY,
        [constants.BROTLI_PARAM_SIZE_HINT]: content.length,
      },
    }),
  );
  writeFileSync(`${file}.gz`, gzipSync(content, { level: constants.Z_BEST_COMPRESSION }));
  count += 1;
}

if (count === 0) {
  console.error('No text asset found to compress in the build output.');
  process.exit(1);
}
console.log(`Compressed ${count} text assets (Brotli q11 and gzip).`);
