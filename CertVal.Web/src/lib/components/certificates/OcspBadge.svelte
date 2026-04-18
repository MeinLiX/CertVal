<script lang="ts">
	import { t } from '$lib/i18n';
	import { language } from '$lib/stores/language.svelte';
	import { formatDate } from '$lib/utils/date';
	import type { OcspStatus } from '$lib/types';

	let {
		status,
		lastCheckedAt,
		revokedAt,
		revocationReason,
		responderUrl,
		showDetails = false,
		size = 'sm'
	}: {
		status: OcspStatus;
		lastCheckedAt?: string;
		revokedAt?: string;
		revocationReason?: string;
		responderUrl?: string;
		showDetails?: boolean;
		size?: 'xs' | 'sm';
	} = $props();

	const meta = $derived.by(() => {
		switch (status) {
			case 'Good':
				return { label: t('ocsp.good', language.current), tone: 'good' };
			case 'Revoked':
				return { label: t('ocsp.revoked', language.current), tone: 'revoked' };
			case 'CheckFailed':
				return { label: t('ocsp.checkFailed', language.current), tone: 'failed' };
			case 'NotConfigured':
				return { label: t('ocsp.notConfigured', language.current), tone: 'neutral' };
			case 'NotChecked':
			default:
				return { label: t('ocsp.notChecked', language.current), tone: 'neutral' };
		}
	});

	const tooltip = $derived.by(() => {
		const parts: string[] = [`OCSP: ${meta.label}`];
		if (lastCheckedAt) parts.push(`${t('ocsp.lastChecked', language.current)}: ${formatDate(lastCheckedAt)}`);
		if (status === 'Revoked' && revokedAt) parts.push(`${t('ocsp.revokedAt', language.current)}: ${formatDate(revokedAt)}`);
		if (status === 'Revoked' && revocationReason) parts.push(`${t('ocsp.reason', language.current)}: ${revocationReason}`);
		return parts.join(' · ');
	});
</script>

<span class="ocsp-badge ocsp-badge--{meta.tone} ocsp-badge--{size}" title={tooltip}>
	<span class="ocsp-badge__dot" aria-hidden="true"></span>
	<span class="ocsp-badge__label">{meta.label}</span>
</span>

{#if showDetails}
	<div class="ocsp-details">
		{#if lastCheckedAt}
			<div class="ocsp-details__row">
				<span class="ocsp-details__label">{t('ocsp.lastChecked', language.current)}</span>
				<span class="ocsp-details__value">{formatDate(lastCheckedAt)}</span>
			</div>
		{/if}
		{#if status === 'Revoked' && revokedAt}
			<div class="ocsp-details__row">
				<span class="ocsp-details__label">{t('ocsp.revokedAt', language.current)}</span>
				<span class="ocsp-details__value">{formatDate(revokedAt)}</span>
			</div>
		{/if}
		{#if status === 'Revoked' && revocationReason}
			<div class="ocsp-details__row">
				<span class="ocsp-details__label">{t('ocsp.reason', language.current)}</span>
				<span class="ocsp-details__value">{revocationReason}</span>
			</div>
		{/if}
		{#if responderUrl}
			<div class="ocsp-details__row">
				<span class="ocsp-details__label">{t('ocsp.responder', language.current)}</span>
				<span class="ocsp-details__value ocsp-details__value--mono">{responderUrl}</span>
			</div>
		{/if}
	</div>
{/if}

<style>
	.ocsp-badge {
		display: inline-flex;
		align-items: center;
		gap: var(--space-1);
		padding: 2px var(--space-2);
		border-radius: var(--radius-sm);
		font-size: 10px;
		font-weight: var(--font-semibold);
		text-transform: uppercase;
		letter-spacing: var(--tracking-wide);
		border: 1px solid transparent;
		white-space: nowrap;
		line-height: 1.4;
	}

	.ocsp-badge--xs {
		font-size: 9px;
		padding: 1px var(--space-1);
	}

	.ocsp-badge__dot {
		width: 6px;
		height: 6px;
		border-radius: 50%;
		background: currentColor;
	}

	.ocsp-badge--good {
		color: var(--color-success);
		background: color-mix(in srgb, var(--color-success) 10%, transparent);
		border-color: color-mix(in srgb, var(--color-success) 30%, transparent);
	}

	.ocsp-badge--revoked {
		color: var(--color-error);
		background: color-mix(in srgb, var(--color-error) 12%, transparent);
		border-color: color-mix(in srgb, var(--color-error) 35%, transparent);
	}

	.ocsp-badge--failed {
		color: var(--color-warning);
		background: color-mix(in srgb, var(--color-warning) 10%, transparent);
		border-color: color-mix(in srgb, var(--color-warning) 30%, transparent);
	}

	.ocsp-badge--neutral {
		color: var(--color-text-muted);
		background: var(--color-surface-elevated);
		border-color: var(--color-border);
	}

	.ocsp-details {
		display: flex;
		flex-direction: column;
		gap: var(--space-1);
		margin-top: var(--space-2);
	}

	.ocsp-details__row {
		display: flex;
		justify-content: space-between;
		gap: var(--space-3);
		font-size: var(--text-xs);
	}

	.ocsp-details__label {
		color: var(--color-text-muted);
		text-transform: uppercase;
		letter-spacing: var(--tracking-wide);
		font-size: 10px;
	}

	.ocsp-details__value {
		color: var(--color-text);
		text-align: right;
		word-break: break-word;
	}

	.ocsp-details__value--mono {
		font-family: var(--font-mono, monospace);
		font-size: 11px;
	}
</style>
