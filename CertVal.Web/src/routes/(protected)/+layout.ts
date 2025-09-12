import { redirect } from '@sveltejs/kit';
import { browser } from '$app/environment';
import type { LayoutLoad } from './$types';

export const load: LayoutLoad = async ({ url }) => {
    // Only check auth on client side to avoid SSR issues
    if (browser) {
        const token = localStorage.getItem('auth_token');
        if (!token) {
            throw redirect(302, '/auth/login');
        }
    }

    return {
        pathname: url.pathname
    };
};