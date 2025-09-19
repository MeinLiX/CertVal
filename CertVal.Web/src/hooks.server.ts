import type { Handle } from '@sveltejs/kit';

export const handle: Handle = async ({ event, resolve }) => {
    const theme = (event.cookies.get('theme') as 'light' | 'dark') || 'light';
    const lang = (event.cookies.get('language') as 'uk' | 'en') || 'uk';

    return await resolve(event, {
        transformPageChunk: ({ html }) =>
            html.replace('<html lang="en">', `<html lang="${lang}" data-theme="${theme}">`)
    });
};