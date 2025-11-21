import { browser } from '$app/environment';
import type { Language } from '$lib/types';

class LanguageStore {
    current = $state<Language>('en');

    constructor() {
        if (browser) {
            const saved = localStorage.getItem('language') as Language;
            if (saved && (saved === 'uk' || saved === 'en')) {
                this.current = saved;
            } else {
                const browserLang = navigator.language.toLowerCase();
                if (browserLang.startsWith('uk')) {
                    this.current = 'uk';
                }
            }
        }
    }

    set(lang: Language) {
        this.current = lang;
        if (browser) {
            localStorage.setItem('language', lang);
            document.cookie = `language=${lang}; path=/; max-age=31536000`;
        }
    }
}

export const language = new LanguageStore();
