import { writable } from 'svelte/store';
import { browser } from '$app/environment';

export type Theme = 'light' | 'dark' | 'system';

interface ThemeState {
    theme: Theme;
    resolved: 'light' | 'dark';
}

function createThemeStore() {
    const { subscribe, set, update } = writable<ThemeState>({
        theme: 'system',
        resolved: 'light'
    });

    return {
        subscribe,
        setTheme: (newTheme: Theme) => {
            if (!browser) return;
            
            localStorage.setItem('theme', newTheme);
            
            const resolved = getResolvedTheme(newTheme);
            applyTheme(resolved);
            
            update(state => ({
                ...state,
                theme: newTheme,
                resolved
            }));
        },
        initialize: () => {
            if (!browser) return;
            
            const savedTheme = localStorage.getItem('theme') as Theme || 'system';
            const resolved = getResolvedTheme(savedTheme);
            
            applyTheme(resolved);
            
            set({
                theme: savedTheme,
                resolved
            });

            // Listen for system theme changes
            const mediaQuery = window.matchMedia('(prefers-color-scheme: dark)');
            const handleChange = () => {
                update(state => {
                    if (state.theme === 'system') {
                        const newResolved = mediaQuery.matches ? 'dark' : 'light';
                        applyTheme(newResolved);
                        return {
                            ...state,
                            resolved: newResolved
                        };
                    }
                    return state;
                });
            };

            mediaQuery.addEventListener('change', handleChange);

            return () => {
                mediaQuery.removeEventListener('change', handleChange);
            };
        }
    };
}

function getResolvedTheme(theme: Theme): 'light' | 'dark' {
    if (theme === 'system') {
        return browser && window.matchMedia('(prefers-color-scheme: dark)').matches 
            ? 'dark' 
            : 'light';
    }
    return theme;
}

function applyTheme(theme: 'light' | 'dark') {
    if (!browser) return;
    
    const root = document.documentElement;
    
    if (theme === 'dark') {
        root.classList.add('dark');
    } else {
        root.classList.remove('dark');
    }
}

export const theme = createThemeStore();