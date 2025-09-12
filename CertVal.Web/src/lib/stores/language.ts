import { writable } from 'svelte/store';
import type { Language } from '$lib/types';

function createLanguageStore() {
    const { subscribe, set } = writable<Language>('uk');

    return {
        subscribe,
        setLanguage: (lang: Language) => {
            if (typeof window !== 'undefined') {
                localStorage.setItem('language', lang);
            }
            set(lang);
        },
        initialize: () => {
            if (typeof window !== 'undefined') {
                const saved = localStorage.getItem('language') as Language;
                if (saved && ['uk', 'en'].includes(saved)) {
                    set(saved);
                }
            }
        }
    };
}

export const language = createLanguageStore();