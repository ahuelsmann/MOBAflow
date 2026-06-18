#!/usr/bin/env python3
"""Optimize function symbol PNGs under MOBAflow/Assets/FunctionSymbols.

Normalizes each PNG in dark|light / 20|32:
- RGBA, exact folder size (20 or 32 px)
- PNG level-9 compression with optimizer pass
"""
from __future__ import annotations

import argparse
import sys
from pathlib import Path

from PIL import Image

REPO_ROOT = Path(__file__).resolve().parents[1]
SYMBOLS_ROOT = REPO_ROOT / "MOBAflow" / "Assets" / "FunctionSymbols"
THEMES = ("dark", "light")
SIZES = (20, 32)


def optimize_rgba(img: Image.Image, target_size: int) -> Image.Image:
    img = img.convert("RGBA")
    if img.size != (target_size, target_size):
        img = img.resize((target_size, target_size), Image.Resampling.LANCZOS)
    return img


def save_optimized(img: Image.Image, path: Path) -> int:
    path.parent.mkdir(parents=True, exist_ok=True)
    img.save(path, format="PNG", optimize=True, compress_level=9)
    return path.stat().st_size


def folder_bytes(folder: Path) -> int:
    return sum(path.stat().st_size for path in folder.glob("*.png"))


def optimize_all(root: Path) -> tuple[int, int]:
    before = 0
    after = 0
    for theme in THEMES:
        for size in SIZES:
            folder = root / theme / str(size)
            if not folder.is_dir():
                continue
            for path in sorted(folder.glob("*.png")):
                before += path.stat().st_size
                with Image.open(path) as img:
                    optimized = optimize_rgba(img, size)
                    after += save_optimized(optimized, path)
    return before, after


def main() -> int:
    parser = argparse.ArgumentParser(description="Optimize MOBAflow function symbol PNG assets.")
    parser.add_argument(
        "--root",
        type=Path,
        default=SYMBOLS_ROOT,
        help="FunctionSymbols root directory",
    )
    args = parser.parse_args()
    root: Path = args.root

    if not root.is_dir():
        print(f"FunctionSymbols folder not found: {root}", file=sys.stderr)
        return 1

    before = sum(folder_bytes(root / theme / str(size)) for theme in THEMES for size in SIZES)
    optimized_before, optimized_after = optimize_all(root)
    after = sum(folder_bytes(root / theme / str(size)) for theme in THEMES for size in SIZES)

    print(f"Processed bytes (sum per file before write): {optimized_before} -> {optimized_after}")
    print(f"Folder total: {before} -> {after} bytes ({before - after} saved)")
    print(f"Files: {sum(1 for _ in root.rglob('*.png'))}")
    return 0


if __name__ == "__main__":
    raise SystemExit(main())
