import { writable } from 'svelte/store';
import { browser } from '$app/environment';
import type { Theme } from '$lib/types';

interface ThemeState {
    theme: Theme;
    resolved: 'light' | 'dark';
}

function applyTheme(theme: Theme) {
    if (!browser) return;
    const root = document.documentElement;
    root.setAttribute('data-theme', theme);
}

function createThemeStore() {
    const { subscribe, set, update } = writable<ThemeState>({
        theme: 'light',
        resolved: 'light'
    });

    return {
        subscribe,
        setTheme: (newTheme: Theme) => {
            if (!browser) return;

            document.cookie = `theme=${newTheme}; path=/; max-age=31536000; samesite=lax`;

            applyTheme(newTheme);

            update(state => ({
                ...state,
                theme: newTheme,
                resolved: newTheme
            }));
        },
        initialize: (initialTheme: Theme) => {
            if (!browser) return;

            const themeToApply = initialTheme || 'light';
            applyTheme(themeToApply);

            set({
                theme: themeToApply,
                resolved: themeToApply
            });
        }
    };
}

export const theme = createThemeStore();