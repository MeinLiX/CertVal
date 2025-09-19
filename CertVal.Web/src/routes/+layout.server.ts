import type { LayoutServerLoad } from './$types';

export const load: LayoutServerLoad = ({ cookies }) => {
    const theme = (cookies.get('theme') as 'light' | 'dark') || 'light';
    return {
        theme
    };
};