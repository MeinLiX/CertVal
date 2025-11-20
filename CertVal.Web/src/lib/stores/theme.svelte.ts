import { browser } from '$app/environment';
import type { Theme } from '$lib/types';

class ThemeStore {
    current = $state<Theme>('light');

    constructor() {
        if (browser) {
            const saved = localStorage.getItem('theme') as Theme;
            if (saved && (saved === 'light' || saved === 'dark')) {
                this.current = saved;
                this.apply(saved);
            } else if (window.matchMedia('(prefers-color-scheme: dark)').matches) {
                this.current = 'dark';
                this.apply('dark');
            }
        }
    }

    set(theme: Theme) {
        this.current = theme;
        this.apply(theme);
        if (browser) {
            localStorage.setItem('theme', theme);
            document.cookie = `theme=${theme}; path=/; max-age=31536000`;
        }
    }

    private apply(theme: Theme) {
        if (!browser) return;
        document.documentElement.setAttribute('data-theme', theme);
    }
}

export const theme = new ThemeStore();
