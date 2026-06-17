<script lang="ts">
	import { language } from '$lib/stores/language.svelte';
	import { t } from '$lib/i18n';
	import UtilsSubnav from '$lib/components/layout/UtilsSubnav.svelte';
	import Button from '$lib/components/ui/Button.svelte';
	import { api } from '$lib/utils/api';
	import type { SslCheckResult, SslCertInfo } from '$lib/types';

	const lang = $derived(language.current);

	let host = $state('');
	let busy = $state(false);
	let error = $state<string | null>(null);
	let result = $state<SslCheckResult | null>(null);

	async function run() {
		error = null;
		result = null;
		if (!host.trim()) {
			error = t('utils.sslCheck.hostLabel', lang);
			return;
		}
		busy = true;
		const response = await api.post<SslCheckResult>('/tools/ssl-check', { host: host.trim() });
		busy = false;
		if (response.data) {
			result = response.data;
		} else {
			error = response.message ?? t('utils.sslCheck.unreachable', lang);
		}
	}

	function yesNo(v: boolean | undefined): string {
		return v ? t('utils.sslCheck.yes', lang) : t('utils.sslCheck.no', lang);
	}

	function rows(c: SslCertInfo): Array<[string, string]> {
		return [
			['Subject', c.subject],
			['Issuer', c.issuer],
			['Valid from', new Date(c.notBefore).toLocaleString()],
			['Valid to', new Date(c.notAfter).toLocaleString()],
			['Public key', c.publicKey],
			['Signature', c.signatureAlgorithm],
			['SHA-256', c.sha256Thumbprint]
		];
	}
</script>

<svelte:head>
	<title>{t('utils.sslCheck.title', lang)} – CertVal</title>
</svelte:head>

<div class="page">
	<UtilsSubnav description={t('utils.sslCheck.description', lang)} />

	<h1>{t('utils.sslCheck.title', lang)}</h1>

	<form
		class="form"
		onsubmit={(e) => {
			e.preventDefault();
			run();
		}}
	>
		<label class="field">
			<span>{t('utils.sslCheck.hostLabel', lang)}</span>
			<input bind:value={host} placeholder={t('utils.sslCheck.hostPlaceholder', lang)} spellcheck="false" />
		</label>
		<Button type="submit" loading={busy}>{t('utils.sslCheck.check', lang)}</Button>
	</form>

	{#if error}<p class="error">{error}</p>{/if}

	{#if result}
		<section class="summary">
			<h2>{result.host}:{result.port}</h2>
			{#if result.reachable}
				<div class="badges">
					<span class="badge badge--ok">{t('utils.sslCheck.reachable', lang)}</span>
					{#if result.negotiatedProtocol}<span class="badge">{t('utils.sslCheck.protocol', lang)}: {result.negotiatedProtocol}</span>{/if}
					<span class="badge {result.hostnameMatches ? 'badge--ok' : 'badge--err'}">{t('utils.sslCheck.hostnameMatch', lang)}: {yesNo(result.hostnameMatches)}</span>
					<span class="badge {result.chainTrusted ? 'badge--ok' : 'badge--warn'}">{t('utils.sslCheck.chainTrusted', lang)}: {yesNo(result.chainTrusted)}</span>
				</div>
			{:else}
				<p class="error">{t('utils.sslCheck.unreachable', lang)}{result.error ? ` — ${result.error}` : ''}</p>
			{/if}
		</section>

		{#each result.chain as c, i (i)}
			<section class="cert">
				<header>
					<h3>{i === 0 ? 'Leaf' : `Chain #${i}`}</h3>
					{#if c.isExpired}<span class="badge badge--err">Expired</span>
					{:else}<span class="badge badge--ok">{c.daysRemaining}d left</span>{/if}
				</header>
				<dl>
					{#each rows(c) as [k, v] (k)}
						<dt>{k}</dt>
						<dd class="mono">{v}</dd>
					{/each}
					{#if c.subjectAltNames.length}
						<dt>SAN</dt>
						<dd class="mono">{c.subjectAltNames.join(', ')}</dd>
					{/if}
				</dl>
			</section>
		{/each}
	{/if}
</div>

<style>
	.page { display: flex; flex-direction: column; gap: var(--space-4); }
	h1 { font-family: var(--font-display); margin: 0; }
	.form { display: flex; gap: var(--space-3); align-items: flex-end; flex-wrap: wrap; }
	.field { display: flex; flex-direction: column; gap: var(--space-1); flex: 1; min-width: 240px; font-size: var(--text-sm); font-weight: var(--font-semibold); }
	input {
		width: 100%; box-sizing: border-box; padding: var(--space-2) var(--space-3);
		border: 1px solid var(--color-border); border-radius: var(--radius-md);
		background: var(--color-surface-inset); color: var(--color-text); font: inherit;
	}
	.error { color: var(--color-error); font-size: var(--text-sm); }
	.summary { display: flex; flex-direction: column; gap: var(--space-2); }
	.summary h2 { margin: 0; font-size: 1.1rem; font-family: var(--font-mono); }
	.badges { display: flex; gap: var(--space-2); flex-wrap: wrap; }
	.badge { font-size: 0.72rem; font-weight: 700; padding: 0.15rem 0.5rem; border-radius: var(--radius-sm); background: var(--color-surface-inset); color: var(--color-text-secondary); }
	.badge--ok { background: var(--color-success-light); color: var(--color-success); }
	.badge--err { background: var(--color-error-light); color: var(--color-error); }
	.badge--warn { background: var(--color-warning-light, #fef3c7); color: var(--color-warning, #b45309); }
	.cert { border: 1px solid var(--color-border); border-radius: var(--radius-md); padding: var(--space-4); display: flex; flex-direction: column; gap: var(--space-2); background: var(--color-surface); }
	.cert header { display: flex; align-items: center; justify-content: space-between; }
	.cert h3 { margin: 0; font-size: 1rem; }
	dl { display: grid; grid-template-columns: minmax(110px, max-content) 1fr; gap: var(--space-1) var(--space-4); margin: 0; }
	dt { font-weight: var(--font-semibold); color: var(--color-text-secondary); font-size: var(--text-sm); }
	dd { margin: 0; word-break: break-all; font-size: var(--text-sm); }
	.mono { font-family: var(--font-mono); font-size: 0.78rem; }
</style>
