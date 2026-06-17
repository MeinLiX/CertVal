<script lang="ts">
	import { onMount } from 'svelte';
	import { page } from '$app/state';
	import { goto } from '$app/navigation';
	import { auth } from '$lib/stores/auth';
	import { language } from '$lib/stores/language.svelte';
	import { api } from '$lib/utils/api';
	import { CertificateService } from '$lib/services/CertificateService';
	import { t } from '$lib/i18n';
	import { formatDate, formatDateTime, getCertificateStatus } from '$lib/utils/date';
	import Button from '$lib/components/ui/Button.svelte';
	import Modal from '$lib/components/ui/Modal.svelte';
	import GlobalLoader from '$lib/components/ui/GlobalLoader.svelte';
	import OcspBadge from '$lib/components/certificates/OcspBadge.svelte';
	import CertificateTags from '$lib/components/certificates/CertificateTags.svelte';
	import type { Certificate } from '$lib/types';

	let certificate = $state<Certificate | null>(null);
	let isLoading = $state(true);
	let showDeleteModal = $state(false);
	let isDeleting = $state(false);
	let isDownloading = $state(false);
	let downloadProgress = $state(0);
	let parentCertificate = $state<Certificate | null>(null);
	let previousCertificate = $state<Certificate | null>(null);
	let nextCertificate = $state<Certificate | null>(null);
	let showUpdateModal = $state(false);
	let updateFile = $state<File | null>(null);
	let isUpdating = $state(false);
	let showToggleSkipModal = $state(false);
	let isTogglingSkip = $state(false);

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
		parentCertificate = null;
		previousCertificate = null;
		nextCertificate = null;

		try {
			const response = await api.get<Certificate>(`/certificates/${certificateId}`);
			if (response.data) {
				certificate = response.data;
				if (certificate.parentCertificateId && certificate.parentCertificateId !== certificate.id) {
					const parentRes = await api.get<Certificate>(`/certificates/${certificate.parentCertificateId}`);
					if (parentRes.data) parentCertificate = parentRes.data;
				}
				if (certificate.previousCertificateId) {
					const prevRes = await api.get<Certificate>(`/certificates/${certificate.previousCertificateId}`);
					if (prevRes.data) previousCertificate = prevRes.data;
				}
				if (certificate.nextCertificateId) {
					const nextRes = await api.get<Certificate>(`/certificates/${certificate.nextCertificateId}`);
					if (nextRes.data) nextCertificate = nextRes.data;
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
			await api.downloadAndSave(`/certificates/${certificate.id}/download`, {
				onProgress: (p) => (downloadProgress = p),
				suggestedFileName: certificate.originalFileName
			});
		} catch (err) {
			console.error('Failed to download certificate:', err);
		} finally {
			isDownloading = false;
			downloadProgress = 0;
		}
	}

	async function handleUpdate(e: Event) {
		e.preventDefault();
		if (!certificate || !updateFile) return;

		isUpdating = true;
		try {
			const res = await CertificateService.uploadUpdated(certificate.id, certificate.workspaceId, updateFile);
			if (res.data) {
				goto(`/certificates/${res.data.id}`);
			}
		} catch (err) {
			console.error(err);
		} finally {
			isUpdating = false;
			showUpdateModal = false;
		}
	}

	function handleUpdateFileChange(event: Event) {
		const input = event.target as HTMLInputElement;
		if (input.files && input.files.length > 0) {
			updateFile = input.files[0];
		}
	}

	function toggleSkip() {
		if (!certificate) return;
		showToggleSkipModal = true;
	}

	async function confirmToggleSkip() {
		if (!certificate) return;
		isTogglingSkip = true;
		try {
			await CertificateService.toggleSkip(certificate.id, certificate.workspaceId, !certificate.isSkipped);
			await loadCertificate();
		} catch (err) {
			console.error('Failed to toggle skip:', err);
		} finally {
			isTogglingSkip = false;
			showToggleSkipModal = false;
		}
	}
</script>

<svelte:head>
	<title>{certificate ? certificate.subject : t('nav.certificates', language.current)} | CertVal</title>
</svelte:head>

<div class="page" data-test-id="certificate-details-page">
	{#if isLoading}
		<GlobalLoader variant="overlay" />
	{:else if !certificate}
		<div class="not-found">
			<div class="not-found__icon">
				<svg xmlns="http://www.w3.org/2000/svg" width="48" height="48" viewBox="0 0 24 24" fill="none" stroke="currentColor" stroke-width="1.5">
					<rect x="3" y="11" width="18" height="11" rx="2" ry="2" />
					<path d="M7 11V7a5 5 0 0 1 10 0v4" />
				</svg>
			</div>
			<h2 class="not-found__title">{t('certificates.notFound', language.current)}</h2>
			<Button variant="secondary" onclick={() => goto('/certificates')} data-test-id="cert-details-back-button">
				← {t('common.back', language.current)}
			</Button>
		</div>
	{:else}
		{@const status = getCertificateStatus(certificate.notAfter)}

		<header class="header">
			<nav class="breadcrumb">
				<a href="/certificates?workspace={certificate.workspaceId}" class="breadcrumb__link">
					{t('nav.certificates', language.current)}
				</a>
				<span class="breadcrumb__separator">/</span>
				<span class="breadcrumb__current">{certificate.subject}</span>
			</nav>

			<div class="header__main">
				<h1 class="header__title">{certificate.subject}</h1>
				<p class="header__thumbprint">{certificate.thumbprint}</p>
			</div>

			<div class="header__actions">
				<Button
					variant="secondary"
					onclick={toggleSkip}
					disabled={!!certificate.nextCertificateId && certificate.isSkipped}
					title={!!certificate.nextCertificateId && certificate.isSkipped ? t('certificates.cannotEnableMonitoring', language.current) : ''}
					data-test-id="cert-toggle-skip-button"
				>
					{certificate.isSkipped ? t('certificates.monitor', language.current) : t('certificates.ignore', language.current)}
				</Button>
				<Button
					variant="secondary"
					onclick={() => { showUpdateModal = true; }}
					disabled={!!certificate.nextCertificateId}
					title={!!certificate.nextCertificateId ? t('certificates.newerVersionAvailable', language.current) : ''}
					data-test-id="cert-update-button"
				>
					{t('certificates.uploadUpdated', language.current)}
				</Button>
				<Button variant="primary" onclick={handleDownload} disabled={isDownloading} data-test-id="cert-download-button">
					{t('common.download', language.current)}{isDownloading ? ` (${downloadProgress}%)` : ''}
				</Button>
				<Button variant="danger" onclick={() => { showDeleteModal = true; }} data-test-id="cert-delete-button">
					{t('common.delete', language.current)}
				</Button>
			</div>
		</header>

		{#if nextCertificate}
			<div class="alert alert--info">
				<svg xmlns="http://www.w3.org/2000/svg" width="24" height="24" viewBox="0 0 24 24" fill="none" stroke="currentColor" stroke-width="2">
					<circle cx="12" cy="12" r="10" />
					<polyline points="12 6 12 12 16 14" />
				</svg>
				<div class="alert__content">
					<h3 class="alert__title">{t('certificates.newerVersionAvailable', language.current)}</h3>
					<p class="alert__text">{t('certificates.newerVersionDescription', language.current)}</p>
				</div>
				<Button variant="secondary" onclick={() => goto(`/certificates/${nextCertificate?.id}`)}>
					{t('common.view', language.current)}
				</Button>
			</div>
		{/if}

		{#if certificate.isSkipped}
			<div class="alert alert--warning">
				<svg xmlns="http://www.w3.org/2000/svg" width="24" height="24" viewBox="0 0 24 24" fill="none" stroke="currentColor" stroke-width="2">
					<path d="M17.94 17.94A10.07 10.07 0 0 1 12 20c-7 0-11-8-11-8a18.45 18.45 0 0 1 5.06-5.94" />
					<line x1="1" y1="1" x2="23" y2="23" />
				</svg>
				<div class="alert__content">
					<h3 class="alert__title">{t('certificates.monitoringSkipped', language.current)}</h3>
					<p class="alert__text">{t('certificates.monitoringSkippedDescription', language.current)}</p>
				</div>
			</div>
		{/if}

		{#if previousCertificate}
			<div class="alert alert--info">
				<svg xmlns="http://www.w3.org/2000/svg" width="24" height="24" viewBox="0 0 24 24" fill="none" stroke="currentColor" stroke-width="2">
					<circle cx="12" cy="12" r="10" />
					<polyline points="12 6 12 12 16 14" />
				</svg>
				<div class="alert__content">
					<h3 class="alert__title">{t('certificates.newVersion', language.current)}</h3>
					<p class="alert__text">
						{t('certificates.previousVersion', language.current)}:
						<a href="/certificates/{previousCertificate.id}" class="alert__link">{previousCertificate.subject}</a>
					</p>
				</div>
			</div>
		{/if}

		<div class="status-card status-card--{status}">
			<div class="status-card__icon">
				<svg xmlns="http://www.w3.org/2000/svg" width="24" height="24" viewBox="0 0 24 24" fill="none" stroke="currentColor" stroke-width="2">
					<circle cx="12" cy="12" r="10" />
					<polyline points="12 6 12 12 16 14" />
				</svg>
			</div>
			<div class="status-card__info">
				<span class="status-card__label">{t(`certificates.${status}`, language.current)}</span>
				<span class="status-card__days">{certificate.daysUntilExpiry} {t('certificates.daysRemaining', language.current)}</span>
			</div>
			<div class="status-card__date">
				<span class="status-card__date-label">{t('certificates.expires', language.current)}</span>
				<span class="status-card__date-value">{formatDateTime(certificate.notAfter)}</span>
			</div>
		</div>

		<div class="content-grid">
			<div class="content-main">
				<section class="card">
					<div class="card__title-row">
						<h2 class="card__title">{t('ocsp.title', language.current)}</h2>
						<OcspBadge
							status={certificate.ocspStatus}
							lastCheckedAt={certificate.ocspLastCheckedAt}
							revokedAt={certificate.ocspRevokedAt}
							revocationReason={certificate.ocspRevocationReason}
							size="sm"
						/>
					</div>
					<p class="card__hint">{t('ocsp.description', language.current)}</p>
				</section>

				<section class="card">
					<h2 class="card__title">{t('certificates.details', language.current)}</h2>
					<div class="details-grid">
						<div class="detail-item">
							<span class="detail-item__label">{t('certificates.issuer', language.current)}</span>
							<span class="detail-item__value">{certificate.issuer}</span>
						</div>
						<div class="detail-item">
							<span class="detail-item__label">{t('certificates.serialNumber', language.current)}</span>
							<span class="detail-item__value detail-item__value--mono">{certificate.serialNumber}</span>
						</div>
						<div class="detail-item detail-item--full">
							<span class="detail-item__label">{t('certificates.thumbprint', language.current)}</span>
							<span class="detail-item__value detail-item__value--mono">{certificate.thumbprint}</span>
						</div>
						<div class="detail-item">
							<span class="detail-item__label">{t('certificates.validFrom', language.current)}</span>
							<span class="detail-item__value">{formatDateTime(certificate.notBefore)}</span>
						</div>
						<div class="detail-item">
							<span class="detail-item__label">{t('certificates.validUntil', language.current)}</span>
							<span class="detail-item__value">{formatDateTime(certificate.notAfter)}</span>
						</div>
					</div>
				</section>

				{#if !certificate.isBundle}
					<CertificateTags
						{certificate}
						onUpdated={(updated) => {
							if (certificate) certificate.tags = updated;
						}}
					/>
				{/if}

				{#if certificate.isBundle && certificate.childCertificates.length > 0}
					<section class="card">
						<h2 class="card__title">{t('certificates.bundleContents', language.current)} ({certificate.childCertificates.length})</h2>
						<div class="bundle-list">
							{#each certificate.childCertificates as child}
								<div class="bundle-item">
									<div class="bundle-item__icon">
										<svg xmlns="http://www.w3.org/2000/svg" width="16" height="16" viewBox="0 0 24 24" fill="none" stroke="currentColor" stroke-width="2">
											<path d="M14 2H6a2 2 0 0 0-2 2v16a2 2 0 0 0 2 2h12a2 2 0 0 0 2-2V8z" />
											<polyline points="14 2 14 8 20 8" />
										</svg>
									</div>
									<div class="bundle-item__info">
										<span class="bundle-item__subject">{child.subject}</span>
										<span class="bundle-item__expiry">{t('certificates.expires', language.current)}: {formatDate(child.notAfter)}</span>
									</div>
								</div>
							{/each}
						</div>
					</section>
				{/if}
			</div>

			<aside class="content-aside">
				<section class="card">
					<h2 class="card__title">{t('certificates.metadata', language.current)}</h2>
					<div class="meta-list">
						<div class="meta-item">
							<span class="meta-item__label">{t('certificates.originalFilename', language.current)}</span>
							<span class="meta-item__value" title={certificate.originalFileName}>{certificate.originalFileName}</span>
						</div>
						<div class="meta-item">
							<span class="meta-item__label">{t('certificates.fileFormat', language.current)}</span>
							<span class="meta-item__value">{certificate.fileFormat}</span>
						</div>
						<div class="meta-item">
							<span class="meta-item__label">{t('certificates.fileSize', language.current)}</span>
							<span class="meta-item__value">{(certificate.fileSize / 1024).toFixed(2)} KB</span>
						</div>
						<div class="meta-item">
							<span class="meta-item__label">{t('certificates.uploadedAt', language.current)}</span>
							<span class="meta-item__value">{formatDateTime(certificate.createdAt)}</span>
						</div>
					</div>
				</section>

				{#if certificate.parentCertificateId && parentCertificate}
					<section class="card">
						<h2 class="card__title">{t('certificates.baseCertificate', language.current)}</h2>
						<div class="parent-cert">
							<div class="parent-cert__info">
								<span class="parent-cert__subject">{parentCertificate.subject}</span>
								<span class="parent-cert__expiry">{t('certificates.expires', language.current)}: {formatDateTime(parentCertificate.notAfter)}</span>
							</div>
							<Button variant="secondary" fullWidth onclick={() => { if (parentCertificate?.id) goto(`/certificates/${parentCertificate.id}`); }}>
								{t('common.view', language.current)}
							</Button>
						</div>
					</section>
				{/if}
			</aside>
		</div>
	{/if}
</div>

<Modal isOpen={showDeleteModal} title={t('certificates.deleteCertificate', language.current)} onclose={() => (showDeleteModal = false)} data-test-id="cert-delete-modal">
	<div class="modal-content">
		<div class="modal-warning modal-warning--error">
			<svg xmlns="http://www.w3.org/2000/svg" width="24" height="24" viewBox="0 0 24 24" fill="none" stroke="currentColor" stroke-width="2">
				<polyline points="3 6 5 6 21 6" />
				<path d="M19 6v14a2 2 0 0 1-2 2H7a2 2 0 0 1-2-2V6m3 0V4a2 2 0 0 1 2-2h4a2 2 0 0 1 2 2v2" />
			</svg>
			<p>{t('certificates.confirmDeleteMessage', language.current)}</p>
		</div>
		<p class="modal-text">
			This action cannot be undone. This will permanently delete the certificate
			<strong>{certificate?.subject}</strong> and remove it from our servers.
		</p>
	</div>
	<div class="modal-actions">
		<Button variant="secondary" onclick={() => { showDeleteModal = false; }}>{t('common.cancel', language.current)}</Button>
		<Button variant="danger" disabled={isDeleting} onclick={handleDelete} data-test-id="cert-delete-confirm-button">
			{isDeleting ? '...' : t('common.delete', language.current)}
		</Button>
	</div>
</Modal>

<Modal isOpen={showUpdateModal} title={t('certificates.uploadUpdated', language.current)} onclose={() => (showUpdateModal = false)} data-test-id="update-certificate-modal">
	<form onsubmit={handleUpdate} class="modal-form">
		<div class="form-group">
			<label class="form-label" for="update-file-upload">{t('certificates.selectFile', language.current)}</label>
			<div class="file-input-wrapper">
				<input id="update-file-upload" type="file" class="file-input" accept=".cer,.crt,.pem,.pfx,.p12" onchange={handleUpdateFileChange} required data-test-id="update-file-input" />
				<div class="file-input-display">
					{#if updateFile}
						<span>{updateFile.name}</span>
					{:else}
						<span class="file-input-placeholder">Choose file or drag & drop</span>
					{/if}
				</div>
			</div>
			<p class="form-hint">{t('certificates.supportedFormats', language.current)}: .cer, .crt, .pem, .pfx, .p12</p>
		</div>
		<div class="modal-actions">
			<Button variant="secondary" type="button" onclick={() => { showUpdateModal = false; }} data-test-id="update-cancel-button">{t('common.cancel', language.current)}</Button>
			<Button variant="primary" type="submit" disabled={isUpdating || !updateFile} data-test-id="update-submit-button">
				{isUpdating ? '...' : t('common.upload', language.current)}
			</Button>
		</div>
	</form>
</Modal>

<Modal
	isOpen={showToggleSkipModal}
	title={certificate?.isSkipped ? t('certificates.monitorConfirmTitle', language.current) : t('certificates.ignoreConfirmTitle', language.current)}
	onclose={() => (showToggleSkipModal = false)}
	data-test-id="cert-toggle-skip-modal"
>
	<div class="modal-content">
		<div class="modal-warning modal-warning--warning">
			<svg xmlns="http://www.w3.org/2000/svg" width="24" height="24" viewBox="0 0 24 24" fill="none" stroke="currentColor" stroke-width="2">
				<path d="M1 12s4-8 11-8 11 8 11 8-4 8-11 8-11-8-11-8z" />
				<circle cx="12" cy="12" r="3" />
			</svg>
			<p>
				{certificate?.isSkipped
					? t('certificates.monitorConfirmMessage', language.current)
					: t('certificates.ignoreConfirmMessage', language.current)}
			</p>
		</div>
	</div>
	<div class="modal-actions">
		<Button variant="secondary" onclick={() => { showToggleSkipModal = false; }}>{t('common.cancel', language.current)}</Button>
		<Button variant="primary" disabled={isTogglingSkip} onclick={confirmToggleSkip} data-test-id="cert-toggle-skip-confirm-button">
			{isTogglingSkip ? '...' : t('common.confirm', language.current)}
		</Button>
	</div>
</Modal>

<style>
	.page {
		padding: var(--space-6);
		max-width: 1200px;
		margin: 0 auto;
	}

	.not-found {
		display: flex;
		flex-direction: column;
		align-items: center;
		justify-content: center;
		text-align: center;
		padding: var(--space-12);
	}

	.not-found__icon {
		color: var(--color-error);
		opacity: 0.5;
		margin-bottom: var(--space-4);
	}

	.not-found__title {
		font-size: var(--text-xl);
		font-weight: 600;
		color: var(--color-error);
		margin: 0 0 var(--space-4);
	}

	.header {
		margin-bottom: var(--space-6);
	}

	.breadcrumb {
		display: flex;
		align-items: center;
		gap: var(--space-2);
		font-size: var(--text-sm);
		color: var(--color-text-muted);
		margin-bottom: var(--space-3);
	}

	.breadcrumb__link {
		color: var(--color-text-muted);
		text-decoration: none;
		transition: color 0.15s ease;
	}

	.breadcrumb__link:hover {
		color: var(--color-primary);
	}

	.breadcrumb__current {
		max-width: 200px;
		overflow: hidden;
		text-overflow: ellipsis;
		white-space: nowrap;
	}

	.header__main {
		margin-bottom: var(--space-4);
	}

	.header__title {
		font-family: var(--font-display);
		font-size: var(--text-3xl);
		font-weight: var(--font-semibold);
		letter-spacing: var(--tracking-tight);
		line-height: var(--leading-tight);
		color: var(--color-text);
		margin: 0;
	}

	.header__thumbprint {
		font-family: var(--font-mono, monospace);
		font-size: var(--text-sm);
		color: var(--color-text-muted);
		margin: var(--space-1) 0 0;
	}

	.header__actions {
		display: flex;
		flex-wrap: wrap;
		gap: var(--space-3);
	}

	.alert {
		display: flex;
		align-items: flex-start;
		gap: var(--space-3);
		padding: var(--space-4);
		border-radius: var(--radius-lg);
		margin-bottom: var(--space-4);
	}

	.alert--info {
		background: var(--color-primary-alpha);
		border: 1px solid var(--color-primary);
		color: var(--color-primary);
	}

	.alert--warning {
		background: var(--color-warning-bg);
		border: 1px solid var(--color-warning);
		color: var(--color-warning);
	}

	.alert svg {
		flex-shrink: 0;
		margin-top: 2px;
	}

	.alert__content {
		flex: 1;
	}

	.alert__title {
		font-size: var(--text-sm);
		font-weight: 600;
		margin: 0 0 var(--space-1);
	}

	.alert__text {
		font-size: var(--text-sm);
		margin: 0;
		opacity: 0.9;
	}

	.alert__link {
		color: inherit;
		font-weight: 600;
	}

	.status-card {
		display: flex;
		align-items: center;
		gap: var(--space-4);
		padding: var(--space-4);
		border-radius: var(--radius-lg);
		border-left: 4px solid;
		margin-bottom: var(--space-6);
	}

	.status-card--valid {
		background: var(--color-success-bg);
		border-left-color: var(--color-success);
	}

	.status-card--expiring {
		background: var(--color-warning-bg);
		border-left-color: var(--color-warning);
	}

	.status-card--expired {
		background: var(--color-error-bg);
		border-left-color: var(--color-error);
	}

	.status-card__icon {
		padding: var(--space-2);
		border-radius: var(--radius-full);
	}

	.status-card--valid .status-card__icon {
		background: var(--color-success);
		color: white;
	}

	.status-card--expiring .status-card__icon {
		background: var(--color-warning);
		color: white;
	}

	.status-card--expired .status-card__icon {
		background: var(--color-error);
		color: white;
	}

	.status-card__info {
		flex: 1;
	}

	.status-card__label {
		display: block;
		font-size: var(--text-lg);
		font-weight: 600;
		color: var(--color-text);
	}

	.status-card__days {
		font-size: var(--text-sm);
		color: var(--color-text-muted);
	}

	.status-card__date {
		text-align: right;
	}

	.status-card__date-label {
		display: block;
		font-size: var(--text-xs);
		color: var(--color-text-muted);
	}

	.status-card__date-value {
		font-family: var(--font-mono, monospace);
		font-size: var(--text-lg);
		font-weight: 600;
		color: var(--color-text);
	}

	.content-grid {
		display: grid;
		grid-template-columns: 1fr;
		gap: var(--space-6);
	}

	@media (min-width: 1024px) {
		.content-grid {
			grid-template-columns: 2fr 1fr;
		}
	}

	.content-main,
	.content-aside {
		display: flex;
		flex-direction: column;
		gap: var(--space-6);
	}

	.card {
		background: var(--color-surface);
		border: 1px solid var(--color-border);
		border-radius: var(--radius-lg);
		padding: var(--space-5);
	}

	.card__title {
		font-size: var(--text-lg);
		font-weight: 600;
		color: var(--color-text);
		margin: 0 0 var(--space-4);
		padding-bottom: var(--space-3);
		border-bottom: 1px solid var(--color-border);
	}

	.card__title-row {
		display: flex;
		align-items: center;
		justify-content: space-between;
		gap: var(--space-3);
		margin: 0 0 var(--space-4);
		padding-bottom: var(--space-3);
		border-bottom: 1px solid var(--color-border);
	}

	.card__title-row .card__title {
		margin: 0;
		padding: 0;
		border: none;
	}

	.card__hint {
		margin: 0 0 var(--space-3);
		color: var(--color-text-muted);
		font-size: var(--text-sm);
	}

	.details-grid {
		display: grid;
		grid-template-columns: 1fr;
		gap: var(--space-4);
	}

	@media (min-width: 640px) {
		.details-grid {
			grid-template-columns: repeat(2, 1fr);
		}
	}

	.detail-item {
		display: flex;
		flex-direction: column;
		gap: var(--space-1);
	}

	.detail-item--full {
		grid-column: 1 / -1;
	}

	.detail-item__label {
		font-size: var(--text-sm);
		color: var(--color-text-muted);
		font-weight: 500;
	}

	.detail-item__value {
		padding: var(--space-3);
		background: var(--color-surface-elevated);
		border-radius: var(--radius-md);
		font-size: var(--text-sm);
		color: var(--color-text);
		word-break: break-all;
	}

	.detail-item__value--mono {
		font-family: var(--font-mono, monospace);
	}

	.bundle-list {
		display: flex;
		flex-direction: column;
		gap: var(--space-3);
	}

	.bundle-item {
		display: flex;
		align-items: center;
		gap: var(--space-3);
		padding: var(--space-3);
		background: var(--color-surface-elevated);
		border-radius: var(--radius-md);
	}

	.bundle-item__icon {
		color: var(--color-primary);
	}

	.bundle-item__info {
		display: flex;
		flex-direction: column;
		gap: 2px;
		min-width: 0;
	}

	.bundle-item__subject {
		font-size: var(--text-sm);
		font-weight: 500;
		color: var(--color-text);
	}

	.bundle-item__expiry {
		font-size: var(--text-xs);
		color: var(--color-text-muted);
	}

	.meta-list {
		display: flex;
		flex-direction: column;
		gap: var(--space-3);
	}

	.meta-item {
		display: flex;
		justify-content: space-between;
		align-items: center;
		padding-bottom: var(--space-3);
		border-bottom: 1px solid var(--color-border);
	}

	.meta-item:last-child {
		padding-bottom: 0;
		border-bottom: none;
	}

	.meta-item__label {
		font-size: var(--text-sm);
		color: var(--color-text-muted);
	}

	.meta-item__value {
		font-size: var(--text-sm);
		font-weight: 500;
		color: var(--color-text);
		max-width: 50%;
		overflow: hidden;
		text-overflow: ellipsis;
		white-space: nowrap;
	}

	.parent-cert {
		display: flex;
		flex-direction: column;
		gap: var(--space-3);
	}

	.parent-cert__info {
		display: flex;
		flex-direction: column;
		gap: var(--space-1);
	}

	.parent-cert__subject {
		font-size: var(--text-sm);
		font-weight: 500;
		color: var(--color-text);
	}

	.parent-cert__expiry {
		font-size: var(--text-xs);
		color: var(--color-text-muted);
	}

	.modal-content {
		margin-bottom: var(--space-4);
	}

	.modal-warning {
		display: flex;
		align-items: center;
		gap: var(--space-3);
		padding: var(--space-4);
		border-radius: var(--radius-md);
		margin-bottom: var(--space-4);
	}

	.modal-warning--error {
		background: var(--color-error-bg);
		color: var(--color-error);
	}

	.modal-warning--warning {
		background: var(--color-warning-bg);
		color: var(--color-warning);
	}

	.modal-warning p {
		margin: 0;
		font-weight: 500;
	}

	.modal-text {
		font-size: var(--text-sm);
		color: var(--color-text-muted);
		margin: 0;
	}

	.modal-text strong {
		font-family: var(--font-mono, monospace);
		color: var(--color-text);
	}

	.modal-actions {
		display: flex;
		justify-content: flex-end;
		gap: var(--space-3);
		padding-top: var(--space-4);
		border-top: 1px solid var(--color-border);
	}

	.modal-form {
		display: flex;
		flex-direction: column;
		gap: var(--space-4);
	}

	.form-group {
		display: flex;
		flex-direction: column;
		gap: var(--space-2);
	}

	.form-label {
		font-size: var(--text-sm);
		font-weight: 500;
		color: var(--color-text);
	}

	.form-hint {
		font-size: var(--text-xs);
		color: var(--color-text-muted);
		margin: 0;
	}

	.file-input-wrapper {
		position: relative;
	}

	.file-input {
		position: absolute;
		inset: 0;
		opacity: 0;
		cursor: pointer;
	}

	.file-input-display {
		padding: var(--space-4);
		border: 2px dashed var(--color-border);
		border-radius: var(--radius-md);
		text-align: center;
		background: var(--color-surface);
		transition: border-color 0.15s ease;
	}

	.file-input-wrapper:hover .file-input-display {
		border-color: var(--color-primary);
	}

	.file-input-placeholder {
		color: var(--color-text-muted);
	}

	@media (max-width: 768px) {
		.page {
			padding: var(--space-4);
		}

		.header__actions {
			flex-direction: column;
		}

		.status-card {
			flex-direction: column;
			text-align: center;
		}

		.status-card__date {
			text-align: center;
		}
	}
</style>
