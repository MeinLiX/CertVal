import type { PageLoad } from './$types';

// Public page – no auth check.
export const load: PageLoad = () => {
    return {};
};

export const prerender = false;
export const ssr = false;
