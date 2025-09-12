import { writable } from 'svelte/store';
import type { Language } from '$lib/types';

function createLanguageStore() {
    const { subscribe, set } = writable<Language>('uk');

    return {
        subscribe,
        setLanguage: (lang: Language) => {
            localStorage.setItem('language', lang);
            set(lang);
        },
        initialize: () => {
            const saved = localStorage.getItem('language') as Language;
            if (saved && ['uk', 'en'].includes(saved)) {
                set(saved);
            }
        }
    };
}

export const language = createLanguageStore();