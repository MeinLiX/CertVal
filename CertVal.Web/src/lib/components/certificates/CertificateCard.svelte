<script lang="ts">
	import { t } from '$lib/i18n';
	import { language } from '$lib/stores/language.svelte';
	import { formatDate, getCertificateStatus } from '$lib/utils/date';
	import type { Certificate } from '$lib/types';

	let {
		certificate,
		workspaceName = '',
		status: externalStatus,
		onclick
	}: {
		certificate: Certificate;
		workspaceName?: string;
		status?: 'valid' | 'expiring' | 'expired';
		onclick?: () => void;
	} = $props();

	const calculatedStatus = $derived(externalStatus || getCertificateStatus(certificate.notAfter));

	const statusInfo = $derived.by(() => {
		switch (calculatedStatus) {
			case 'expired':
				return {
					text: t('certificates.expired', language.current),
					class: 'cert-card--expired'
				};
			case 'expiring':
				return {
					text: t('certificates.expiring', language.current),
					class: 'cert-card--expiring'
				};
			default:
				return {
					text: t('certificates.valid', language.current),
					class: 'cert-card--valid'
				};
		}
	});
</script>

<button
	type="button"
	class="cert-card {statusInfo.class}"
	onclick={onclick}
	data-test-id="certificate-card-{certificate.id}"
>
	{#if certificate.isSkipped}
		<div class="cert-card__badge" title={t('certificates.ignored', language.current)}>
			<svg xmlns="http://www.w3.org/2000/svg" width="12" height="12" viewBox="0 0 24 24" fill="none" stroke="currentColor" stroke-width="2">
				<path d="M17.94 17.94A10.07 10.07 0 0 1 12 20c-7 0-11-8-11-8a18.45 18.45 0 0 1 5.06-5.94M9.9 4.24A9.12 9.12 0 0 1 12 4c7 0 11 8 11 8a18.5 18.5 0 0 1-2.16 3.19m-6.72-1.07a3 3 0 1 1-4.24-4.24" />
				<line x1="1" y1="1" x2="23" y2="23" />
			</svg>
			<span>{t('common.skipped', language.current)}</span>
		</div>
	{/if}

	<div class="cert-card__indicator"></div>

	<div class="cert-card__content">
		<div class="cert-card__header">
			<div class="cert-card__workspace" title={workspaceName}>
				<svg xmlns="http://www.w3.org/2000/svg" width="14" height="14" viewBox="0 0 24 24" fill="none" stroke="currentColor" stroke-width="2">
					<path d="M3 9l9-7 9 7v11a2 2 0 0 1-2 2H5a2 2 0 0 1-2-2z" />
					<polyline points="9 22 9 12 15 12 15 22" />
				</svg>
				<span>{workspaceName || '...'}</span>
			</div>
			<span class="cert-card__status">{statusInfo.text}</span>
		</div>

		<div class="cert-card__body">
			<h3 class="cert-card__subject" title={certificate.subject}>
				{certificate.subject}
			</h3>
			{#if certificate.issuer}
				<p class="cert-card__issuer" title={certificate.issuer}>
					{certificate.issuer}
				</p>
			{/if}
		</div>

		<div class="cert-card__footer">
			<div class="cert-card__expiry">
				<span class="cert-card__expiry-label">{t('certificates.expires', language.current)}</span>
				<span class="cert-card__expiry-date">{formatDate(certificate.notAfter)}</span>
			</div>
			<div class="cert-card__days">
				<span class="cert-card__days-count">{certificate.daysUntilExpiry}</span>
				<span class="cert-card__days-label">{t('certificates.days', language.current)}</span>
			</div>
		</div>
	</div>
</button>

<style>
	.cert-card {
		position: relative;
		display: block;
		width: 100%;
		text-align: left;
		background: var(--color-surface);
		border: 1px solid var(--color-border);
		border-radius: var(--radius-lg);
		overflow: hidden;
		cursor: pointer;
		transition: border-color var(--transition-fast);
	}

	.cert-card:hover {
		border-color: var(--color-border-strong);
	}

	.cert-card:focus {
		outline: none;
		border-color: var(--color-primary);
		box-shadow: 0 0 0 3px var(--color-primary-alpha);
	}

	.cert-card__indicator {
		position: absolute;
		left: 0;
		top: 0;
		bottom: 0;
		width: 4px;
		transition: width 0.2s ease;
	}

	.cert-card:hover .cert-card__indicator {
		width: 6px;
	}

	.cert-card--valid .cert-card__indicator {
		background: var(--color-success);
	}

	.cert-card--expiring .cert-card__indicator {
		background: var(--color-warning);
	}

	.cert-card--expired .cert-card__indicator {
		background: var(--color-error);
	}

	.cert-card__badge {
		position: absolute;
		top: var(--space-2);
		right: var(--space-2);
		display: flex;
		align-items: center;
		gap: var(--space-1);
		padding: var(--space-1) var(--space-2);
		background: var(--color-surface-elevated);
		border-radius: var(--radius-sm);
		font-size: 10px;
		text-transform: uppercase;
		color: var(--color-text-muted);
	}

	.cert-card__content {
		display: flex;
		flex-direction: column;
		gap: var(--space-2);
		padding: var(--space-3);
		padding-left: var(--space-4);
	}

	.cert-card__header {
		display: flex;
		align-items: center;
		justify-content: space-between;
		gap: var(--space-2);
		font-size: var(--text-xs);
	}

	.cert-card__workspace {
		display: flex;
		align-items: center;
		gap: var(--space-1);
		color: var(--color-text-muted);
		max-width: 70%;
		overflow: hidden;
	}

	.cert-card__workspace span {
		white-space: nowrap;
		overflow: hidden;
		text-overflow: ellipsis;
		font-weight: 500;
	}

	.cert-card__workspace svg {
		flex-shrink: 0;
	}

	.cert-card__status {
		font-family: var(--font-mono);
		font-size: 10px;
		font-weight: var(--font-semibold);
		text-transform: uppercase;
		letter-spacing: var(--tracking-wide);
	}

	.cert-card--valid .cert-card__status {
		color: var(--color-success);
	}

	.cert-card--expiring .cert-card__status {
		color: var(--color-warning);
	}

	.cert-card--expired .cert-card__status {
		color: var(--color-error);
	}

	.cert-card__body {
		flex: 1;
		min-height: 0;
		padding: var(--space-1) 0;
	}

	.cert-card__subject {
		margin: 0;
		font-size: var(--text-sm);
		font-weight: 600;
		color: var(--color-text);
		line-height: 1.4;
		display: -webkit-box;
		line-clamp: 2;
		-webkit-line-clamp: 2;
		-webkit-box-orient: vertical;
		overflow: hidden;
	}

	.cert-card__issuer {
		margin: var(--space-1) 0 0;
		font-size: var(--text-xs);
		color: var(--color-text-muted);
		white-space: nowrap;
		overflow: hidden;
		text-overflow: ellipsis;
	}

	.cert-card__footer {
		display: flex;
		align-items: flex-end;
		justify-content: space-between;
		padding-top: var(--space-2);
		border-top: 1px solid var(--color-border);
		margin-top: var(--space-1);
	}

	.cert-card__expiry {
		display: flex;
		flex-direction: column;
		gap: 2px;
	}

	.cert-card__expiry-label {
		font-size: 10px;
		text-transform: uppercase;
		letter-spacing: 0.05em;
		color: var(--color-text-muted);
	}

	.cert-card__expiry-date {
		font-family: var(--font-mono, monospace);
		font-size: var(--text-xs);
		font-weight: 500;
		color: var(--color-text);
	}

	.cert-card__days {
		text-align: right;
	}

	.cert-card__days-count {
		display: block;
		font-family: var(--font-display);
		font-size: var(--text-xl);
		font-weight: var(--font-semibold);
		letter-spacing: var(--tracking-tight);
		line-height: 1;
	}

	.cert-card--valid .cert-card__days-count {
		color: var(--color-success);
	}

	.cert-card--expiring .cert-card__days-count {
		color: var(--color-warning);
	}

	.cert-card--expired .cert-card__days-count {
		color: var(--color-error);
	}

	.cert-card__days-label {
		font-size: 10px;
		color: var(--color-text-muted);
	}
</style>
