import type { Handle } from '@sveltejs/kit';

export const handle: Handle = async ({ event, resolve }) => {
    const theme = event.cookies.get('theme') || 'light';

    return await resolve(event, {
        transformPageChunk: ({ html }) =>
            html.replace('<html lang="en">', `<html lang="en" data-theme="${theme}">`)
    });
};