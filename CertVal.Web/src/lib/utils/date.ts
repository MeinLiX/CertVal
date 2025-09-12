export function formatDate(dateString: string): string {
    const date = new Date(dateString);
    return date.toLocaleDateString('uk-UA', {
        year: 'numeric',
        month: 'long',
        day: 'numeric'
    });
}

export function formatDateTime(dateString: string): string {
    const date = new Date(dateString);
    return date.toLocaleString('uk-UA', {
        year: 'numeric',
        month: 'short',
        day: 'numeric',
        hour: '2-digit',
        minute: '2-digit'
    });
}

export function getDaysUntilExpiry(expiryDate: string): number {
    const expiry = new Date(expiryDate);
    const now = new Date();
    const diffTime = expiry.getTime() - now.getTime();
    return Math.ceil(diffTime / (1000 * 60 * 60 * 24));
}

export function getCertificateStatus(expiryDate: string): 'expired' | 'expiring' | 'valid' {
    const days = getDaysUntilExpiry(expiryDate);
    if (days <= 0) return 'expired';
    if (days <= 30) return 'expiring';
    return 'valid';
}