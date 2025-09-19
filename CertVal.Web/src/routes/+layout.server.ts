import type { LayoutServerLoad } from './$types';

export const load: LayoutServerLoad = ({ cookies }) => {
    const theme = (cookies.get('theme') as 'light' | 'dark') || 'light';
    const language = (cookies.get('language') as 'uk' | 'en') || 'uk';

    return {
        theme,
        language
    };
};