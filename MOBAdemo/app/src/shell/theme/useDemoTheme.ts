import { useLayoutEffect, useState } from 'react';
import '../../tokens/light.css';

type Theme = 'dark' | 'light';
const themeStorageKey = 'mobademo-theme';

function readTheme(): Theme {
  try {
    return localStorage.getItem(themeStorageKey) === 'light' ? 'light' : 'dark';
  } catch {
    return 'dark';
  }
}

export function useDemoTheme() {
  const [theme, setTheme] = useState(readTheme);

  useLayoutEffect(() => {
    document.documentElement.dataset.theme = theme;
    return () => {
      delete document.documentElement.dataset.theme;
    };
  }, [theme]);

  function toggleTheme() {
    const next = theme === 'dark' ? 'light' : 'dark';
    setTheme(next);
    try {
      localStorage.setItem(themeStorageKey, next);
    } catch {
      // The theme still works for this visit when browser storage is unavailable.
    }
  }

  return { theme, toggleTheme };
}
