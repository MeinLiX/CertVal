<script lang="ts">
	import { language } from '$lib/stores/language.svelte';
	import { t } from '$lib/i18n';
	import UtilsSubnav from '$lib/components/layout/UtilsSubnav.svelte';
	import FileDropZone from '$lib/components/utils/FileDropZone.svelte';
	import Button from '$lib/components/ui/Button.svelte';
	import { readFileAsUint8Array, downloadText } from '$lib/utils/fileDownload';
	import { describeCertificate, toCertificateDers, type CertInfo } from '$lib/utils/x509';

	let text = $state('');
	let files = $state<File[]>([]);
	let certs = $state<CertInfo[]>([]);
	let error = $state<string | null>(null);
	let busy = $state(false);
	let copied = $state(false);

	const lang = $derived(language.current);

	async function parse() {
		error = null;
		certs = [];
		copied = false;
		let bytes: Uint8Array | null = null;
		if (files.length) bytes = await readFileAsUint8Array(files[0]);
		else if (text.trim()) bytes = new TextEncoder().encode(text);
		if (!bytes) {
			error = t('utils.x509.common.noInput', lang);
			return;
		}
		busy = true;
		try {
			const ders = toCertificateDers(bytes);
			const out: CertInfo[] = [];
			for (const d of ders) out.push(await describeCertificate(d));
			if (out.length === 0) error = t('utils.x509.common.error', lang);
			certs = out;
		} catch (e) {
			error = e instanceof Error ? e.message : t('utils.x509.common.error', lang);
		} finally {
			busy = false;
		}
	}

	function clearAll() {
		text = '';
		files = [];
		certs = [];
		error = null;
		copied = false;
	}

	async function copyJson() {
		await navigator.clipboard.writeText(JSON.stringify(certs, null, 2));
		copied = true;
		setTimeout(() => (copied = false), 1500);
	}

	function rows(c: CertInfo): Array<[string, string]> {
		return [
			['Subject', c.subject],
			['Issuer', c.issuer],
			['Serial number', c.serialNumber],
			['Valid from', new Date(c.notBefore).toLocaleString()],
			['Valid to', new Date(c.notAfter).toLocaleString()],
			['Public key', `${c.publicKeyAlgorithm} ${c.publicKeySize}`],
			['Signature', c.signatureAlgorithm],
			['SHA-1', c.sha1],
			['SHA-256', c.sha256]
		];
	}
</script>

<svelte:head>
	<title>{t('utils.x509.decode.title', lang)} – CertVal</title>
</svelte:head>

<div class="page">
	<UtilsSubnav description={t('utils.x509.decode.description', lang)} />

	<h1>{t('utils.x509.decode.title', lang)}</h1>
	<p class="privacy">{t('utils.x509.common.privacy', lang)}</p>

	<div class="input-grid">
		<label class="field">
			<span>{t('utils.x509.common.pasteLabel', lang)}</span>
			<textarea
				bind:value={text}
				rows="7"
				spellcheck="false"
				placeholder="-----BEGIN CERTIFICATE-----"
			></textarea>
		</label>
		<FileDropZone
			bind:files
			accept=".pem,.crt,.cer,.der,.txt"
			label={t('utils.x509.common.pasteLabel', lang)}
			hint={t('utils.x509.common.pasteHint', lang)}
		/>
	</div>

	<div class="actions">
		<Button onclick={parse} loading={busy}>{t('utils.x509.common.parse', lang)}</Button>
		<Button variant="secondary" onclick={clearAll}>{t('utils.x509.common.clear', lang)}</Button>
		{#if certs.length}
			<Button variant="secondary" onclick={copyJson}>
				{copied ? t('utils.x509.common.copied', lang) : t('utils.x509.common.copyJson', lang)}
			</Button>
		{/if}
	</div>

	{#if error}<p class="error">{error}</p>{/if}

	{#each certs as c, i (i)}
		<section class="cert">
			<header>
				<h2>{c.selfSigned ? 'Self-signed certificate' : 'Certificate'} #{i + 1}</h2>
				<div class="badges">
					{#if c.isExpired}<span class="badge badge--err">Expired</span>
					{:else if c.isNotYetValid}<span class="badge badge--warn">Not yet valid</span>
					{:else}<span class="badge badge--ok">Valid · {c.daysRemaining}d left</span>{/if}
					{#if c.extensions.isCa}<span class="badge badge--ca">CA</span>{/if}
				</div>
			</header>
			<dl>
				{#each rows(c) as [k, v] (k)}
					<dt>{k}</dt>
					<dd class="mono">{v}</dd>
				{/each}
				{#if c.extensions.subjectAltNames.length}
					<dt>SAN</dt>
					<dd class="mono">{c.extensions.subjectAltNames.join(', ')}</dd>
				{/if}
				{#if c.extensions.keyUsages.length}
					<dt>Key usage</dt>
					<dd>{c.extensions.keyUsages.join(', ')}</dd>
				{/if}
				{#if c.extensions.extendedKeyUsages.length}
					<dt>Extended key usage</dt>
					<dd class="mono">{c.extensions.extendedKeyUsages.join(', ')}</dd>
				{/if}
			</dl>
			<Button variant="secondary" onclick={() => downloadText(c.pem, `certificate-${i + 1}.pem`)}>
				{t('utils.x509.common.download', lang)} PEM
			</Button>
		</section>
	{/each}
</div>

<style>
	.page {
		display: flex;
		flex-direction: column;
		gap: var(--space-4);
	}
	h1 {
		font-family: var(--font-display);
		margin: 0;
	}
	.privacy {
		color: var(--color-text-secondary);
		font-size: var(--text-sm);
		margin: 0;
	}
	.input-grid {
		display: grid;
		grid-template-columns: 1fr 1fr;
		gap: var(--space-4);
	}
	@media (max-width: 720px) {
		.input-grid {
			grid-template-columns: 1fr;
		}
	}
	.field {
		display: flex;
		flex-direction: column;
		gap: var(--space-2);
		font-size: var(--text-sm);
		font-weight: var(--font-semibold);
	}
	textarea {
		width: 100%;
		box-sizing: border-box;
		font-family: var(--font-mono);
		font-size: 0.8rem;
		padding: var(--space-3);
		border: 1px solid var(--color-border);
		border-radius: var(--radius-md);
		background: var(--color-surface-inset);
		color: var(--color-text);
		resize: vertical;
	}
	.actions {
		display: flex;
		gap: var(--space-2);
		flex-wrap: wrap;
	}
	.error {
		color: var(--color-error);
		font-size: var(--text-sm);
	}
	.cert {
		border: 1px solid var(--color-border);
		border-radius: var(--radius-md);
		padding: var(--space-4);
		display: flex;
		flex-direction: column;
		gap: var(--space-3);
		background: var(--color-surface);
	}
	.cert header {
		display: flex;
		align-items: center;
		justify-content: space-between;
		gap: var(--space-2);
		flex-wrap: wrap;
	}
	.cert h2 {
		margin: 0;
		font-size: 1.05rem;
	}
	.badges {
		display: flex;
		gap: var(--space-2);
		flex-wrap: wrap;
	}
	.badge {
		font-size: 0.7rem;
		font-weight: 700;
		padding: 0.1rem 0.5rem;
		border-radius: var(--radius-sm);
	}
	.badge--ok {
		background: var(--color-success-light);
		color: var(--color-success);
	}
	.badge--err {
		background: var(--color-error-light);
		color: var(--color-error);
	}
	.badge--warn {
		background: var(--color-warning-light, #fef3c7);
		color: var(--color-warning, #b45309);
	}
	.badge--ca {
		background: var(--color-primary-light);
		color: var(--color-primary);
	}
	dl {
		display: grid;
		grid-template-columns: minmax(120px, max-content) 1fr;
		gap: var(--space-1) var(--space-4);
		margin: 0;
	}
	dt {
		font-weight: var(--font-semibold);
		color: var(--color-text-secondary);
		font-size: var(--text-sm);
	}
	dd {
		margin: 0;
		word-break: break-all;
		font-size: var(--text-sm);
	}
	.mono {
		font-family: var(--font-mono);
		font-size: 0.78rem;
	}
</style>
