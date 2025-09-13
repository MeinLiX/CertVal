<script lang="ts">
    import { onMount } from 'svelte';
    import { goto } from '$app/navigation';
    import { page } from '$app/stores';
    import { auth } from '$lib/stores/auth';
    import { language } from '$lib/stores/language';
    import { api } from '$lib/utils/api';
    import { t } from '$lib/i18n';
    import { formatDate, formatDateTime, getCertificateStatus } from '$lib/utils/date';
    import Card from '$lib/components/ui/Card.svelte';
    import Button from '$lib/components/ui/Button.svelte';
    import Modal from '$lib/components/ui/Modal.svelte';
    import type { Certificate } from '$lib/types';

    let certificate = $state<Certificate | null>(null);
    let isLoading = $state(true);
    let showDeleteModal = $state(false);
    let isDeleting = $state(false);
    let error = $state<string>('');

    const certificateId = $derived($page.params.id);

    onMount(async () => {
        if (!$auth.isAuthenticated) {
            goto('/auth/login');
            return;
        }

        await loadCertificate();
    });

    async function loadCertificate() {
        try {
            const response = await api.get<Certificate>(`/v1/certificates/${certificateId}`);
            if (response.data) {
                certificate = response.data;
            } else if (response.message) {
                error = response.message;
            }
        } catch (err) {
            error = t('certificates.failedToLoad', $language);
            console.error('Failed to load certificate:', err);
        } finally {
            isLoading = false;
        }
    }

    async function handleDelete() {
        if (!certificate) return;

        isDeleting = true;
        try {
            const response = await api.delete(`/v1/certificates/${certificateId}`);
            if (response.message && response.message.includes('not found')) {
                error = response.message;
            } else {
                const workspaceParam = certificate.workspaceId ? `?workspace=${certificate.workspaceId}` : '';
                goto(`/certificates${workspaceParam}`);
            }
        } catch (err) {
            error = t('certificates.failedToDelete', $language);
        } finally {
            isDeleting = false;
            showDeleteModal = false;
        }
    }

    function formatFileSize(bytes: number): string {
        if (bytes === 0) return '0 B';
        const k = 1024;
        const sizes = ['B', 'KB', 'MB', 'GB'];
        const i = Math.floor(Math.log(bytes) / Math.log(k));
        return parseFloat((bytes / Math.pow(k, i)).toFixed(1)) + ' ' + sizes[i];
    }

    function getStatusColor(status: 'expired' | 'expiring' | 'valid'): string {
        switch (status) {
            case 'expired':
                return 'text-red-600 bg-red-100 border-red-200';
            case 'expiring':
                return 'text-yellow-600 bg-yellow-100 border-yellow-200';
            case 'valid':
                return 'text-green-600 bg-green-100 border-green-200';
        }
    }

    function getStatusText(status: 'expired' | 'expiring' | 'valid'): string {
        return t(`certificates.${status}`, $language);
    }

    function copyToClipboard(text: string) {
        navigator.clipboard.writeText(text).then(() => {
            // Could show a toast notification here
        }).catch(() => {
            const textArea = document.createElement('textarea');
            textArea.value = text;
            document.body.appendChild(textArea);
            textArea.select();
            document.execCommand('copy');
            document.body.removeChild(textArea);
        });
    }

    function downloadCertificate() {
        // This would need to be implemented in the backend
        // For now, just show a placeholder
        alert(t('certificates.downloadNotImplemented', $language));
    }

    function handleBackNavigation() {
        const referrer = document.referrer;
        const currentOrigin = window.location.origin;
        
        if (referrer && referrer.startsWith(currentOrigin)) {
            window.history.back();
        } else {
            const workspaceParam = certificate?.workspaceId ? `?workspace=${certificate.workspaceId}` : '';
            goto(`/certificates${workspaceParam}`);
        }
    }

    const status = $derived(certificate ? getCertificateStatus(certificate.notAfter) : 'valid');
    const hasChildCertificates = $derived(
        certificate?.childCertificates && certificate.childCertificates.length > 0
    );
</script>

<svelte:head>
    <title>
        {certificate ? `${certificate.subject} - ${t('certificates.details', $language)}` : t('certificates.details', $language)}
    </title>
</svelte:head>

<div class="space-y-6">
    {#if isLoading}
        <div class="flex h-64 items-center justify-center">
            <div class="h-8 w-8 animate-spin rounded-full border-b-2 border-blue-600" aria-hidden="true"></div>
        </div>
    {:else if error}
        <Card>
            <div class="py-12 text-center">
                <svg
                    class="mx-auto h-12 w-12 text-red-400"
                    fill="none"
                    viewBox="0 0 24 24"
                    stroke="currentColor"
                    aria-hidden="true"
                >
                    <path
                        stroke-linecap="round"
                        stroke-linejoin="round"
                        stroke-width="2"
                        d="M12 8v4m0 4h.01M21 12a9 9 0 11-18 0 9 9 0 0118 0z"
                    />
                </svg>
                <h3 class="mt-2 text-sm font-medium text-gray-900">{t('certificates.errorLoading', $language)}</h3>
                <p class="mt-1 text-sm text-gray-500">{error}</p>
                <div class="mt-6">
                    <Button onclick={handleBackNavigation}>
                        {t('common.back', $language)} {t('common.to', $language)} {t('certificates.title', $language)}
                    </Button>
                </div>
            </div>
        </Card>
    {:else if certificate}
        <!-- Header -->
        <div class="flex items-start justify-between">
            <div class="min-w-0 flex-1">
                <div class="flex items-center space-x-3">
                    <Button variant="outline" size="sm" onclick={handleBackNavigation}>
                        <svg class="mr-1 h-4 w-4" fill="none" viewBox="0 0 24 24" stroke="currentColor" aria-hidden="true">
                            <path
                                stroke-linecap="round"
                                stroke-linejoin="round"
                                stroke-width="2"
                                d="M10 19l-7-7m0 0l7-7m-7 7h18"
                            />
                        </svg>
                        {t('common.back', $language)}
                    </Button>
                    {#if certificate.isBundle}
                        <div class="flex items-center text-blue-600" aria-hidden="true">
                            <svg class="mr-1 h-5 w-5" fill="none" viewBox="0 0 24 24" stroke="currentColor" aria-hidden="true">
                                <path
                                    stroke-linecap="round"
                                    stroke-linejoin="round"
                                    stroke-width="2"
                                    d="M19 11H5m14-7v12a2 2 0 01-2 2H7a2 2 0 01-2-2V4a2 2 0 012-2h10a2 2 0 012 2zM9 11h6"
                                />
                            </svg>
                            <span class="text-sm font-medium">{t('certificates.bundle', $language)}</span>
                        </div>
                    {/if}
                </div>
                <h1 class="mt-2 truncate text-2xl font-bold text-gray-900" title={certificate.subject}>
                    {certificate.subject}
                </h1>
                <div class="mt-1 flex items-center space-x-4 text-sm text-gray-500">
                    <span>{t('certificates.file', $language)}: {certificate.originalFileName}</span>
                    <span>•</span>
                    <span>{t('certificates.format', $language)}: {certificate.fileFormat}</span>
                    <span>•</span>
                    <span>{t('certificates.size', $language)}: {formatFileSize(certificate.fileSize)}</span>
                </div>
            </div>

            <div class="flex items-center space-x-3">
                <div class="text-right">
                    <div
                        class="inline-flex rounded-lg border px-3 py-2 text-sm font-semibold {getStatusColor(
                            status
                        )}"
                    >
                        {getStatusText(status)}
                        {#if certificate.daysUntilExpiry > 0}
                            ({certificate.daysUntilExpiry} {t('certificates.daysLeft', $language)})
                        {:else if certificate.daysUntilExpiry === 0}
                            ({t('certificates.expiresToday', $language)})
                        {:else}
                            ({t('certificates.expiredAgo', $language, { days: Math.abs(certificate.daysUntilExpiry) })})
                        {/if}
                    </div>
                </div>

                <div class="flex space-x-2">
                    <Button variant="outline" onclick={downloadCertificate}>
                        <svg class="mr-1 h-4 w-4" fill="none" viewBox="0 0 24 24" stroke="currentColor" aria-hidden="true">
                            <path
                                stroke-linecap="round"
                                stroke-linejoin="round"
                                stroke-width="2"
                                d="M12 10v6m0 0l-3-3m3 3l3-3m2 8H7a2 2 0 01-2-2V5a2 2 0 012-2h5.586a1 1 0 01.707.293l5.414 5.414a1 1 0 01.293.707V19a2 2 0 01-2 2z"
                            />
                        </svg>
                        {t('certificates.download', $language)}
                    </Button>
                    <Button variant="danger" onclick={() => (showDeleteModal = true)}>
                        <svg class="mr-1 h-4 w-4" fill="none" viewBox="0 0 24 24" stroke="currentColor" aria-hidden="true">
                            <path
                                stroke-linecap="round"
                                stroke-linejoin="round"
                                stroke-width="2"
                                d="M19 7l-.867 12.142A2 2 0 0116.138 21H7.862a2 2 0 01-1.995-1.858L5 7m5 4v6m4-6v6m1-10V4a1 1 0 00-1-1h-4a1 1 0 00-1 1v3M4 7h16"
                            />
                        </svg>
                        {t('common.delete', $language)}
                    </Button>
                </div>
            </div>
        </div>

        <div class="grid grid-cols-1 gap-6 lg:grid-cols-3">
            <!-- Main Certificate Details -->
            <div class="space-y-6 lg:col-span-2">
                <!-- Certificate Information -->
                <Card title={t('certificates.information', $language)}>
                    <dl class="grid grid-cols-1 gap-x-4 gap-y-6 sm:grid-cols-2">
                        <div>
                            <dt class="text-sm font-medium text-gray-500">{t('certificates.subject', $language)}</dt>
                            <dd class="mt-1 text-sm text-gray-900">
                                <div class="flex items-center justify-between">
                                    <span class="break-all">{certificate.subject}</span>
                                    <button
                                        onclick={() => copyToClipboard(certificate!.subject)}
                                        class="ml-2 text-gray-400 hover:text-gray-600"
                                        title={t('certificates.copyToClipboard', $language)}
                                        aria-label={`${t('certificates.copyToClipboard', $language)} ${t('certificates.subject', $language)}`}
                                    >
                                        <svg class="h-4 w-4" fill="none" viewBox="0 0 24 24" stroke="currentColor" aria-hidden="true">
                                            <path
                                                stroke-linecap="round"
                                                stroke-linejoin="round"
                                                stroke-width="2"
                                                d="M8 16H6a2 2 0 01-2-2V6a2 2 0 012-2h8a2 2 0 012 2v2m-6 12h8a2 2 0 002-2v-8a2 2 0 00-2-2h-8a2 2 0 00-2 2v8a2 2 0 002 2z"
                                            />
                                        </svg>
                                    </button>
                                </div>
                            </dd>
                        </div>

                        <div>
                            <dt class="text-sm font-medium text-gray-500">{t('certificates.issuer', $language)}</dt>
                            <dd class="mt-1 text-sm text-gray-900">
                                <div class="flex items-center justify-between">
                                    <span class="break-all">{certificate.issuer}</span>
                                    <button
                                        onclick={() => copyToClipboard(certificate!.issuer)}
                                        class="ml-2 text-gray-400 hover:text-gray-600"
                                        title={t('certificates.copyToClipboard', $language)}
                                        aria-label={`${t('certificates.copyToClipboard', $language)} ${t('certificates.issuer', $language)}`}
                                    >
                                        <svg class="h-4 w-4" fill="none" viewBox="0 0 24 24" stroke="currentColor" aria-hidden="true">
                                            <path
                                                stroke-linecap="round"
                                                stroke-linejoin="round"
                                                stroke-width="2"
                                                d="M8 16H6a2 2 0 01-2-2V6a2 2 0 012-2h8a2 2 0 012 2v2m-6 12h8a2 2 0 002-2v-8a2 2 0 00-2-2h-8a2 2 0 00-2 2v8a2 2 0 002 2z"
                                            />
                                        </svg>
                                    </button>
                                </div>
                            </dd>
                        </div>

                        {#if certificate.serialNumber}
                            <div>
                                <dt class="text-sm font-medium text-gray-500">{t('certificates.serialNumber', $language)}</dt>
                                <dd class="mt-1 font-mono text-sm text-gray-900">
                                    <div class="flex items-center justify-between">
                                        <span class="break-all">{certificate.serialNumber}</span>
                                        <button
                                            onclick={() => copyToClipboard(certificate!.serialNumber!)}
                                            class="ml-2 text-gray-400 hover:text-gray-600"
                                            title={t('certificates.copyToClipboard', $language)}
                                            aria-label={`${t('certificates.copyToClipboard', $language)} ${t('certificates.serialNumber', $language)}`}
                                        >
                                            <svg class="h-4 w-4" fill="none" viewBox="0 0 24 24" stroke="currentColor" aria-hidden="true">
                                                <path
                                                    stroke-linecap="round"
                                                    stroke-linejoin="round"
                                                    stroke-width="2"
                                                    d="M8 16H6a2 2 0 01-2-2V6a2 2 0 012-2h8a2 2 0 012 2v2m-6 12h8a2 2 0 002-2v-8a2 2 0 00-2-2h-8a2 2 0 00-2 2v8a2 2 0 002 2z"
                                                />
                                            </svg>
                                        </button>
                                    </div>
                                </dd>
                            </div>
                        {/if}

                        <div>
                            <dt class="text-sm font-medium text-gray-500">{t('certificates.thumbprint', $language)}</dt>
                            <dd class="mt-1 font-mono text-sm text-gray-900">
                                <div class="flex items-center justify-between">
                                    <span class="break-all">{certificate.thumbprint}</span>
                                    <button
                                        onclick={() => copyToClipboard(certificate!.thumbprint)}
                                        class="ml-2 text-gray-400 hover:text-gray-600"
                                        title={t('certificates.copyToClipboard', $language)}
                                        aria-label={`${t('certificates.copyToClipboard', $language)} ${t('certificates.thumbprint', $language)}`}
                                    >
                                        <svg class="h-4 w-4" fill="none" viewBox="0 0 24 24" stroke="currentColor" aria-hidden="true">
                                            <path
                                                stroke-linecap="round"
                                                stroke-linejoin="round"
                                                stroke-width="2"
                                                d="M8 16H6a2 2 0 01-2-2V6a2 2 0 012-2h8a2 2 0 012 2v2m-6 12h8a2 2 0 002-2v-8a2 2 0 00-2-2h-8a2 2 0 00-2 2v8a2 2 0 002 2z"
                                            />
                                        </svg>
                                    </button>
                                </div>
                            </dd>
                        </div>

                        <div>
                            <dt class="text-sm font-medium text-gray-500">{t('certificates.validFrom', $language)}</dt>
                            <dd class="mt-1 text-sm text-gray-900">{formatDateTime(certificate.notBefore)}</dd>
                        </div>

                        <div>
                            <dt class="text-sm font-medium text-gray-500">{t('certificates.validUntil', $language)}</dt>
                            <dd
                                class="mt-1 text-sm text-gray-900 {status === 'expired'
                                    ? 'font-medium text-red-600'
                                    : ''}"
                            >
                                {formatDateTime(certificate.notAfter)}
                            </dd>
                        </div>
                    </dl>
                </Card>

                <!-- Bundle Contents -->
                {#if hasChildCertificates}
                    <Card title={t('certificates.bundleContents', $language)}>
                        <div class="space-y-4">
                            <p class="text-sm text-gray-600">
                                {t('certificates.bundleContainsCount', $language, { count: certificate.childCertificates.length })}:
                            </p>
                            <div class="space-y-3">
                                {#each certificate.childCertificates as childCert}
                                    {@const childStatus = getCertificateStatus(childCert.notAfter)}
                                    <div class="rounded-lg border border-gray-200 p-4">
                                        <div class="flex items-center justify-between">
                                            <div class="min-w-0 flex-1">
                                                <h4
                                                    class="truncate text-sm font-medium text-gray-900"
                                                    title={childCert.subject}
                                                >
                                                    {childCert.subject}
                                                </h4>
                                                <p class="mt-1 truncate text-xs text-gray-500" title={childCert.issuer}>
                                                    {t('certificates.issuedBy', $language)}: {childCert.issuer}
                                                </p>
                                                <p class="mt-1 text-xs text-gray-500">
                                                    {t('certificates.expires', $language)}: {formatDate(childCert.notAfter)}
                                                </p>
                                            </div>
                                            <div class="ml-4">
                                                <span
                                                    class="inline-flex rounded-full px-2 py-1 text-xs font-semibold {getStatusColor(
                                                        childStatus
                                                    )}"
                                                >
                                                    {getStatusText(childStatus)}
                                                </span>
                                            </div>
                                        </div>
                                    </div>
                                {/each}
                            </div>
                        </div>
                    </Card>
                {/if}
            </div>

            <!-- Sidebar -->
            <div class="space-y-6">
                <!-- Quick Stats -->
                <Card title={t('certificates.quickStats', $language)}>
                    <dl class="space-y-4">
                        <div class="flex items-center justify-between">
                            <dt class="text-sm font-medium text-gray-500">{t('certificates.status', $language)}</dt>
                            <dd
                                class="text-sm font-semibold {status === 'expired'
                                    ? 'text-red-600'
                                    : status === 'expiring'
                                        ? 'text-yellow-600'
                                        : 'text-green-600'}"
                            >
                                {getStatusText(status)}
                            </dd>
                        </div>

                        <div class="flex items-center justify-between">
                            <dt class="text-sm font-medium text-gray-500">{t('certificates.daysUntilExpiry', $language)}</dt>
                            <dd class="text-sm font-medium text-gray-900">
                                {#if certificate.daysUntilExpiry > 0}
                                    {certificate.daysUntilExpiry} {t('certificates.days', $language)}
                                {:else if certificate.daysUntilExpiry === 0}
                                    <span class="text-yellow-600">{t('certificates.expiresToday', $language)}</span>
                                {:else}
                                    <span class="text-red-600">
                                        {t('certificates.expiredDaysAgo', $language, { days: Math.abs(certificate.daysUntilExpiry) })}
                                    </span>
                                {/if}
                            </dd>
                        </div>

                        <div class="flex items-center justify-between">
                            <dt class="text-sm font-medium text-gray-500">{t('certificates.fileFormat', $language)}</dt>
                            <dd class="text-sm text-gray-900">{certificate.fileFormat}</dd>
                        </div>

                        <div class="flex items-center justify-between">
                            <dt class="text-sm font-medium text-gray-500">{t('certificates.fileSize', $language)}</dt>
                            <dd class="text-sm text-gray-900">{formatFileSize(certificate.fileSize)}</dd>
                        </div>

                        {#if hasChildCertificates}
                            <div class="flex items-center justify-between">
                                <dt class="text-sm font-medium text-gray-500">{t('certificates.bundleSize', $language)}</dt>
                                <dd class="text-sm text-gray-900">
                                    {t('certificates.certificatesCount', $language, { count: certificate.childCertificates.length })}
                                </dd>
                            </div>
                        {/if}
                    </dl>
                </Card>

                <!-- File Information -->
                <Card title={t('certificates.fileInformation', $language)}>
                    <dl class="space-y-4">
                        <div>
                            <dt class="text-sm font-medium text-gray-500">{t('certificates.originalFilename', $language)}</dt>
                            <dd class="mt-1 text-sm break-all text-gray-900">{certificate.originalFileName}</dd>
                        </div>

                        <div>
                            <dt class="text-sm font-medium text-gray-500">{t('certificates.uploaded', $language)}</dt>
                            <dd class="mt-1 text-sm text-gray-900">{formatDateTime(certificate.createdAt)}</dd>
                        </div>

                        <div>
                            <dt class="text-sm font-medium text-gray-500">{t('certificates.lastUpdated', $language)}</dt>
                            <dd class="mt-1 text-sm text-gray-900">{formatDateTime(certificate.updatedAt)}</dd>
                        </div>
                    </dl>
                </Card>
            </div>
        </div>
    {/if}
</div>

<!-- Delete Confirmation Modal -->
<Modal
    isOpen={showDeleteModal}
    title={t('certificates.deleteCertificate', $language)}
    onClose={() => (showDeleteModal = false)}
>
    <div class="space-y-4">
        <div class="flex items-center">
            <svg class="mr-3 h-6 w-6 text-red-600" fill="none" viewBox="0 0 24 24" stroke="currentColor" aria-hidden="true">
                <path
                    stroke-linecap="round"
                    stroke-linejoin="round"
                    stroke-width="2"
                    d="M12 9v2m0 4h.01m-6.938 4h13.856c1.54 0 2.502-1.667 1.732-2.5L13.732 4c-.77-.833-1.865-.833-2.634 0L4.268 18.5c-.77.833.192 2.5 1.732 2.5z"
                />
            </svg>
            <div>
                <h3 class="text-lg font-medium text-gray-900">{t('certificates.confirmDeletion', $language)}</h3>
                <p class="mt-1 text-sm text-gray-500">
                    {t('certificates.deletionWarning', $language)}
                </p>
            </div>
        </div>

        {#if certificate}
            <div class="rounded-md border border-gray-200 bg-gray-50 p-3">
                <p class="text-sm font-medium text-gray-900">{certificate.subject}</p>
                <p class="text-xs text-gray-500">{t('certificates.file', $language)}: {certificate.originalFileName}</p>
            </div>
        {/if}

        <div class="flex justify-end space-x-3 pt-4">
            <Button variant="outline" onclick={() => (showDeleteModal = false)} type="button">
                {t('common.cancel', $language)}
            </Button>
            <Button variant="danger" loading={isDeleting} onclick={handleDelete}>
                {t('common.delete', $language)}
            </Button>
        </div>
    </div>
</Modal>