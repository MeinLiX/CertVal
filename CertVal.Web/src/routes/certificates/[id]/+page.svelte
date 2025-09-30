<script lang="ts">
	import { onMount } from 'svelte';
	import { page } from '$app/state';
	import { goto } from '$app/navigation';
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
	let isDownloading = $state(false);
	let downloadProgress = $state(0);
	let parentCertificate = $state<Certificate | null>(null);

	const certificateId = $derived(page.params.id);

	onMount(async () => {
		if (!$auth.isAuthenticated) {
			goto('/auth/login');
			return;
		}
		await loadCertificate();
	});

	$effect(() => {
		if (certificateId) {
			loadCertificate();
		}
	});

	async function loadCertificate() {
		isLoading = true;
		try {
			const response = await api.get<Certificate>(`/v1/certificates/${certificateId}`);
			if (response.data) {
				certificate = response.data;
				if (certificate.parentCertificateId && certificate.parentCertificateId !== certificate.id) {
					const parentRes = await api.get<Certificate>(
						`/v1/certificates/${certificate.parentCertificateId}`
					);
					if (parentRes.data) parentCertificate = parentRes.data;
				}
			}
		} catch (err) {
			console.error('Failed to load certificate:', err);
		} finally {
			isLoading = false;
		}
	}

	async function handleDelete() {
		isDeleting = true;
		try {
			await api.delete(`/v1/certificates/${certificateId}`);
			goto(`/certificates?workspace=${certificate?.workspaceId || ''}`);
		} catch (err) {
			// Handle error, e.g., show a toast
		} finally {
			isDeleting = false;
			showDeleteModal = false;
		}
	}

	async function handleDownload() {
		if (!certificate) return;
		isDownloading = true;
		downloadProgress = 0;
		try {
			const res = await api.downloadAndSave(`/v1/certificates/${certificate.id}/download`, {
				onProgress: (p) => (downloadProgress = p),
				suggestedFileName: certificate.originalFileName
			});
			if ('message' in res) {
				alert(res.message);
			}
		} catch (err) {
			console.error('Failed to download certificate:', err);
			alert('Download failed.');
		} finally {
			isDownloading = false;
			downloadProgress = 0;
		}
	}
</script>

<svelte:head>
	<title>{certificate ? certificate.subject : t('nav.certificates', $language)}</title>
</svelte:head>

<div class="space-y-6">
	{#if isLoading}
		<div class="flex h-96 items-center justify-center">
			<span class="loading loading-lg loading-spinner"></span>
		</div>
	{:else if !certificate}
		<Card><p class="py-12 text-center">{t('certificates.notFound', $language)}</p></Card>
	{:else}
		<div>
			<div class="breadcrumbs text-sm">
				<ul>
					<li>
						<a href="/certificates?workspace={certificate.workspaceId}"
							>{t('nav.certificates', $language)}</a
						>
					</li>
					<li><span class="max-w-xs truncate">{certificate.subject}</span></li>
				</ul>
			</div>
			<div class="mt-2 flex items-center justify-between">
				<h1 class="max-w-lg truncate text-2xl font-bold">{certificate.subject}</h1>
				<div class="flex gap-2">
					<Button variant="ghost" onclick={handleDownload} loading={isDownloading}>
						{t('common.download', $language)}{isDownloading ? ` (${downloadProgress}%)` : ''}
					</Button>
					<Button variant="danger" onclick={() => (showDeleteModal = true)}>
						{t('common.delete', $language)}
					</Button>
				</div>
			</div>
		</div>

		{@const status = getCertificateStatus(certificate.notAfter)}
		<Card
			class={`mt-4 ${
				status === 'expired'
					? 'border-error bg-error/10'
					: status === 'expiring'
						? 'border-warning bg-warning/10'
						: 'border-success bg-success/10'
			}`}
		>
			<div class="flex items-center justify-between">
				<div class="text-lg font-semibold">{t(`certificates.${status}`, $language)}</div>
				<div class="text-right">
					<div>
						{t('certificates.expires', $language)}:
						<strong>{formatDateTime(certificate.notAfter)}</strong>
					</div>
					<div class="text-sm opacity-80">
						{certificate.daysUntilExpiry}
						{t('certificates.daysRemaining', $language)}
					</div>
				</div>
			</div>
		</Card>

		<div class="grid grid-cols-1 gap-6 lg:grid-cols-3">
			<div class="space-y-6 lg:col-span-2">
				<Card title={t('certificates.details', $language)}>
					<dl class="space-y-4 text-sm">
						<div class="grid grid-cols-1 gap-2 md:grid-cols-3">
							<dt class="font-semibold opacity-70">{t('certificates.issuer', $language)}</dt>
							<dd class="break-all md:col-span-2">{certificate.issuer}</dd>
						</div>
						<div class="grid grid-cols-1 gap-2 md:grid-cols-3">
							<dt class="font-semibold opacity-70">{t('certificates.serialNumber', $language)}</dt>
							<dd class="break-all font-mono md:col-span-2">{certificate.serialNumber}</dd>
						</div>
						<div class="grid grid-cols-1 gap-2 md:grid-cols-3">
							<dt class="font-semibold opacity-70">
								{t('certificates.thumbprint', $language)} (SHA-1)
							</dt>
							<dd class="break-all font-mono md:col-span-2">{certificate.thumbprint}</dd>
						</div>
						<div class="grid grid-cols-1 gap-2 md:grid-cols-3">
							<dt class="font-semibold opacity-70">{t('certificates.validFrom', $language)}</dt>
							<dd class="md:col-span-2">{formatDateTime(certificate.notBefore)}</dd>
						</div>
						<div class="grid grid-cols-1 gap-2 md:grid-cols-3">
							<dt class="font-semibold opacity-70">{t('certificates.validUntil', $language)}</dt>
							<dd class="md:col-span-2">{formatDateTime(certificate.notAfter)}</dd>
						</div>
					</dl>
				</Card>

				{#if certificate.isBundle && certificate.childCertificates.length > 0}
					<Card
						title={`${t('certificates.bundleContents', $language)} (${certificate.childCertificates.length})`}
					>
						<div class="space-y-2">
							{#each certificate.childCertificates as child}
								<div class="border-base-content/10 rounded-lg border p-3">
									<p class="truncate text-sm font-semibold">{child.subject}</p>
									<p class="text-xs opacity-60">
										{t('certificates.expires', $language)}: {formatDate(child.notAfter)}
									</p>
								</div>
							{/each}
						</div>
					</Card>
				{/if}
			</div>

			<div class="space-y-6">
				<Card title={t('certificates.metadata', $language)}>
					<dl class="space-y-3 text-sm">
						<div>
							<dt class="font-semibold opacity-70">
								{t('certificates.originalFilename', $language)}
							</dt>
							<dd class="break-all">{certificate.originalFileName}</dd>
						</div>
						<div>
							<dt class="font-semibold opacity-70">{t('certificates.fileFormat', $language)}</dt>
							<dd>{certificate.fileFormat}</dd>
						</div>
						<div>
							<dt class="font-semibold opacity-70">{t('certificates.fileSize', $language)}</dt>
							<dd>{(certificate.fileSize / 1024).toFixed(2)} KB</dd>
						</div>
						<div>
							<dt class="font-semibold opacity-70">{t('certificates.uploadedAt', $language)}</dt>
							<dd>{formatDateTime(certificate.createdAt)}</dd>
						</div>
					</dl>
				</Card>

				{#if certificate.parentCertificateId}
					<Card title={t('certificates.baseCertificate', $language)}>
						{#if parentCertificate}
							<div class="flex items-center justify-between gap-4 text-sm">
								<div class="min-w-0">
									<div class="truncate font-semibold">{parentCertificate.subject}</div>
									<div class="opacity-70">
										{t('certificates.expires', $language)}: {formatDateTime(
											parentCertificate.notAfter
										)}
									</div>
								</div>
								<div class="shrink-0">
									<Button
										size="sm"
										onclick={() => {
											if (parentCertificate?.id) {
												goto(`/certificates/${parentCertificate.id}`);
											}
										}}
									>
										{t('common.view', $language)}
									</Button>
								</div>
							</div>
						{:else}
							<div class="flex items-center gap-2 text-sm opacity-70">
								<span class="loading loading-spinner loading-xs"></span>
								<span>{t('common.loading', $language)}</span>
							</div>
						{/if}
					</Card>
				{/if}
			</div>
		</div>
	{/if}
</div>

<Modal
	isOpen={showDeleteModal}
	title={t('certificates.deleteCertificate', $language)}
	onClose={() => (showDeleteModal = false)}
>
	<p>{t('certificates.confirmDeleteMessage', $language)}</p>
	<div class="modal-action">
		<Button type="button" variant="ghost" onclick={() => (showDeleteModal = false)}>
			{t('common.cancel', $language)}
		</Button>
		<Button variant="danger" loading={isDeleting} onclick={handleDelete}>
			{t('common.delete', $language)}
		</Button>
	</div>
</Modal>
