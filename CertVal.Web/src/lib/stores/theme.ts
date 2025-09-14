import { writable } from 'svelte/store';
import { browser } from '$app/environment';

export type Theme = 'light' | 'dark';

interface ThemeState {
    theme: Theme;
    resolved: 'light' | 'dark';
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
            
            localStorage.setItem('theme', newTheme);
            applyTheme(newTheme);
            
            update(state => ({
                ...state,
                theme: newTheme,
                resolved: newTheme
            }));
        },
        initialize: () => {
            if (!browser) return;
            
            const savedTheme = localStorage.getItem('theme') as Theme;
            const defaultTheme: Theme = savedTheme === 'dark' ? 'dark' : 'light';
            
            applyTheme(defaultTheme);
            
            set({
                theme: defaultTheme,
                resolved: defaultTheme
            });
        }
    };
}

function applyTheme(theme: Theme) {
    if (!browser) return;
    
    const root = document.documentElement;
    
    if (theme === 'dark') {
        root.classList.add('dark');
        root.setAttribute('data-theme', 'dark');
    } else {
        root.classList.remove('dark');
        root.setAttribute('data-theme', 'light');
    }
}

export const theme = createThemeStore();