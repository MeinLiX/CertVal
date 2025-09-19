import { writable } from 'svelte/store';
import type { Language } from '$lib/types';
import { browser } from '$app/environment';

function createLanguageStore() {
    const { subscribe, set } = writable<Language>('uk');

    return {
        subscribe,
        set,
        setLanguage: (lang: Language) => {
            if (browser) {
                document.cookie = `language=${lang}; path=/; max-age=31536000; samesite=lax`;
            }
            set(lang);
        }
    };
}

export const language = createLanguageStore();