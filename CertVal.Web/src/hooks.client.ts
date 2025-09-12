import { auth } from '$lib/stores/auth';
import { language } from '$lib/stores/language';

// Initialize stores on client side
if (typeof window !== 'undefined') {
    auth.initialize();
    language.initialize();
}