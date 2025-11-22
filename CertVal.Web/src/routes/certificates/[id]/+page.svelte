<script lang="ts">
	import { onMount } from 'svelte';
	import { page } from '$app/state';
	import { goto } from '$app/navigation';
	import { auth } from '$lib/stores/auth';
	import { language } from '$lib/stores/language.svelte';
	import { api } from '$lib/utils/api';
	import { t } from '$lib/i18n';
	import { formatDate, formatDateTime, getCertificateStatus } from '$lib/utils/date';
	import Card from '$lib/components/ui/Card.svelte';
	import Button from '$lib/components/ui/Button.svelte';
	import Modal from '$lib/components/ui/Modal.svelte';
	import Icon from '$lib/components/ui/Icon.svelte';
	import GlobalLoader from '$lib/components/ui/GlobalLoader.svelte';
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
			const response = await api.get<Certificate>(`/certificates/${certificateId}`);
			if (response.data) {
				certificate = response.data;
				if (certificate.parentCertificateId && certificate.parentCertificateId !== certificate.id) {
					const parentRes = await api.get<Certificate>(
						`/certificates/${certificate.parentCertificateId}`
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
			await api.delete(`/certificates/${certificateId}`);
			goto(`/certificates?workspace=${certificate?.workspaceId || ''}`);
		} catch (err) {
			console.error('Delete failed', err);
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
			const res = await api.downloadAndSave(`/certificates/${certificate.id}/download`, {
				onProgress: (p) => (downloadProgress = p),
				suggestedFileName: certificate.originalFileName
			});
			if ('message' in res) {
				// alert(res.message);
			}
		} catch (err) {
			console.error('Failed to download certificate:', err);
		} finally {
			isDownloading = false;
			downloadProgress = 0;
		}
	}
</script>

<svelte:head>
	<title
		>{certificate ? certificate.subject : t('nav.certificates', language.current)} | CertVal</title
	>
</svelte:head>

<div
	class="animate-in fade-in mx-auto max-w-7xl space-y-8 p-6 duration-500"
	data-test-id="certificate-details-page"
>
	{#if isLoading}
		<GlobalLoader variant="overlay" />
	{:else if !certificate}
		<Card variant="glass" class="border-error/20 bg-error/5">
			<div class="flex flex-col items-center justify-center py-12 text-center">
				<div class="bg-error/10 mb-4 rounded-full p-4">
					<Icon name="security" class="text-error h-12 w-12" />
				</div>
				<h2 class="text-error text-xl font-bold">{t('certificates.notFound', language.current)}</h2>
				<Button
					variant="ghost"
					class="mt-4"
					onclick={() => goto('/certificates')}
					data-test-id="cert-details-back-button"
				>
					<Icon name="leftArrow" class="mr-2 h-4 w-4" />
					{t('common.back', language.current)}
				</Button>
			</div>
		</Card>
	{:else}
		<div class="flex flex-col gap-6 md:flex-row md:items-start md:justify-between">
			<div class="space-y-2">
				<div class="text-base-content/60 flex items-center gap-2 text-sm">
					<a
						href="/certificates?workspace={certificate.workspaceId}"
						class="hover:text-primary transition-colors"
					>
						{t('nav.certificates', language.current)}
					</a>
					<span>/</span>
					<span class="max-w-[200px] truncate">{certificate.subject}</span>
				</div>

				<div class="flex items-center gap-4">
					<div>
						<h1 class="text-3xl font-bold tracking-tight">{certificate.subject}</h1>
						<p class="text-base-content/60 mt-1 font-mono text-sm">{certificate.thumbprint}</p>
					</div>
				</div>
			</div>

			<div class="flex gap-3">
				<Button
					variant="glass"
					onclick={handleDownload}
					loading={isDownloading}
					data-test-id="cert-download-button"
				>
					<Icon name="download" class="mr-2 h-4 w-4" />
					{t('common.download', language.current)}
					{isDownloading ? ` (${downloadProgress}%)` : ''}
				</Button>
				<Button
					variant="danger"
					class="bg-error/10 hover:bg-error/20 text-error border-error/20"
					onclick={() => (showDeleteModal = true)}
					data-test-id="cert-delete-button"
				>
					<Icon name="trash" class="mr-2 h-4 w-4" />
					{t('common.delete', language.current)}
				</Button>
			</div>
		</div>

		{@const status = getCertificateStatus(certificate.notAfter)}
		<Card
			variant="glass"
			class={`border-l-4 ${
				status === 'expired'
					? 'border-l-error bg-error/5'
					: status === 'expiring'
						? 'border-l-warning bg-warning/5'
						: 'border-l-success bg-success/5'
			}`}
		>
			<div class="flex items-center justify-between p-2">
				<div class="flex items-center gap-4">
					<div
						class={`rounded-full p-2 ${
							status === 'expired'
								? 'bg-error/10 text-error'
								: status === 'expiring'
									? 'bg-warning/10 text-warning'
									: 'bg-success/10 text-success'
						}`}
					>
						<Icon name="time" class="h-6 w-6" />
					</div>
					<div>
						<div class="text-lg font-semibold">{t(`certificates.${status}`, language.current)}</div>
						<div class="text-sm opacity-70">
							{certificate.daysUntilExpiry}
							{t('certificates.daysRemaining', language.current)}
						</div>
					</div>
				</div>
				<div class="text-right">
					<div class="text-sm opacity-60">{t('certificates.expires', language.current)}</div>
					<div class="font-mono text-lg font-semibold">{formatDateTime(certificate.notAfter)}</div>
				</div>
			</div>
		</Card>

		<div class="grid grid-cols-1 gap-6 lg:grid-cols-3">
			<div class="space-y-6 lg:col-span-2">
				<Card variant="glass" title={t('certificates.details', language.current)}>
					<div class="grid gap-6 sm:grid-cols-2">
						<div class="space-y-1">
							<div class="flex items-center gap-2 text-sm font-medium opacity-70">
								<Icon name="security" class="h-4 w-4" />
								{t('certificates.issuer', language.current)}
							</div>
							<div class="bg-base-200/30 break-all rounded-lg p-3 text-sm">
								{certificate.issuer}
							</div>
						</div>

						<div class="space-y-1">
							<div class="flex items-center gap-2 text-sm font-medium opacity-70">
								<Icon name="hash" class="h-4 w-4" />
								{t('certificates.serialNumber', language.current)}
							</div>
							<div class="bg-base-200/30 break-all rounded-lg p-3 font-mono text-sm">
								{certificate.serialNumber}
							</div>
						</div>

						<div class="space-y-1 sm:col-span-2">
							<div class="flex items-center gap-2 text-sm font-medium opacity-70">
								<Icon name="key" class="h-4 w-4" />
								{t('certificates.thumbprint', language.current)}
							</div>
							<div class="bg-base-200/30 break-all rounded-lg p-3 font-mono text-sm">
								{certificate.thumbprint}
							</div>
						</div>

						<div class="space-y-1">
							<div class="flex items-center gap-2 text-sm font-medium opacity-70">
								<Icon name="calendar" class="h-4 w-4" />
								{t('certificates.validFrom', language.current)}
							</div>
							<div class="bg-base-200/30 rounded-lg p-3 text-sm">
								{formatDateTime(certificate.notBefore)}
							</div>
						</div>

						<div class="space-y-1">
							<div class="flex items-center gap-2 text-sm font-medium opacity-70">
								<Icon name="calendar" class="h-4 w-4" />
								{t('certificates.validUntil', language.current)}
							</div>
							<div class="bg-base-200/30 rounded-lg p-3 text-sm">
								{formatDateTime(certificate.notAfter)}
							</div>
						</div>
					</div>
				</Card>

				{#if certificate.isBundle && certificate.childCertificates.length > 0}
					<Card
						variant="glass"
						title={`${t('certificates.bundleContents', language.current)} (${certificate.childCertificates.length})`}
					>
						<div class="space-y-3">
							{#each certificate.childCertificates as child}
								<div
									class="border-base-content/5 bg-base-100/30 hover:bg-base-100/50 flex items-center justify-between rounded-xl border p-4 backdrop-blur-sm transition-colors"
								>
									<div class="flex items-center gap-3">
										<div class="bg-primary/10 text-primary rounded-full p-2">
											<Icon name="document" class="h-4 w-4" />
										</div>
										<div>
											<p class="font-medium">{child.subject}</p>
											<p class="text-xs opacity-60">
												{t('certificates.expires', language.current)}: {formatDate(child.notAfter)}
											</p>
										</div>
									</div>
								</div>
							{/each}
						</div>
					</Card>
				{/if}
			</div>

			<div class="space-y-6">
				<Card variant="glass" title={t('certificates.metadata', language.current)}>
					<div class="space-y-4">
						<div
							class="border-base-content/5 flex items-center justify-between border-b pb-3 last:border-0 last:pb-0"
						>
							<div class="flex items-center gap-2 text-sm opacity-70">
								<Icon name="document" class="h-4 w-4" />
								{t('certificates.originalFilename', language.current)}
							</div>
							<div
								class="max-w-[50%] truncate text-sm font-medium"
								title={certificate.originalFileName}
							>
								{certificate.originalFileName}
							</div>
						</div>

						<div
							class="border-base-content/5 flex items-center justify-between border-b pb-3 last:border-0 last:pb-0"
						>
							<div class="flex items-center gap-2 text-sm opacity-70">
								<Icon name="document" class="h-4 w-4" />
								{t('certificates.fileFormat', language.current)}
							</div>
							<div class="text-sm font-medium">{certificate.fileFormat}</div>
						</div>

						<div
							class="border-base-content/5 flex items-center justify-between border-b pb-3 last:border-0 last:pb-0"
						>
							<div class="flex items-center gap-2 text-sm opacity-70">
								<Icon name="hardDrive" class="h-4 w-4" />
								{t('certificates.fileSize', language.current)}
							</div>
							<div class="text-sm font-medium">{(certificate.fileSize / 1024).toFixed(2)} KB</div>
						</div>

						<div
							class="border-base-content/5 flex items-center justify-between border-b pb-3 last:border-0 last:pb-0"
						>
							<div class="flex items-center gap-2 text-sm opacity-70">
								<Icon name="time" class="h-4 w-4" />
								{t('certificates.uploadedAt', language.current)}
							</div>
							<div class="text-sm font-medium">{formatDateTime(certificate.createdAt)}</div>
						</div>
					</div>
				</Card>

				{#if certificate.parentCertificateId}
					<Card variant="glass" title={t('certificates.baseCertificate', language.current)}>
						{#if parentCertificate}
							<div class="bg-base-100/30 rounded-xl p-4">
								<div class="mb-3 flex items-start gap-3">
									<div class="bg-secondary/10 text-secondary rounded-full p-2">
										<Icon name="security" class="h-4 w-4" />
									</div>
									<div class="min-w-0 flex-1">
										<div class="truncate font-medium">{parentCertificate.subject}</div>
										<div class="text-xs opacity-70">
											{t('certificates.expires', language.current)}: {formatDateTime(
												parentCertificate.notAfter
											)}
										</div>
									</div>
								</div>
								<Button
									variant="outline"
									size="sm"
									class="w-full"
									onclick={() => {
										if (parentCertificate?.id) {
											goto(`/certificates/${parentCertificate.id}`);
										}
									}}
								>
									{t('common.view', language.current)}
								</Button>
							</div>
						{:else}
							<div class="flex items-center justify-center py-8 opacity-50">
								<GlobalLoader variant="inline" size="sm" />
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
	title={t('certificates.deleteCertificate', language.current)}
	onClose={() => (showDeleteModal = false)}
	data-test-id="cert-delete-modal"
>
	<div class="space-y-4">
		<div class="bg-error/10 text-error flex items-center gap-4 rounded-lg p-4">
			<Icon name="trash" class="h-6 w-6" />
			<p class="font-medium">{t('certificates.confirmDeleteMessage', language.current)}</p>
		</div>
		<p class="text-sm opacity-70">
			This action cannot be undone. This will permanently delete the certificate
			<span class="font-mono font-bold">{certificate?.subject}</span> and remove it from our servers.
		</p>
	</div>

	<div class="modal-action mt-6">
		<Button type="button" variant="ghost" onclick={() => (showDeleteModal = false)}>
			{t('common.cancel', language.current)}
		</Button>
		<Button
			variant="danger"
			loading={isDeleting}
			onclick={handleDelete}
			data-test-id="cert-delete-confirm-button"
		>
			{t('common.delete', language.current)}
		</Button>
	</div>
</Modal>
